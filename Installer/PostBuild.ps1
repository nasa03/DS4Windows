#$env:APPVEYOR_BUILD_FOLDER = "c:/dev/Vapour"
#$env:APPVEYOR_BUILD_VERSION = 1.0.1
#$env:PlATFORM = "x64"

$publishFolder = $env:APPVEYOR_BUILD_FOLDER + "/Publish/" + $env:PlATFORM
$artifacts = $env:APPVEYOR_BUILD_FOLDER + "/artifacts"
$installerDirectory = $env:APPVEYOR_BUILD_FOLDER + "/Installer"
$installerProject = $installerDirectory + "/Vapour.Installer." + $env:PlATFORM + ".aip"
$installerOutputExe = $installerDirectory + "/Setup Files " + $env:PlATFORM + "/Vapour Setup (" + $env:PlATFORM + ").exe"
$installerOutputArtifact = $artifacts + "/Vapour Setup (" + $env:PlATFORM + ").exe"
$publishOutputArtifact = $artifacts + "/Publish.zip"
$aiInstallRoot = (Get-ItemProperty -Path "Registry::HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Caphyon\Advanced Installer" -Name "InstallRoot").InstallRoot
$adi = "$aiInstallRoot\bin\x86\AdvancedInstaller.com"
$serial = $installerDirectory + "/AppVeyor_ProductKey.txt"

if (Test-Path -Path $artifacts)
{
	Remove-Item -Path $artifacts -Recurse
}

if ($env:APPVEYOR_REPO_TAG -eq "true")
{
	& "$adi" /register $(gc $serial)
	& "$adi" /edit $installerProject /SetVersion $env:APPVEYOR_BUILD_VERSION
	& "$adi" /build $installerProject 

	Move-Item -Path $installerOutputExe -Destination $installerOutputArtifact
}

New-Item $artifacts -itemtype directory
Compress-Archive -Path $publishFolder -DestinationPath $publishOutputArtifact
