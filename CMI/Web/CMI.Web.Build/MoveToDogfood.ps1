#Parameters
param(
	[string]$BuildName,
	[string]$BuildOutput,
	[string]$DogFoodPath,
	[string]$FilesToMove
)

#Functions
function moveToDogFood{
	param(
		[string]$path,
		[string]$targetPath
	)

	Write-Host "path $path\n"
	Write-Host "targetPath $targetPath\n"

	if((Test-Path -Path $path) -eq $true){
		Write-Host "`nMoving Files to Dogfood ($targetPath)"
		Get-ChildItem -Path $path | Copy-Item -Recurse -Destination $targetPath
	}
}

#Main-program
$ManagementDllPath = "$BuildOutput\bin\CMI.Web.$($BuildName).dll"
$buildNumber = ((Get-Item $ManagementDllPath).VersionInfo.FileVersion)
$targetPath = Join-Path $DogFoodPath -ChildPath $BuildName | Join-Path -ChildPath "$BuildName_$buildNumber"
	
if((Test-Path $targetPath -PathType Container)) {
	for($i = 0; $true; $i++) {
		$temp = $targetPath + "-" + $i
		if(-not (Test-Path $temp -PathType Container)) {
			$targetPath = $temp
			break
		}
	}
}

if(-not (Test-Path $targetPath -PathType Container)) {
	New-Item -ItemType directory -Path $targetPath
}

moveToDogFood $FilesToMove (Get-Item $targetPath | Select-Object -ExpandProperty FullName)