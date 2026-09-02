
<#
.SYNOPSIS
    Script um einen Export von den Viaduc-Repositories zu machen, mit dem Ziel öffentlich publizierbare ZIPs zu erstellen
.DESCRIPTION
    .
.PARAMETER GitPesonalAccessToken
    GitHub Personal Access Token mit Repository-Rechten auf das gewünschte CMI Repository
.PARAMETER WorkingDirectory
    Arbeitsverzeichnis, z.B. C:\Temp
#>
param (
    [Parameter(Mandatory = $true)]
    [string]$GitPesonalAccessToken, #GitHub-Access-Token mit Berechtigung auf CMI GitHub-Account
    [Parameter(Mandatory = $true)]
    [string]$WorkingDirectory, #z.B. C:\Temp
    [Parameter(Mandatory = $true)]
    [string]$Version #z. B v.1.2.1007
)


Function Export-All-OpenSource {
    # (unchanged) exclude these helper scripts when zipping
    $ToExclude = 'export-all-opensource.ps1', 'publish-all-opensource.ps1'
    
    # ❌ REMOVE these (old, separate repos):
    # Export-OpenSource "cmi-viaduc-web-core" $ToExclude
    # Export-OpenSource "cmi-viaduc-web-frontend"
    # Export-OpenSource "cmi-viaduc-web-management"

    # ✅ KEEP IIIF as separate repos (unchanged)
    Export-OpenSource "cmi-iiif-frontend"
    Export-OpenSource "cmi-iiif-backend"

    # ✅ Export the monorepo once. Reuse your existing exclude list.
    $ToExclude = @(
        'RemoveBindingRedirect.ps1', 
        'Update Binding Redirects - readme.txt',
        'Change Script Art. 12.3.sql',
        'Change Script PVW-426 .sql',
        'Change Script PVW-798.sql',
        'Change Script PVW-895 Anonymization.sql',
        'Small Tests for FK_VIADUC_DIR_ACCESS.sql',
        'Small Tests for FK_VIADUC_VE_ACCESS_TKN.sql',
        'Viaduc DB Objects for scopeArchiv Database.sql'
    )

    # NOTE: ResultName stays as before to minimize change. 
    # If you prefer the zip to be called 'cmi-viaduc.zip', set -ResultName "cmi-viaduc".
    Export-OpenSource -ProjectName "viaduc" -FilesOrDirsToExclude $ToExclude -ResultName "cmi-viaduc-backend"

    Write-Host "Finished exporting repositories"
}



Function Export-OpenSource([string] $ProjectName, [string[]] $FilesOrDirsToExclude, [string] $ResultName) {
    $ErrorActionPreference = "Stop";

    $ClonePath = Join-Path $WorkingDirectory -ChildPath $ProjectName
    if (Test-Path $ClonePath) {
        Remove-Item $ClonePath -Recurse -Force
    }

	# Get-Git-Repository -GitRepoUrl "https://$GitPesonalAccessToken@github.com/AkrosAG/$ProjectName" -CloneFolder $ClonePath -BranchName $Version
	git clone --branch $Version --single-branch "https://$GitPersonalAccessToken@github.com/AkrosAG/$ProjectName.git" $ClonePath
    Write-Replacements

    if ([System.String]::IsNullOrWhiteSpace($ResultName)) {
        $ResultName = $ProjectName
    }

    $ZipResultPath = "$WorkingDirectory\$ResultName.zip";

    if (Test-Path $ZipResultPath) {
        Remove-Item $ZipResultPath
    }

    
    Get-ChildItem $ClonePath -Recurse | Where-Object { $_.Name -in $ToExclude} | Remove-Item -Force -Verbose

    Compress-Directory -DirectoryToZip "$ClonePath" -ZipResultFullPath $ZipResultPath
}

# Replacements definieren
Function Write-Core-Replacements {
    Write-Replacement -FilePath "$WorkingDirectory\$ProjectName\src\lib\wijmo\wijmo.licensekey.ts" -Replacement "export const WIJMO_LICENSEKEY = 'dummy_license'; // to obtain your own licensekey, see https://www.grapecity.com/wijmo/licensing"
    Write-PartReplacement -FilePath "$WorkingDirectory\$ProjectName\src\package.json" -SearchForRegex '"registry": ".*",' -Replacement '"registry": "enter-a-url-to-package-repository",'
    Remove-File -FilePath  "$WorkingDirectory\$ProjectName\.npmrc"
    Remove-Item "$WorkingDirectory\$ProjectName\.github" -Force -Recurse  -ErrorAction Ignore
    Write-Readme
}

