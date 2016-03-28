$tempDirectoryName = ".\__squirrel_temp__"

if ( $DTE -eq $null ) {
	echo "Run this from the NuGet Package Console inside VS"
	exit 1
}

mkdir -Path $tempDirectoryName -ErrorAction SilentlyContinue
rm -Recurse -Force "$tempDirectoryName\*.nupkg"

# Todo: auto pick up version number
NuGet pack .\Sentinel.1.0.0.nuspec -OutputDirectory "$tempDirectoryName" -verbose
ls "$tempDirectoryName\*.nupkg" | %{Squirrel --releasify $_ -p .\packages -r .\Releases}
# rm -r -fo "$tempDirectoryName"