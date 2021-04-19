#Parameters
param(
       [string]$BuildConfiguration,
       [string]$DogFoodPath,
       [string]$SolutionDir,
       [string]$BuildNumber
)

#Functions
function New-TemporaryDirectory {
    $parent = [System.IO.Path]::GetTempPath()
    $name = [System.IO.Path]::GetRandomFileName()
    $dirPath = Join-Path -Path $parent -ChildPath $name
    $tempDir = New-Item -ItemType Directory -Path $dirPath
    $tempDir.FullName 
}

Function Write-BuildLog {
	param (
		[string]$Message
	)
	write-host $( '##teamcity[message text=''{0}'']' -f $Message ) 
}

#Main-program
#Das Deployment-Skript kopieren
$tempDir = New-TemporaryDirectory

Write-BuildLog -Message "kopiere DeploymentScript"
$deploySrc = [io.path]::Combine($SolutionDir, "buildServices", "deploy.ps1")
$deployTrg = [io.path]::combine($tempDir, "deploy.ps1")
Write-BuildLog -Message $deploySrc
Write-BuildLog -Message $deployTrg

Copy-Item -Path $deploySrc -Destination  $deployTrg

Write-BuildLog -Message "Fertig"


$hostDir = [io.path]::combine($SolutionDir, 'CMI', 'Host')

# Die Debug- resp Release-Verzeichnisse ins tempDir kopieren und dabei einen
# sprechenen Namen als Zielverzeichnis verwenden: 
Get-ChildItem -Path $hostDir -Directory|
    ForEach-Object {
        $sourceDir = [io.path]::combine($_.FullName, 'bin', $BuildConfiguration)
        $destDir = [io.path]::combine($tempDir, "Services", $_.Name)
        Copy-Item $sourceDir –destination $destDir -recurse -container
    }

# das Frontend Web Verzeichnis (=Artefakt aus anderem Build) kopieren 
$artefaktDir = [io.path]::Combine($SolutionDir, "frontendartefacts")
$destDir = [io.path]::combine($tempDir, "Web", "Frontend")
Copy-Item $artefaktDir –destination $destDir -recurse -container

# das Management Web Verzeichnis (=Artefakt aus anderem Build) kopieren 
$artefaktDir = [io.path]::Combine($SolutionDir, "managementartefacts")
$destDir = [io.path]::combine($tempDir, "Web", "Management")
Copy-Item $artefaktDir –destination $destDir -recurse -container

# das UrbanCode Verzeichnis erstellen:
$urbanCodeSourceDir = [io.path]::Combine($SolutionDir, "CMI", "Tools", "UrbanCode", "bin", "Release")
$destDir = [io.path]::combine($tempDir, "UrbanCode")
Copy-Item $urbanCodeSourceDir –destination $destDir -recurse -container

# Mit unserem eigens dafür erstellten Werkzeug (CMI.Tools.UrbanCode.exe)
# die Konfig Dateien so frisieren, dass darin Platzhalter stehen statt unserer
# Werte aus der DEV Umgebung:
$urbanCodeExe = [io.path]::Combine($SolutionDir, "CMI", "Tools", "UrbanCode", "bin", "Release", "CMI.Tools.UrbanCode.exe")
& $urbanCodeExe t $tempDir

# Dokumentatonsdatei (Beschreibung der Parameter) erstellen:
$documentationHtmlFile = [io.path]::Combine($tempDir, "UrbanCode", "Config-Dokumentation.html");
& $urbanCodeExe d $tempDir $documentationHtmlFile

# packen und ins Dogfood kopieren
Add-Type -assembly "system.io.compression.filesystem"
$completePath = [io.path]::combine($DogFoodPath, "Complete")
 
# verzeichnis anlegen, wenn es noch nicht existiert
if (![System.IO.Directory]::Exists($completePath ))
{
     New-Item -ItemType Directory -Force -Path $completePath
}

$zipPath = [io.path]::combine($completePath, "viaduc-complete-$BuildNumber.zip")
[io.compression.zipfile]::CreateFromDirectory($tempDir, $zipPath)

# octopus-package erstellen
$artefaktDir = [io.path]::Combine($SolutionDir, "octopusartefacts")

# verzeichnis anlegen, wenn es noch nicht existiert
if (![System.IO.Directory]::Exists($artefaktDir))
{
     New-Item -ItemType Directory -Force -Path $artefaktDir
}
$artefaktDir = [io.path]::Combine($artefaktDir, "viaduc.complete.$BuildNumber.zip")

[io.compression.zipfile]::CreateFromDirectory($tempDir, $artefaktDir)

# das Temp-Vereichnis löschen
Remove-Item $tempDir -Recurse

