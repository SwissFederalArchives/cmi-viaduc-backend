Param(
    [string]$version,
	[string]$buildNumber
)

write-host $( '##teamcity[message text=''{0}'']' -f $version ) 


$allAssemblieInfoFiles = Get-ChildItem -Recurse | Where-Object {$_.Name -eq "AssemblyInfo.cs"}

$fullVersion = $version + "." + $buildNumber

"*** Release " + $fullVersion | Out-File ".\comment.txt"

$logMessage = "New BuildNr: " + $buildNumber
write-host $( '##teamcity[message text=''{0}'']' -f $logMessage ) 

$COPYRIGHT = "2017, CM Informatik AG"


[int] $i = 1
foreach($assemblyFileInfo in $allAssemblieInfoFiles) {
    if(-not ((Get-Content $assemblyFileInfo.FullName) -eq $null)){
        (Get-Content $assemblyFileInfo.FullName) -replace '^\[assembly\: AssemblyVersion.*\)\]$', "[assembly: AssemblyVersion(`"$fullVersion`")]" | Set-Content ($assemblyFileInfo.FullName) -Encoding UTF8
        (Get-Content $assemblyFileInfo.FullName) -replace '\[assembly\: AssemblyFileVersion.*\)\]', "[assembly: AssemblyFileVersion(`"$fullVersion`")]" | Set-Content ($assemblyFileInfo.FullName) -Encoding UTF8
        
        (Get-Content $assemblyFileInfo.FullName) -replace '\[assembly\: AssemblyCopyright.*\)\]', "[assembly: AssemblyCopyright(`"$COPYRIGHT`")]" | Set-Content $assemblyFileInfo.FullName -Encoding UTF8
       
        echo ("$i -- Project: " + $assemblyFileInfo.FullName)
        $i++
    }
}
