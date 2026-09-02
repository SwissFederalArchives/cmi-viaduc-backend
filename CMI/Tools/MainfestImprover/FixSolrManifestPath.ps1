# Dieses Script fixt die Probleme in Solr die mit doppelten Slashes vorhanden sein könnten.
# Das Tool geht durch sämltiche Datensätze und prüft der Wert in manifest_path.
# Wenn doppelte Slashes gefunden werden, wird der Datensatz in Solr aktualisiert.
#
# Aufruf: powershell.exe -executionpolicy bypass .\FixSolrManifestPath.ps1 -SolrBase "http://localhost:8983/solr" -Collection "iiif" 
#
# Aufruf muss aus dem Verzeichnis heraus erfolgen, wo das Script abgelegt ist. 
param(
  [string]$SolrBase    = "http://localhost:8983/solr",
  [string]$Collection  = "iiif",
  [int]$Rows           = 2000,
  [int]$BatchSize      = 500,
  [int]$MaxDocs        = 0,
  [switch]$DryRun
)

$ErrorActionPreference = "Stop"

$selectUrl = "$SolrBase/$Collection/select"
$updateUrl = "$SolrBase/$Collection/update?overwrite=true&commit=false"
$commitUrl = "$SolrBase/$Collection/update?commit=true"

# Nur Dokumente, die manifest_path wirklich haben
$q  = '*:*'
$fq = 'manifest_path:[* TO *]'

function New-QueryString([hashtable]$p) {
  ($p.GetEnumerator() | ForEach-Object {
    "{0}={1}" -f [uri]::EscapeDataString($_.Key), [uri]::EscapeDataString([string]$_.Value)
  }) -join "&"
}

function Needs-Fix([string]$url) {
  if ([string]::IsNullOrWhiteSpace($url)) { return $false }
  $m = [regex]::Match($url, '^(?<scheme>https?://)(?<rest>.*)$')
  if ($m.Success) {
    return ($m.Groups['rest'].Value -match '/{2,}')
  }
  return ($url -match '/{2,}')
}

function Fix-ManifestPath([string]$url) {
  if ([string]::IsNullOrWhiteSpace($url)) { return $url }
  $m = [regex]::Match($url, '^(?<scheme>https?://)(?<rest>.*)$')
  if ($m.Success) {
    return $m.Groups['scheme'].Value + ([regex]::Replace($m.Groups['rest'].Value, '/{2,}', '/'))
  }
  return [regex]::Replace($url, '/{2,}', '/')
}

function Invoke-SolrSelect([string]$cursorMark) {
	
  $params = @{
    q          = $q
    fq         = $fq
    fl         = 'id,manifest_path'
    rows       = $Rows
    sort       = 'id asc'
    wt         = 'json'
    cursorMark = $cursorMark
  }
  
  $qs  = New-QueryString $params
  $uri = "$selectUrl" + "?" + "$qs"

  Invoke-RestMethod -Method Get -Uri $uri
}



function Post-AtomicUpdates($updates) {
  if ($null -eq $updates -or $updates.Count -eq 0) { return }

  # Sicher in ein echtes Array materialisieren
  $arr = @()
  foreach ($u in $updates) { $arr += $u }

  # Jetzt explizit ein JSON-Array erzeugen
  $payload = ConvertTo-Json -InputObject $arr -Depth 20

  Invoke-RestMethod -Method Post -Uri $updateUrl `
    -ContentType "application/json" `
    -Body $payload | Out-Null
}


$cursorMark = "*"
$totalSeen  = 0
$totalFixed = 0
$pending = New-Object System.Collections.ArrayList


while ($true) {
  $resp = Invoke-SolrSelect $cursorMark
  $docs = $resp.response.docs
  $next = $resp.nextCursorMark

  if (-not $docs -or $docs.Count -eq 0) { break }

  foreach ($d in $docs) {
    $totalSeen++
    $id = $d.id
    $mp = $d.manifest_path

    if (Needs-Fix $mp) {
      $fixed = Fix-ManifestPath $mp
      if ($fixed -ne $mp) {
        $totalFixed++

        if ($DryRun) {
          if ($totalFixed -le 10) {
            Write-Host "FIX $id"
            Write-Host "  old: $mp"
            Write-Host "  new: $fixed"
          }
        } else {
          [void]$pending.Add([pscustomobject]@{
			id = $id
			manifest_path = [pscustomobject]@{ set = $fixed }
			})

          if ($pending.Count -ge $BatchSize) {
            Post-AtomicUpdates $pending
            $pending.Clear()
            Write-Host "Updated: $totalFixed (seen $totalSeen)"
          }
        }
      }
    }

    if ($MaxDocs -gt 0 -and $totalSeen -ge $MaxDocs) { break }
  }

  if ($MaxDocs -gt 0 -and $totalSeen -ge $MaxDocs) { break }
  if ($next -eq $cursorMark) { break }
  $cursorMark = $next

  if (($totalSeen % 200000) -eq 0) {
    Write-Host "Progress: seen $totalSeen, fixed $totalFixed"
  }
}

if (-not $DryRun -and $pending.Count -gt 0) {
  Post-AtomicUpdates $pending
  $pending.Clear()
}

if ($DryRun) {
  Write-Host "DRY RUN done. Seen: $totalSeen, Would-fix: $totalFixed"
} else {
  Invoke-RestMethod -Method Get -Uri $commitUrl | Out-Null
  Write-Host "DONE. Seen: $totalSeen, Fixed: $totalFixed (committed)"
}
