# Script to update all YAML files to new schema
# Changes:
# - require_admin → requires_admin (apps)
# - run_as_admin → requires_admin (patches)
# - extract_to → extract_dir (patches)
# - args → exec_args (patches execute type)

Write-Host "Updating app manifests..." -ForegroundColor Cyan

# Update all app YAML files
Get-ChildItem "apps/*.yaml" -Exclude "_template.yaml" | ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    if ($content -match "require_admin:") {
        $content = $content -replace "require_admin:", "requires_admin:"
        Set-Content $_.FullName -Value $content -NoNewline
        Write-Host "  Updated: $($_.Name)" -ForegroundColor Green
    }
}

Write-Host "`nUpdating patch manifests..." -ForegroundColor Cyan

# Update all patch YAML files
Get-ChildItem "patches/**/manifest.yaml" -Recurse | ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    $updated = $false

    if ($content -match "run_as_admin:") {
        $content = $content -replace "run_as_admin:", "requires_admin:"
        $updated = $true
    }

    if ($content -match "extract_to:") {
        $content = $content -replace "extract_to:", "extract_dir:"
        $updated = $true
    }

    if ($content -match "(\s+)args:") {
        $content = $content -replace "(\s+)args:", "`$1exec_args:"
        $updated = $true
    }

    if ($updated) {
        Set-Content $_.FullName -Value $content -NoNewline
        Write-Host "  Updated: $($_.FullName)" -ForegroundColor Green
    }
}

Write-Host "`nSchema update complete!" -ForegroundColor Green
