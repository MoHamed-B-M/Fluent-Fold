# =============================================================================
# Bulk File Organizer - Local Build Script (Windows)
# =============================================================================
# Usage:
#   .\build_local.ps1              # Build portable .exe (default)
#   .\build_local.ps1 -Clean       # Clean previous builds first
#   .\build_local.ps1 -InstallMsi  # Also build MSI installer (requires WiX)
#
# Prerequisites:
#   - Python 3.9+
#   - pip install -r requirements.txt
#   - pip install git+https://github.com/zhiyiYo/PyQt-Fluent-Widgets.git
#   - pip install pyinstaller
#   - (optional) WiX Toolset v3.14+ for MSI: choco install wixtoolset
# =============================================================================

param(
    [switch]$Clean,
    [switch]$InstallMsi
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$AppName = "FluentFold"
$Version = "1.0.0"

# Try to get version from git tag
try {
    $tag = git -C $ScriptDir describe --tags --abbrev=0 2>$null
    if ($tag -and ($tag -match '^v(.+)$')) {
        $v = $matches[1]
        if ($v -match '^\d+\.\d+\.\d+(\.\d+)?$') { $Version = $v }
    }
} catch {}

Write-Host "==============================" -ForegroundColor Cyan
Write-Host " Fluent Fold Builder" -ForegroundColor Cyan
Write-Host " Version: $Version" -ForegroundColor Cyan
Write-Host "==============================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Clean previous builds
if ($Clean -and (Test-Path "$ScriptDir\dist")) {
    Write-Host "🧹 Cleaning previous builds..." -ForegroundColor Yellow
    Remove-Item -Path "$ScriptDir\dist" -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -Path "$ScriptDir\build" -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -Path "$ScriptDir\*.spec" -Recurse -Force -ErrorAction SilentlyContinue
}

# Step 2: Verify dependencies
Write-Host "📦 Checking dependencies..." -ForegroundColor Yellow

$deps = @("PySide6", "qfluentwidgets", "pyinstaller")
foreach ($dep in $deps) {
    try {
        python -c "import $dep" 2>$null
        Write-Host "   ✅ $dep"
    } catch {
        Write-Host "   ❌ $dep not found. Run: pip install $dep" -ForegroundColor Red
        exit 1
    }
}

# Step 3: Verify organizer import
try {
    python -c "from organizer import FileOrganizer; print('   ✅ organizer.py')"
} catch {
    Write-Host "   ❌ organizer.py import failed: $_" -ForegroundColor Red
    exit 1
}

# Step 4: Build with PyInstaller
Write-Host ""
Write-Host "🔨 Building executable with PyInstaller..." -ForegroundColor Yellow

$pyinstallerArgs = @(
    "--onedir",
    "--windowed",
    "--name", $AppName,
    "--exclude", "PyQt5",
    "--exclude", "PyQt5.QtCore",
    "--exclude", "PyQt5.QtGui",
    "--exclude", "PyQt5.QtWidgets",
    "--exclude", "PyQt5.sip",
    "--exclude", "PyQt5.QtNetwork",
    "--exclude", "PyQt5-Frameless-Window",
    "--hidden-import", "organizer",
    "--add-data", "organizer.py;.",
    "--noconfirm"
)

Set-Location -Path $ScriptDir
& pyinstaller $pyinstallerArgs main.py

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}

# Build summary
$exePath = "$ScriptDir\dist\$AppName\$AppName.exe"
if (Test-Path $exePath) {
    $fileInfo = Get-Item $exePath
    Write-Host ""
    Write-Host "✅ Build successful!" -ForegroundColor Green
    Write-Host "   Executable: $exePath"
    Write-Host "   Size: $([math]::Round($fileInfo.Length / 1MB, 2)) MB"
} else {
    Write-Host "❌ Executable not found at expected path!" -ForegroundColor Red
    exit 1
}

# Step 5: Create ZIP archive
Write-Host ""
Write-Host "📦 Creating portable ZIP archive..." -ForegroundColor Yellow
$zipPath = "$ScriptDir\dist\$AppName.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path "$ScriptDir\dist\$AppName\*" -DestinationPath $zipPath -CompressionLevel Optimal
Write-Host "   ✅ Created: $zipPath"

# Step 6: Build MSI installer (optional)
if ($InstallMsi) {
    Write-Host ""
    Write-Host "📀 Building MSI installer..." -ForegroundColor Yellow

    $srcDir = (Get-Item "$ScriptDir\dist\$AppName").FullName

    if (-not (Test-Path "$ScriptDir\build_msi.ps1")) {
        Write-Host "   ⚠️  build_msi.ps1 not found. Skipping MSI build." -ForegroundColor Yellow
    } else {
        & "$ScriptDir\build_msi.ps1" -SourceDir $srcDir -Version $Version -OutputDir "$ScriptDir\dist"
        if ($LASTEXITCODE -eq 0) {
            Write-Host "   ✅ MSI build completed" -ForegroundColor Green
        } else {
            Write-Host "   ⚠️  MSI build failed or WiX not found" -ForegroundColor Yellow
        }
    }
}

# Summary
Write-Host ""
Write-Host "==============================" -ForegroundColor Cyan
Write-Host " Build Complete!" -ForegroundColor Green
Write-Host "==============================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Output Directory: $ScriptDir\dist\" -ForegroundColor White
Write-Host "  📁 $AppName\          - Portable folder (run $AppName.exe)" -ForegroundColor White
Write-Host "  📦 $AppName.zip       - Portable ZIP archive" -ForegroundColor White

if ($InstallMsi) {
    Write-Host "  📀 $AppName-$Version.msi - Windows installer" -ForegroundColor White
}

Write-Host ""
Write-Host "To run: .\dist\$AppName\$AppName.exe" -ForegroundColor Green
Write-Host ""