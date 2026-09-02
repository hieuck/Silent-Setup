# Build and Package Script for Silent Setup
$ErrorActionPreference = "Stop"

Write-Host "================================"
Write-Host "Silent Setup - Build & Package"
Write-Host "================================"
Write-Host ""

# Stop any running instances
Write-Host "[1/6] Stopping running instances..."
Stop-Process -Name SilentSetup -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

# Clean previous builds
Write-Host "[2/6] Cleaning previous builds..."
if (Test-Path ".\SilentSetup\bin") { Remove-Item ".\SilentSetup\bin" -Recurse -Force }
if (Test-Path ".\SilentSetup\obj") { Remove-Item ".\SilentSetup\obj" -Recurse -Force }

# Build
Write-Host "[3/6] Building Release..."
dotnet publish .\SilentSetup\SilentSetup.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o .\Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!"
    exit 1
}

# Copy resources
Write-Host "[4/6] Copying resources..."
Copy-Item "apps" "Release\" -Recurse -Force
Copy-Item "patches" "Release\" -Recurse -Force
Copy-Item "docs" "Release\" -Recurse -Force
Copy-Item "README.md" "Release\" -Force
if (Test-Path "config.json") { Copy-Item "config.json" "Release\" -Force }

# Create directories
if (-not (Test-Path "Release\cache")) { New-Item -ItemType Directory -Path "Release\cache" -Force | Out-Null }
if (-not (Test-Path "Release\logs")) { New-Item -ItemType Directory -Path "Release\logs" -Force | Out-Null }

# Get file info
Write-Host "[5/6] Gathering info..."
$exePath = "Release\SilentSetup.exe"
$fileSize = [math]::Round((Get-Item $exePath).Length / 1MB, 2)
$fileHash = (Get-FileHash $exePath -Algorithm SHA256).Hash
Write-Host "  File: SilentSetup.exe"
Write-Host "  Size: $fileSize MB"
Write-Host "  SHA256: $fileHash"

# Create ZIP
Write-Host "[6/6] Creating ZIP package..."
$tempPackage = "Release\SilentSetup"
New-Item -ItemType Directory -Path $tempPackage -Force | Out-Null
Copy-Item "Release\SilentSetup.exe" "$tempPackage\"
Copy-Item "Release\apps" "$tempPackage\" -Recurse -Force
Copy-Item "Release\patches" "$tempPackage\" -Recurse -Force
Copy-Item "Release\docs" "$tempPackage\" -Recurse -Force
Copy-Item "Release\README.md" "$tempPackage\" -Force
if (Test-Path "Release\config.json") { Copy-Item "Release\config.json" "$tempPackage\" }
New-Item -ItemType Directory -Path "$tempPackage\cache" -Force | Out-Null
New-Item -ItemType Directory -Path "$tempPackage\logs" -Force | Out-Null

$zipPath = "Release\SilentSetup-v1.0-win64.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path "$tempPackage\*" -DestinationPath $zipPath -Force
$zipSize = [math]::Round((Get-Item $zipPath).Length / 1MB, 2)
Remove-Item $tempPackage -Recurse -Force

Write-Host ""
Write-Host "================================"
Write-Host "Build Complete!"
Write-Host "================================"
Write-Host "  Executable: Release\SilentSetup.exe"
Write-Host "  Package: $zipPath ($zipSize MB)"
Write-Host ""