Function Write-Frontend-Replacements {
    Remove-File -FilePath  "$WorkingDirectory\$ProjectName\.npmrc"
    Write-Replacement -FilePath "$WorkingDirectory\$ProjectName\src\app\modules\client\services\unblu-apikey.ts" -Replacement "export const UNBLU_APIKEY = 'dummy_apikey'; // to obtain your own licensekey, see https://www.unblu.com/en/docs/latest/knowledge-base/apikeys.html"
    Write-PartReplacement -FilePath "$WorkingDirectory\$ProjectName\src\config\settings.json" -SearchForRegex '"secret": ".*",' -Replacement '"secret": "google-captcha-secret-here",'
    Write-PartReplacement -FilePath "$WorkingDirectory\$ProjectName\src\config\settings.json" -SearchForRegex '"key": "6L.*",' -Replacement '"key": "google-captcha-key-here",'
    Remove-Item "$WorkingDirectory\$ProjectName\.github" -Force -Recurse  -ErrorAction Ignore
    Write-Readme
}

Function Write-Management-Replacements {
    Remove-File -FilePath  "$WorkingDirectory\$ProjectName\.npmrc"
    Remove-Item "$WorkingDirectory\$ProjectName\.github" -Force -Recurse  -ErrorAction Ignore
    Write-Readme
}

Function Write-IIIF-Frontend-Replacements {
    Remove-Item "$WorkingDirectory\$ProjectName\.github" -Force -Recurse  -ErrorAction Ignore
    Write-Readme
}

Function Write-IIIF-Backend-Replacements {
    Remove-Item "$WorkingDirectory\$ProjectName\.github" -Force -Recurse  -ErrorAction Ignore
    Write-Readme
}

Function Write-Viaduc-Replacements {
    Write-Replacement -FilePath "$WorkingDirectory\$ProjectName\CMI\Engine\Asset\Aspose.Total.NET.lic" -Replacement "Buy license"
    Write-Replacement -FilePath "$WorkingDirectory\$ProjectName\CMI\Manager\DocumentConverter\Extraction\Aspose.Total.NET.lic" -Replacement "Buy license"
    Write-Replacement -FilePath "$WorkingDirectory\$ProjectName\CMI\Web\CMI.Web.Common\Aspose.Total.NET.lic" -Replacement "Buy license"
    Write-Replacement -FilePath "$WorkingDirectory\$ProjectName\CMI\Host\DataFeed\cmi.host.datafeed.exe.licenses" -Replacement "To generate your own licenses file, check the documentation of Devart about licensing."
    Write-Replacement -FilePath "$WorkingDirectory\$ProjectName\CMI\Host\ExternalContent\cmi.host.externalcontent.exe.licenses" -Replacement "To generate your own licenses file, check the documentation of Devart about licensing."
    Write-Replacement -FilePath "$WorkingDirectory\$ProjectName\CMI\Host\Harvest\cmi.host.harvest.exe.licenses" -Replacement "To generate your own licenses file, check the documentation of Devart about licensing."
    Remove-Item "$WorkingDirectory\$ProjectName\.github" -Force -Recurse  -ErrorAction Ignore
    Write-Readme
}

Function Write-CsProj  {
    param (
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$Reference
    )

    $files = Get-ChildItem $Path -Recurse -Filter *.csproj 
    ForEach ($f in $files)
    {
        $xPath = [string]::Format("//a:EmbeddedResource[contains(@Include, '{0}')]", $Reference)      
        $proj = [xml](Get-Content $f.FullName)

        [System.Xml.XmlNamespaceManager] $nsmgr = $proj.NameTable
        $nsmgr.AddNamespace('a','http://schemas.microsoft.com/developer/msbuild/2003')
        $node = $proj.SelectSingleNode($xPath, $nsmgr)
    
        if (!$node)
        { 
            continue
        }
    
        Write-Host "Removing $Reference in project $($f.FullName)";
        $node.ParentNode.RemoveChild($node);
        $proj.Save($f.FullName)
    }    
}

