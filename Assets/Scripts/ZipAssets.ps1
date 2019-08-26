param (
   [Parameter(Mandatory=$true)][string]$folder,
   [Parameter(Mandatory=$true)][string]$outputFile
)

# Because `Compress-Archive` requires the extension to be a .zip (why...) we
# have to work around it by making it create the zip file, and then renaming
# the zip to pk3. Since the script will be calling it with a pk3 extension,
# all we have to do is convert the pk3 extension to a zip, invoke it with the
# zip extension, and then convert it back to a pk3.
$zipPath = [io.path]::ChangeExtension($outputFile, ".zip")

if (Test-Path -Path $zipPath) {
    Remove-Item $zipPath
}

if (Test-Path -Path $outputFile) {
    Remove-Item $outputFile
}

Compress-Archive -Path $folder -CompressionLevel Optimal -DestinationPath $zipPath
Rename-Item $zipPath $outputFile
