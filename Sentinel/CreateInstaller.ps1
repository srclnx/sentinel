$tempDirectoryName = ".\__squirrel_temp__"

if ( $DTE -eq $null ) {
	echo "Run this from the NuGet Package Console inside VS"
	exit 1
}

mkdir -Path $tempDirectoryName
rm -Recurse -Force "$tempDirectoryName\*.nupkg"
NuGet pack .\sentinel.sln -outputDirectory $tempDirectoryName
ls "$tempDirectoryName\*.nupkg" | % { Squirrel --releasify $_ -p .\packages }
