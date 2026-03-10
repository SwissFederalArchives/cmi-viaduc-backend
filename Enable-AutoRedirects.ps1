# One-shot script: enable AutoGenerateBindingRedirects across solution

$solutionDir = Get-Location
$csprojs = Get-ChildItem -Path $solutionDir -Recurse -Include *.csproj

foreach ($proj in $csprojs) {
    [xml]$xml = Get-Content $proj.FullName
    $changed = $false

    # Look for first PropertyGroup without a Condition
    $pg = $xml.Project.PropertyGroup |
          Where-Object { -not $_.Condition } |
          Select-Object -First 1

    if (-not $pg) {
        $pg = $xml.CreateElement("PropertyGroup", $xml.Project.NamespaceURI)
        $xml.Project.AppendChild($pg) | Out-Null
    }

    if (-not $pg.AutoGenerateBindingRedirects) {
        $pg.AppendChild($xml.CreateElement("AutoGenerateBindingRedirects", $xml.Project.NamespaceURI)).InnerText = "true"
        $changed = $true
    } elseif ($pg.AutoGenerateBindingRedirects.InnerText -ne "true") {
        $pg.AutoGenerateBindingRedirects.InnerText = "true"
        $changed = $true
    }

    if (-not $pg.GenerateBindingRedirectsOutputType) {
        $pg.AppendChild($xml.CreateElement("GenerateBindingRedirectsOutputType", $xml.Project.NamespaceURI)).InnerText = "true"
        $changed = $true
    } elseif ($pg.GenerateBindingRedirectsOutputType.InnerText -ne "true") {
        $pg.GenerateBindingRedirectsOutputType.InnerText = "true"
        $changed = $true
    }

    if ($changed) {
        Write-Host "Updated $($proj.FullName)"
        $xml.Save($proj.FullName)
    }
    else {
        Write-Host "No change needed in $($proj.FullName)"
    }
}

# Clean + Rebuild the solution (requires msbuild in PATH, or use full path)
$solutionFile = Get-ChildItem -Path $solutionDir -Filter *.sln | Select-Object -First 1
if ($solutionFile) {
    Write-Host "Cleaning and rebuilding $($solutionFile.FullName)..."
    msbuild $solutionFile.FullName /t:Clean,Build /p:Configuration=Debug
} else {
    Write-Warning "No .sln file found in $solutionDir"
}
