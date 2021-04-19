param(
	[string]$ClientPath,
	[string]$PublishOutput,
	[string]$BuildNumber
)

function WriteTeamCityError {
	param([string]$errorMessage)

	$errorMessage = ("" + $errorMessage).Replace("`n", "|n")
	$errorMessage = ("" + $errorMessage).Replace("\", "|\")
	$errorMessage = ("" + $errorMessage).Replace("/", "|/")
	$errorMessage = ("" + $errorMessage).Replace("'", "|'")

	Write-host "##teamcity[message text='$errorMessage' status='FAILURE']"
	Write-host "##teamcity[buildProblem description='$errorMessage']"

	exit 1
}


$targetPath = Join-Path $PublishOutput "client"
Write-Host "Copy " + $ClientPath + " To " + $targetPath
cp -R $ClientPath $targetPath

#cp -R P:\work\CMI.Viaduc.Client\build\dev\ C:\Temp\Release3\_PublishedWebsites\CMI.Web.Frontend\client33\