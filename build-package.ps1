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
Set-Location .\SilentSetup
dotnet publish -c Release -r win-x64 --self-contained /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Build failed!" -ForegroundColor Red
    exit 1
}
Set-Location ..
Write-Host "✓ Build complete" -ForegroundColor Green

# 3. Create Release directory structure
Write-Host ""
Write-Host "[3/5] Creating package structure..." -ForegroundColor Yellow
New-Item -ItemType Directory -Path "Release\SilentSetup" -Force | Out-Null

# Copy executable
Copy-Item ".\SilentSetup\bin\Release\net8.0-windows\win-x64\publish\SilentSetup.exe" "Release\SilentSetup\"

# Copy configuration and directories
Copy-Item "config.json" "Release\SilentSetup\" -ErrorAction SilentlyContinue
Copy-Item "apps" "Release\SilentSetup\" -Recurse -Force
Copy-Item "patches" "Release\SilentSetup\" -Recurse -Force
Copy-Item "docs" "Release\SilentSetup\" -Recurse -Force
Copy-Item "README.md" "Release\SilentSetup\" -Force

# Create empty directories
New-Item -ItemType Directory -Path "Release\SilentSetup\cache" -Force | Out-Null
New-Item -ItemType Directory -Path "Release\SilentSetup\logs" -Force | Out-Null

Write-Host "✓ Package structure created" -ForegroundColor Green

# 4. Get version and file info
Write-Host ""
Write-Host "[4/5] Gathering info..." -ForegroundColor Yellow
$exePath = "Release\SilentSetup\SilentSetup.exe"
$fileSize = [math]::Round((Get-Item $exePath).Length / 1MB, 2)
$fileHash = (Get-FileHash $exePath -Algorithm SHA256).Hash

Write-Host "  File: SilentSetup.exe" -ForegroundColor Cyan
Write-Host "  Size: $fileSize MB" -ForegroundColor Cyan
Write-Host "  SHA256: $fileHash" -ForegroundColor Cyan

# 5. Create ZIP package
Write-Host ""
Write-Host "[5/5] Creating ZIP package..." -ForegroundColor Yellow
$zipPath = "Release\SilentSetup-v1.0-win64.zip"
Compress-Archive -Path "Release\SilentSetup\*" -DestinationPath $zipPath -Force
$zipSize = [math]::Round((Get-Item $zipPath).Length / 1MB, 2)
Write-Host "✓ Package created: $zipPath ($zipSize MB)" -ForegroundColor Green

# Summary
Write-Host ""
Write-Host "================================" -ForegroundColor Cyan
Write-Host "Build Complete!" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Distribution files:" -ForegroundColor White
Write-Host "  → Release\SilentSetup\" -ForegroundColor Yellow
Write-Host "  → $zipPath" -ForegroundColor Yellow
Write-Host ""
Write-Host "Next steps:" -ForegroundColor White
Write-Host "  1. Test the executable in Release\SilentSetup\" -ForegroundColor Gray
Write-Host "  2. Create GitHub release" -ForegroundColor Gray
Write-Host "  3. Upload $zipPath as release asset" -ForegroundColor Gray
Write-Host ""
