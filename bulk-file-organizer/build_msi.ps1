# =============================================================================
# Build MSI Installer for Fluent Fold
# Requires: WiX Toolset v3.14+ (choco install wixtoolset)
# Usage: .\build_msi.ps1 -SourceDir "dist\FluentFold" -Version "1.0.0"
# =============================================================================

param(
    [Parameter(Mandatory = $true)]
    [string]$SourceDir,

    [Parameter(Mandatory = $false)]
    [string]$Version = "1.0.0",

    [Parameter(Mandatory = $false)]
    [string]$OutputDir = ""
)

$ErrorActionPreference = "Stop"
$AppName = "FluentFold"

if (-not $OutputDir) {
    $OutputDir = (Get-Item $SourceDir).Parent.FullName
}

if (-not (Test-Path $SourceDir)) {
    Write-Error "Source directory not found: $SourceDir"
    exit 1
}

$msiPath = Join-Path $OutputDir "$AppName-$Version.msi"

# Find WiX Toolset
$wixPaths = @(
    "${env:ProgramFiles(x86)}\WiX Toolset v3.14\bin",
    "${env:ProgramFiles(x86)}\WiX Toolset v3.11\bin",
    "${env:ProgramFiles}\WiX Toolset v3.14\bin",
    "${env:ProgramFiles}\WiX Toolset v3.11\bin"
)

$wixPath = $null
foreach ($p in $wixPaths) {
    if (Test-Path "$p\candle.exe") {
        $wixPath = $p
        break
    }
}

if (-not $wixPath) {
    Write-Error "WiX Toolset not found. Install with: choco install wixtoolset"
    exit 1
}

Write-Host "Found WiX at: $wixPath" -ForegroundColor Green
Write-Host "Building MSI for $AppName v$Version..." -ForegroundColor Yellow

$guid = [guid]::NewGuid().ToString("D")
$appDir = (Get-Item $SourceDir).FullName

# Collect all files for WiX
$componentLines = @()
$refLines = @()

Get-ChildItem -Path $appDir -Recurse -File | ForEach-Object {
    $relPath = $_.FullName.Substring($appDir.Length + 1)
    if ($relPath -eq "$AppName.exe") {
        $componentLines += @"
                <Component Id="MainExecutable" Guid="*">
                    <File Id="MainExe" Name="$AppName.exe" Source="$appDir\$AppName.exe" KeyPath="yes" />
                    <Shortcut Id="StartMenuShortcut"
                              Directory="ProgramMenuDir"
                              Name="$AppName"
                              WorkingDirectory="APPLICATIONFOLDER"
                              Advertise="yes" />
                </Component>
"@
        $refLines += '                <ComponentRef Id="MainExecutable" />'
    } else {
        $fileId = [System.IO.Path]::GetFileNameWithoutExtension($relPath) -replace '[^a-zA-Z0-9]', '_'
        if ([string]::IsNullOrEmpty($fileId)) { $fileId = "file_$([System.IO.Path]::GetFileNameWithoutExtension($relPath).GetHashCode().ToString().Replace('-','_'))" }
        $fileName = [System.IO.Path]::GetFileName($relPath)
        $fileSource = "$appDir\$relPath"
        $componentLines += @"
                <Component Id="$fileId" Guid="*">
                    <File Id="$fileId" Name="$fileName" Source="$fileSource" KeyPath="yes" />
                </Component>
"@
        $refLines += "                <ComponentRef Id=""$fileId"" />"
    }
}

$componentBody = $componentLines -join "`n"
$refBody = $refLines -join "`n"

$wxsContent = @"
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://schemas.microsoft.com/wix/2006/wi">
    <Product Id="*"
             Name="$AppName"
             Language="1033"
             Version="$Version"
             Manufacturer="Fluent Fold"
             UpgradeCode="$guid">
        <Package InstallerVersion="200"
                 Compressed="yes"
                 InstallScope="perMachine"
                 Platform="x64" />
        <MajorUpgrade DowngradeErrorMessage="A newer version of [ProductName] is already installed." />
        <MediaTemplate EmbedCab="yes" CompressionLevel="high" />
        <Directory Id="TARGETDIR" Name="SourceDir">
            <Directory Id="ProgramFiles64Folder">
                <Directory Id="APPLICATIONFOLDER" Name="$AppName">
$componentBody
                    <Component Id="ApplicationFolder" Guid="*">
                        <CreateFolder />
                        <RemoveFile Id="PurgeAppFolder" Name="*.*" On="uninstall" />
                    </Component>
                </Directory>
            </Directory>
            <Directory Id="ProgramMenuFolder">
                <Directory Id="ProgramMenuDir" Name="$AppName" />
            </Directory>
        </Directory>
        <Feature Id="MainFeature" Title="$AppName" Level="1">
$refBody
            <ComponentRef Id="ApplicationFolder" />
        </Feature>
        <UI>
            <UIRef Id="WixUI_Minimal" />
        </UI>
        <Property Id="WIXUI_INSTALLDIR" Value="APPLICATIONFOLDER" />
    </Product>
</Wix>
"@

$wxsFile = Join-Path $OutputDir "installer.wxs"
$wxsContent | Out-File -FilePath $wxsFile -Encoding utf8

Write-Host "Compiling WiX source..." -ForegroundColor Yellow
& "$wixPath\candle.exe" $wxsFile -out (Join-Path $OutputDir "installer.wixobj")
if ($LASTEXITCODE -ne 0) { throw "Candle compilation failed" }

Write-Host "Linking MSI installer..." -ForegroundColor Yellow
& "$wixPath\light.exe" -ext WixUIExtension (Join-Path $OutputDir "installer.wixobj") -out $msiPath
if ($LASTEXITCODE -ne 0) { throw "Light linking failed" }

# Cleanup temp files
Remove-Item $wxsFile -Force -ErrorAction SilentlyContinue
Remove-Item (Join-Path $OutputDir "installer.wixobj") -Force -ErrorAction SilentlyContinue

$msiInfo = Get-Item $msiPath
Write-Host "MSI created: $msiPath ($([math]::Round($msiInfo.Length / 1MB, 2)) MB)" -ForegroundColor Green
