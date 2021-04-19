Param(
    [string]$version,
	[string]$buildNumber,
	[string]$branch
)

cd $PSScriptRoot

$fullVersion = $version + "." + $buildNumber

try{
	git commit -am "*** Release $fullVersion"
    git push origin $branch
}catch{

}