#Requires -RunAsAdministrator

param(
    [String]$ServicesRootDir,         <# Speicherort für alle Viaduc Services, z.B. C:\Viaduc Services #>
    [String[]]$ServicesToInstall,     <# Liste der zu installierenden Services: Asset, Cache, DataFeeed, etc #>
    [String]$WebSitesDir,             <# Speicherort für alle WebSites, z.B. C:\Viaduc Services #>
    [String[]]$WebApplicationsToInstall,     <# Liste der zu installierenden Services: Frontend, Management, etc #>
    [String]$BackupDir,                <# Verzeichnis, in welches die bestehenden binaries vor dem Update gebackupt werden. Wenn leer -> automatisch #>   
    [String]$StageParameterFile       <# Pfad, zum Parameterfile für die Anpassung der Variablen in den Config-Files #>
)

Function Main
{
	if([System.String]::IsNullOrWhiteSpace($StageParameterFile))
	{
		Write-Error "Kein StageParameterFile angegeben."
		return
	}

    if([System.String]::IsNullOrWhiteSpace($BackupDir))
    {
        $tempDir   = [System.IO.Path]::GetTempPath()        
        $BackupDir = [System.IO.Path]::Combine($tempDir, "Backup")
        Write-Output "Kein Backup-Verzeichnis angegeben. Es wird das Verzeichnis $BackupDir verwendet"
    }
    Write-Output "Deployment wird gestartet..."

    if($ServicesToInstall.Length -gt 0)
    {
        Install-Services
    }

    if ($WebApplicationsToInstall.Length -gt 0)
    {
        Install-WebApplications
    }

    Write-Output "Deployment beendet."
}

Function Install-Services
{
    if ([System.String]::IsNullOrWhitespace($ServicesRootDir))
    {
        throw "ServicesRootDir wurde nicht angegeben."
    }
    if ($false -eq [System.IO.Directory]::Exists($ServicesRootDir))
    {
        throw "Das angegebene ServicesRootDir wurde nicht gefunden." 
    }

    $ServicesToInstall | foreach {Install-Service -ServiceToInstall $_}
}

Function Install-Service
{
    param(
        [String]$ServiceToInstall
    )

    Write-Output ""
    Write-Output "Service ${ServiceToInstall} wird installiert..."
    
    $serviceId = "CMI${ServiceToInstall}Service"
    $windowsService = Get-Service $serviceId -ErrorAction 'silentlycontinue'

    if($windowsService -eq $null)
    {
        Write-Output "Ein existierender Service mit er ID $serviceId wurde nicht gefunden."
    }
    else
    {
        Write-Output "Der Service $serviceId wurde gefunden."
        if ($windowsService.Status -ne "Stopped") 
        {
            Stop-Service $windowsService.Name
            Write-Output "Der Service $serviceId wurde gestoppt."
        }
        else
        {
            $statusText = $windowsService.Status
            Write-Output "Der Service $windowsService  ist im Status $statusText"
        }


        
    }

    Backup-Service -ServiceToBackup $ServiceToInstall
    $destDir = GetServiceInstallDir -service $ServiceToInstall
    DeleteContentsExcept -dir $destDir -except @("Parameters")
    Copy-ServiceFiles $ServiceToInstall

    if([System.String]::IsNullOrWhiteSpace($StageParameterFile) -eq $false)
    {
        #Parameter file
        $UrbanExe = [System.IO.Path]::Combine($PSScriptRoot,"UrbanCode","CMI.Tools.UrbanCode.exe")
        & $UrbanExe e $destDir $StageParameterFile 
    }


    if($windowsService -eq $null)
    {
        Register-Service -Service $ServiceToInstall
    }
    
    Start-Service $serviceId
}

Function Register-Service 
{
    param(
            [String]$Service
    )
    
    $serviceInstallDir = GetServiceInstallDir -service $service
    $exe = "CMI.Host.${service}.exe"

    $pathToServiceExe = [System.IO.Path]::Combine($serviceInstallDir, $exe)

    Write-Output "Service $service wird registriert..."

    if ($false -eq [System.IO.File]::Exists($pathToServiceExe))
    {
        Write-Output "Not found"
    }

    & "$pathToServiceExe" "install"

    Write-Output "Service wurde registriert."
}

Function GetServiceInstallDir
{
    param(
            [String]$service
    )
    return [System.Io.Path]::Combine($ServicesRootDir, $service)
}


