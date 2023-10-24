# Get 7Zip path
if ($PSVersionTable.Platform -eq "Win32NT")
{
    # Windows
    $zip = "C:\Program Files\7-Zip\7z.exe"
}
elseif (($PSVersionTable.Platform -eq "Unix") -and ($PSVersionTable.OS.Contains("Darwin") -eq $true))
{
    # Mac
    $zip = "~/Downloads/7z2107-mac/7zz"
}

# Get version from the project file
$projXML = [xml](Get-Content -Path .\observe-entity-explorer.csproj)
$version = $projXML.SelectNodes("Project/PropertyGroup/Version")."#text"
$version

cd "bin/Publish/win-x64"
& $zip a "../../../../entity-explorer-releases/$version/observe-entity-explorer.win-x64.$version.zip" '@../../../ReleaseIncludes/listfile.win.txt'

cd "../osx-x64"
& $zip a "../../../../entity-explorer-releases/$version/observe-entity-explorer.osx-x64.$version.zip" '@../../../ReleaseIncludes/listfile.osx.txt'

cd "../osx-arm64"
& $zip a "../../../../entity-explorer-releases/$version/observe-entity-explorer.osx-arm64.$version.zip" '@../../../ReleaseIncludes/listfile.osx.txt'

cd "../linux-x64"
& $zip a "../../../../entity-explorer-releases/$version/observe-entity-explorer.linux-x64.$version.zip" '@../../../ReleaseIncludes/listfile.linux.txt'

cd "../../.."
