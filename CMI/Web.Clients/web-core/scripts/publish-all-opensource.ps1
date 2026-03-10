
<#
.SYNOPSIS
    Script um einen Import in GitHub Repositories der zuvor exportierten Viaduc-Repositories zu machen.
.DESCRIPTION
    .
.PARAMETER GitPesonalAccessToken
    GitHub Personal Access Token mit Repository-Rechten auf das gewünschte Ziel-Repository (BAR Repository)
.PARAMETER TargetDirectory
    Zielverzeichnis, z.B. C:\Temp\dest
.PARAMETER SourceDirectory
    Quellverzeichnis, z.B. C:\Temp\os
#>
param (
    [Parameter(Mandatory = $true)]
    [string]$GitPesonalAccessToken, #GitHub-Access-Token mit Berechtigung auf CMI GitHub-Account
    [Parameter(Mandatory = $true)]
    [string]$TargetDirectory, #z.B. C:\Temp\dest
    [Parameter(Mandatory = $true)]
    [string]$SourceDirectory, #z.B. C:\Temp\os
    [Parameter(Mandatory = $true)]
    [string]$VersionTag, #z.B. Version vom 08.04.2021
    [Parameter(Mandatory = $false)]
    [string]$GitHubCompanyName = (Read-Host -prompt "GitHubCompanyName ($("SwissFederalArchives"))") #z.B. SwissFederalArchives
)
if (!$GitHubCompanyName) { 
	$GitHubCompanyName = "SwissFederalArchives" 
}


$versionDots = $VersionTag.split(".");

for ($index = 1; $index -lt $versionDots.count; $index++) {
    $isNumeric = $versionDots[$index]-match "^[\d\.]+$"
    if (!$isNumeric) {
        Write-Host "Versionsschema einhalten: v.0.0.0.0 "
        Write-Host "Keine weiteren Buchstaben mehr angeben!"
        Return 
        }        
    }     
    if (!$versionDots[0].Equals("v")) {
        Write-Host "Versionsschema einhalten: v.0.0.0.0 "
        Write-Host "Muss mit v starten! und nnicht mit"
        Write-Host $versionDots[0] 
        Return 
    }


if (!$versionDots.Count.Equals(5)) {
    Write-Host "Versionsschema einhalten: v.0.0.0.0 "
    Write-Host "Zu viele Punkte!"
    Return 
}

Function Import-All-OpenSource {    
    Import-OpenSource "cmi-viaduc-backend"
	Import-OpenSource "cmi-iiif-backend"
	Import-OpenSource "cmi-iiif-frontend"
    Write-Host "Finished publishing repositories"
}


Function Import-OpenSource($ProjectName, $FilesOrDirsToExclude, $ResultName) {
    $ErrorActionPreference = "Stop";

    # Löschen allfällig bestehender Verzeichnisse im Quellverzeichnis
    $UnzipPath = Join-Path $SourceDirectory -ChildPath $ProjectName
    if (Test-Path $UnzipPath) {
        Remove-Item $UnzipPath -Recurse -Force
    }
    
    # Entzippen der Zip Dateien
	Expand-7Zip -ArchiveFileName "$UnzipPath.zip" -TargetPath "$UnzipPath"

    # Holen des bestehenden Destination OS-Github Repositories
    $ClonePath = Join-Path $TargetDirectory -ChildPath $ProjectName
    if (Test-Path $ClonePath) {
        Remove-Item $ClonePath -Recurse -Force
    }
    Get-Git-Repository -GitRepoUrl "https://$GitPesonalAccessToken@github.com/$GitHubCompanyName/$ProjectName" -CloneFolder $ClonePath

    # Löschen des Inhalts des geklonten Repositories
    # Option -force nicht verwenden, ansonsten auch das .git Verzeichnis gelöscht wird
    Remove-Item $ClonePath/* -Recurse 

    # Kopieren des Inhalts aus Source zum Target
    Copy-Item $UnzipPath/* $ClonePath -Recurse

    # Publizieren des Repositories
   # Publish-Git-Repository -GitRepoUrl "https://$GitPesonalAccessToken@github.com/$GitHubCompanyName/$ProjectName" -CloneFolder $ClonePath -VersionTag $VersionTag

   if (-not (git -C $ClonePath rev-parse --quiet --verify HEAD)) {
    # Repo is empty → call empty repo version
    Publish-Git-Repository-EmptyRepo -GitRepoUrl "https://$GitPesonalAccessToken@github.com/$GitHubCompanyName/$ProjectName" -CloneFolder $ClonePath -VersionTag $VersionTag
}
else {
    # Normal case → use original
    Publish-Git-Repository -GitRepoUrl "https://$GitPesonalAccessToken@github.com/$GitHubCompanyName/$ProjectName" -CloneFolder $ClonePath -VersionTag $VersionTag
}


}

Function Get-Git-Repository {
    param (
        [Parameter(Mandatory = $true)]
        [string]$GitRepoUrl,
        [Parameter(Mandatory = $true)]
        [string]$CloneFolder
    )

    Write-Host "Cloning $GitRepoUrl to $CloneFolder"

    try {
        git clone $GitRepoUrl $CloneFolder
        if (-not $?) {
            throw "Error with git clone!"
        }
    }
    catch {
        Write-Error "Error when connecting to $GitRepoUrl"
    }

}
# New function only for empty repos
Function Publish-Git-Repository-EmptyRepo {
    param (
        [Parameter(Mandatory = $true)]
        [string]$GitRepoUrl,
        [Parameter(Mandatory = $true)]
        [string]$CloneFolder,
        [Parameter(Mandatory = $true)]
        [string]$VersionTag
    )

    Write-Host "Publishing (empty repo case) to $GitRepoUrl"
    $currentDir = Get-Location

    try {
        Set-Location -Path $CloneFolder

        # Always create 'main' if repo is empty
        git checkout -B main
        git add --all
        git commit -m $VersionTag

        git push -u $GitRepoUrl main
        if (-not $?) { throw "Error with git push of branch 'main'!" }

        git tag $VersionTag
        git push $GitRepoUrl $VersionTag

        gh release create $VersionTag 2>$null | Out-Null
    }
    catch {
        Write-Error "Error when publishing empty repo to $GitRepoUrl"
        throw
    }
    finally {
        Set-Location $currentDir
    }
}

Function Publish-Git-Repository {
    param (
        [Parameter(Mandatory = $true)]
        [string]$GitRepoUrl,
        [Parameter(Mandatory = $true)]
        [string]$CloneFolder,
        [Parameter(Mandatory = $true)]
        [string]$VersionTag

    )

    Write-Host "Commiting $GitRepoUrl"
    $currentDir = Get-Location

    try {
        Set-Location -Path $CloneFolder

        $ChangedFiles = $(git status --porcelain | Measure-Object | Select-Object -expand Count)
      
        if ($ChangedFiles -gt 0)
        {
            git tag $VersionTag
            git add --all
            git commit -m $VersionTag   
         
            git add --all
            if (-not $?) {
                throw "Error with git commit!"
            }             
          
            git push --repo $GitRepoUrl 
            git push --tags $GitRepoUrl  $VersionTag   
            if (-not $?) {
                throw "Error with git push!"
            }
        }
         gh release create $VersionTag
    }
    catch {
        Write-Error "Error when publishing to $GitRepoUrl"
    }
    finally {
        Set-Location $currentDir
    }

}

Import-All-OpenSource