Function Backup-Service
{
    param(
        [String]$ServiceToBackup
    )

    $source = GetServiceInstallDir -service  $ServiceToBackup

    if ([System.IO.Directory]::Exists($source))
    {
        $date = Get-Date -Format "yyyy-MM-dd HH.mm.ss"
        
        $destination = [System.Io.Path]::Combine($BackupDir, "$ServiceToBackup", $date)
        Write-Output "Erstelle Backup von $source -> $destination"
        Copy-Item -Path $source -Destination $destination -Recurse -Container
        Write-Output "Backup Beendet"
    }
    else{
    
        Write-Output "Kein Backup wird erstellt, weil das Verzeichnis $source nicht existiert."
    }
}

Function Copy-ServiceFiles
{
    param(
        [String]$Service
    )

    $source = Get-SourceDir -Dir "Services" -SubDir $Service 
    $target = $ServicesRootDir

    Write-Output "Dateien werden kopiert $source => $target ..."

    Copy-Item $source $ServicesRootDir -Recurse -Container -Force
    Write-Output "die Dateien wurden kopiert."
    
}

Function Get-SourceDir
{
    param(
        [String]$Dir,
        [String]$SubDir
    )
   
    $dir = [System.IO.Path]::Combine($PSScriptRoot, $Dir, $SubDir)
    return $dir
}

Function DeleteContentsExcept([string]$dir, [string[]]$except)
{
    Write-Output "Inhalt des Ordners $dir wird gelöscht, ausser $except."
    Get-ChildItem -Path $dir -Exclude $except | ForEach-Object {
        
            Remove-Item $_ -Force -Recurse
    }
}



Function Install-WebApplications
{
    if ([System.String]::IsNullOrWhitespace($WebSitesDir))
    {
        throw "WebSitesDir wurde nicht angegeben."
    }
    if ($false -eq [System.IO.Directory]::Exists($WebSitesDir))
    {
        throw "Das angegebene WebSitesDir wurde nicht gefunden." 
    }

	if ($WebApplicationsToInstall.Count -gt 0) {
		Import-Module WebAdministration
		StopAppPools

		$WebApplicationsToInstall | foreach {Install-WebApplication -Web $_}
   
		StartAppPools	
	}
}

Function StopAppPools {
	Get-ChildItem IIS:\AppPools | where {$_.state -eq "Started"} | Stop-WebAppPool
}

Function StartAppPools {
	Get-ChildItem IIS:\AppPools | where {$_.state -eq "Stopped"} | Start-WebAppPool
}


Function RecycleAllAppPools
{
    # http://adicodes.com/powershell-script-recycle-all-application-pools/
    & $env:windir\system32\inetsrv\appcmd list apppools /state:Started /xml | & $env:windir\system32\inetsrv\appcmd recycle apppools /in
}

Function Install-WebApplication
{
    param(
        [String]$Web
    )

    Write-Output ""
    Write-Output "Webapplikation ${Web} wird installiert..."
    
    Backup-Web -Web $Web

    $destDir = GetWebInstallDir -Web $Web
    
    DeleteContentsExcept -dir $destDir -except @("App_Data")
    Copy-WebFiles -Web $Web
}

Function Backup-Web
{
    param(
        [String]$Web
    )

    $source = GetWebInstallDir -Web $Web

    if ([System.IO.Directory]::Exists($source))
    {
        $date = Get-Date -Format "yyyy-MM-dd HH.mm.ss"
        $destination = [System.Io.Path]::Combine($BackupDir, "$web", $date)
        Write-Output "Erstelle Backup von $source -> $destination"
        Copy-Item -Path $source -Destination $destination -Recurse -Container
        Write-Output "Backup Beendet"
    }
    else{
    
        Write-Output "Kein Backup wird erstellt, weil das Verzeichnis $source nicht existiert."
    }
}

Function GetWebInstallDir
{
    param(
            [String]$Web
    )
    return [System.Io.Path]::Combine($WebSitesDir, $Web)
}

Function Copy-WebFiles
{
    param(
        [String]$Web
    )

    $source = Get-SourceDir -Dir "Web" -SubDir $Web
    $target = $WebSitesDir

    Write-Output "Dateien werden kopiert $source => $target ..."

    Copy-Item $source $target -Recurse -Container -Force
    Write-Output "die Dateien wurden kopiert."
    
}


Main