Function Write-Readme {
    if (Test-Path "$WorkingDirectory\$ProjectName\os_readme.md" -PathType leaf)
    {
    	[System.IO.File]::Copy("$WorkingDirectory\$ProjectName\os_readme.md", "$WorkingDirectory\$ProjectName\README.md", $true);
        [System.IO.File]::Delete("$WorkingDirectory\$ProjectName\os_readme.md");
    }
}

Function Write-Replacements {
    switch -Exact ($ProjectName.ToLower()) {
        "cmi-viaduc-web-core"
        {
            Write-Core-Replacements
        }
        "cmi-viaduc-web-frontend"
        {
            Write-Frontend-Replacements
        }
        "cmi-viaduc-web-management"
        {
            Write-Management-Replacements
        }
        "cmi-iiif-frontend"
        {
            Write-IIIF-Frontend-Replacements
        }
        "cmi-iiif-backend"
        {
            Write-IIIF-Backend-Replacements
        }		
        "viaduc" {
			# backend at repo root
			Write-Viaduc-Replacements

			# Temporarily point ProjectName to each client subfolder under viaduc
			$origProjectName = $ProjectName
			try {
				$ProjectName = Join-Path $origProjectName 'CMI\Web.Clients\web-core'
				Write-Core-Replacements

				$ProjectName = Join-Path $origProjectName 'CMI\Web.Clients\web-frontend'
				Write-Frontend-Replacements

				$ProjectName = Join-Path $origProjectName 'CMI\Web.Clients\web-management'
				Write-Management-Replacements
			}
			finally {
				# Always restore to keep Write-Readme and later steps safe
				$ProjectName = $origProjectName
			}
		}

        Default
        {
            Write-Error("Unbekanntes Projekt $ProjectName")
            throw "Unbekanntes Projekt $ProjektName"
        }
    }
}


Function Get-Git-Repository {
    param (
        [Parameter(Mandatory = $true)]
        [string]$GitRepoUrl,
        [Parameter(Mandatory = $true)]
        [string]$CloneFolder,
        [Parameter(Mandatory = $true)]
        [string]$BranchName
    )

    Write-Host "Cloning $GitRepoUrl to $CloneFolder"

    try {
        git clone --branch $BranchName $GitRepoUrl $CloneFolder
        if (-not $?) {
            throw "Error with git clone!"
        }
    }
    catch {
        Write-Error "Error when connecting to $GitRepoUrl"
    }

}

Function Remove-File {
    param (
        [Parameter(Mandatory = $true)]
        [string]$FilePath
    )
    Write-Host "Removing $FilePath..."
    Remove-Item $FilePath -Force -Verbose
}

Function Remove-Licenses {
    param (
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$FileName
    )
    Write-Host "Removing $FileName Files..."
    Get-ChildItem $Path -Recurse | Where-Object { $_.Name.EndsWith($FileName)} | Remove-Item -Force -Verbose
}

Function Write-Replacement {
    param (
        [Parameter(Mandatory = $true)]
        [string]$FilePath,
        [Parameter(Mandatory = $true)]
        [string]$Replacement
    )
    Write-Host "Replacing File-Content of File $FilePath with $Replacement"
    Set-Content -Path $FilePath -Force -Value $Replacement
}

Function Write-PartReplacement {
    param (
        [Parameter(Mandatory = $true)]
        [string]$FilePath,
        [Parameter(Mandatory = $true)]
        [string]$SearchForRegex,
        [Parameter(Mandatory = $true)]
        [string]$Replacement
    )
    Write-Host "Replacing Part of File $FilePath, $SearchForRegex with $Replacement"
    $content = Get-Content -Path $FilePath
    $content = $content -replace $SearchForRegex, $Replacement
    Set-Content -Path $FilePath -Force -Value $content
}

Function Compress-Directory {
    param (
        [Parameter(Mandatory = $true)]
        [string]$DirectoryToZip,
        [Parameter(Mandatory = $true)]
        [string]$ZipResultFullPath
     )

    Write-Host "Compressing Directory $DirectoryToZip, excluding nothing, to path $ZipResultFullPath"
    Get-ChildItem $DirectoryToZip | Compress-Archive -DestinationPath $ZipResultFullPath -Update

    Write-Host "finished. Find the zip here: $ZipResultFullPath"

 }

Export-All-OpenSource
