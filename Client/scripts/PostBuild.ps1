param (
	[Parameter(Mandatory=$true)][string]$outputDir
)

Add-Type -AssemblyName System.IO.Compression.FileSystem

$downloadZipName = "SDL2-2.0.8-win32-x64.zip"
$downloadURL = ("https://www.libsdl.org/release/{0}" -f $downloadZipName)
$dllFile = "SDL2.dll"
$pathToZip = ("{0}/{1}" -f $outputDir, $downloadZipName)
$pathToDLL = ("{0}/{1}" -f $outputDir, $dllFile)

if (!(Test-Path -path $pathToDLL)) {
	Write-Host "Downloading SDL runtime DLLs to" $outputDir
	
    Write-Host $downloadURL " and " $pathToZip
	(New-Object System.Net.WebClient).DownloadFile($downloadURL, $pathToZip)

    Write-Host $pathToZip " and " $outputDir
	[System.IO.Compression.ZipFile]::ExtractToDirectory($pathToZip, $outputDir)

	Remove-Item $pathToZip

	if (!(Test-Path -path $pathToDLL)) {
		Write-Host "Error downloading required DLLs"
		exit 1
	}
}
