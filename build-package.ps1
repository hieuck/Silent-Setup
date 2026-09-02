# Build and Package Script for Silent Setup
# Creates a distribution-ready package

$ErrorActionPreference = "Stop"

Write-Host "================================" -ForegroundColor Cyan
Write-Host "Silent Setup - Build & Package" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

# 1. Clean previous builds
Write-Host "[1/5] Cleaning previous builds..." -ForegroundColor Yellow
if (Test-Path ".\SilentSetup\bin") { Remove-Item ".\SilentSetup\bin" -Recurse -Force }
if (Test-Path ".\SilentSetup\obj") { Remove-Item ".\SilentSetup\obj" -Recurse -Force }
if (Test-Path ".\Release") { Remove-Item ".\Release" -Recurse -Force }
Write-Host "✓ Clean complete" -ForegroundColor Green

# 2. Build Release
Write-Host ""
Write-Host "[2/5] Building Release..." -ForegroundColor Yellow
dotnet publish .\SilentSetup\SilentSetup.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o .\Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Build complete" -ForegroundColor Green

# 3. Create Release directory structure
Write-Host ""
Write-Host "[3/5] Creating package structure..." -ForegroundColor Yellow

# Copy configuration and directories
if (Test-Path "config.json") { Copy-Item "config.json" "Release\" -Force }
Copy-Item "apps" "Release\" -Recurse -Force
Copy-Item "patches" "Release\" -Recurse -Force
Copy-Item "docs" "Release\" -Recurse -Force
Copy-Item "README.md" "Release\" -Force

# Create empty directories if they don't exist
if (-not (Test-Path "Release\cache")) { New-Item -ItemType Directory -Path "Release\cache" -Force | Out-Null }
if (-not (Test-Path "Release\logs")) { New-Item -ItemType Directory -Path "Release\logs" -Force | Out-Null }

Write-Host "✓ Package structure created" -ForegroundColor Green

# 4. Get version and file info
Write-Host ""
Write-Host "[4/5] Gathering info..." -ForegroundColor Yellow
$exePath = "Release\SilentSetup.exe"
$fileSize = [math]::Round((Get-Item $exePath).Length / 1MB, 2)
$fileHash = (Get-FileHash $exePath -Algorithm SHA256).Hash

Write-Host "  File: SilentSetup.exe" -ForegroundColor Cyan
Write-Host "  Size: $fileSize MB" -ForegroundColor Cyan
Write-Host "  SHA256: $fileHash" -ForegroundColor Cyan

# 5. Create ZIP package
Write-Host ""
Write-Host "[5/5] Creating ZIP package..." -ForegroundColor Yellow

# Create a temp folder for packaging
$tempPackage = "Release\SilentSetup"
New-Item -ItemType Directory -Path $tempPackage -Force | Out-Null

# Copy everything to temp folder
Copy-Item "Release\SilentSetup.exe" "$tempPackage\"
Copy-Item "Release\apps" "$tempPackage\" -Recurse -Force
Copy-Item "Release\patches" "$tempPackage\" -Recurse -Force
Copy-Item "Release\docs" "$tempPackage\" -Recurse -Force
Copy-Item "Release\README.md" "$tempPackage\" -Force
if (Test-Path "Release\config.json") { Copy-Item "Release\config.json" "$tempPackage\" }
New-Item -ItemType Directory -Path "$tempPackage\cache" -Force | Out-Null
New-Item -ItemType Directory -Path "$tempPackage\logs" -Force | Out-Null

# Create ZIP from temp folder
$zipPath = "Release\SilentSetup-v1.0-win64.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path "$tempPackage\*" -DestinationPath $zipPath -Force
$zipSize = [math]::Round((Get-Item $zipPath).Length / 1MB, 2)

# Clean up temp folder
Remove-Item $tempPackage -Recurse -Force

Write-Host "✓ Package created: $zipPath ($zipSize MB)" -ForegroundColor Green

# Summary
Write-Host ""
Write-Host "================================" -ForegroundColor Cyan
Write-Host "Build Complete!" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Distribution files:" -ForegroundColor White
Write-Host "  → Release\SilentSetup.exe (standalone)" -ForegroundColor Yellow
Write-Host "  → $zipPath (full package)" -ForegroundColor Yellow
Write-Host ""
Write-Host "Next steps:" -ForegroundColor White
Write-Host "  1. Test the executable in Release\" -ForegroundColor Gray
Write-Host "  2. Create GitHub release" -ForegroundColor Gray
Write-Host "  3. Upload $zipPath as release asset" -ForegroundColor Gray
Write-Host ""
