# App Manifest Specification

## Overview

App manifest là file YAML định nghĩa một ứng dụng có thể cài đặt. Mỗi app có một file riêng trong thư mục `apps/`.

## File Location

```
apps/
├── chrome.yaml
├── vscode.yaml
└── your-app.yaml
```

**Naming convention:** `app-id.yaml` (lowercase, hyphens, no spaces)

## Schema

### Minimal Example

```yaml
name: Google Chrome
id: chrome
homepage: https://www.google.com/chrome/

download:
  url: https://dl.google.com/chrome/install/latest/chrome_installer.exe

install:
  type: exe
  silent_args: /silent /install
```

### Full Example

```yaml
# === REQUIRED FIELDS ===

name: Google Chrome                      # Display name
id: chrome                               # Unique identifier (lowercase, no spaces)
homepage: https://www.google.com/chrome/ # Official website

# === DOWNLOAD CONFIGURATION ===

download:
  # Direct download URL (use if URL is stable)
  url: https://dl.google.com/chrome/install/latest/chrome_installer.exe
  
  # Or specify version pattern for detection
  # version_url: https://www.google.com/chrome/
  # version_regex: 'Version (\d+\.\d+\.\d+\.\d+)'
  
  # Expected file size (optional, for validation)
  size_mb: 85
  
  # SHA256 checksum (optional but recommended)
  checksum: a1b2c3d4e5f6...
  
  # Mirror URLs (fallback if main URL fails)
  mirrors:
    - https://mirror1.example.com/chrome.exe
    - https://mirror2.example.com/chrome.exe

# === INSTALL CONFIGURATION ===

install:
  # Installer type: exe | msi | zip
  type: exe
  
  # Silent install arguments
  silent_args: /silent /install
  
  # Expected install location (for verification)
  install_dir: "%ProgramFiles%\\Google\\Chrome"
  
  # Timeout in seconds (default: 600)
  timeout: 600
  
  # Require admin privileges
  requires_admin: true

# === DETECTION CONFIGURATION ===

detection:
  # Method: registry | file | both
  method: registry
  
  # Registry paths to check
  registry:
    # Check uninstall registry
    - path: "HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\Google Chrome"
      value: DisplayVersion  # Read version from this value
    
    # Or check app registry
    - path: "HKLM\\SOFTWARE\\Google\\Chrome"
      value: Version
  
  # File paths to check
  file:
    - path: "%ProgramFiles%\\Google\\Chrome\\Application\\chrome.exe"
      version_source: file_properties  # Extract version from file properties
    
    # Or check specific version file
    - path: "%ProgramFiles%\\Google\\Chrome\\Application\\chrome.version"
      version_source: file_content  # Read version from file content
      version_regex: '(\d+\.\d+\.\d+\.\d+)'

# === METADATA (OPTIONAL) ===

metadata:
  # App category
  category: Browser
  
  # Publisher/Developer
  publisher: Google LLC
  
  # License type
  license: Freeware
  
  # Short description
  description: Fast, secure web browser from Google
  
  # Tags for search/filter
  tags:
    - browser
    - google
    - web
  
  # Last updated (YYYY-MM-DD)
  last_updated: 2026-09-01
  
  # Maintainer of this manifest
  maintainer: Your Name

# === ADVANCED OPTIONS (OPTIONAL) ===

advanced:
  # Pre-install actions
  pre_install:
    # Kill processes before install
    kill_processes:
      - chrome.exe
      - googleupdate.exe
    
    # Delete files/folders
    cleanup:
      - "%TEMP%\\ChromeSetup"
  
  # Post-install actions
  post_install:
    # Create desktop shortcut
    shortcuts:
      - name: Google Chrome
        target: "%ProgramFiles%\\Google\\Chrome\\Application\\chrome.exe"
        location: desktop
    
    # Set default browser (requires user consent)
    set_default_browser: true
  
  # Uninstall configuration
  uninstall:
    command: "%ProgramFiles%\\Google\\Chrome\\Application\\chrome_uninstaller.exe"
    args: --force-uninstall --system-level
  
  # Dependencies (other apps that must be installed first)
  dependencies:
    - id: vcredist2015
      optional: false
```

## Field Reference

### Required Fields

| Field | Type | Description |
|-------|------|-------------|
| `name` | string | Display name shown in UI |
| `id` | string | Unique identifier (lowercase, hyphens only) |
| `homepage` | string | Official website URL |
| `download.url` | string | Direct download URL |
| `install.type` | enum | Installer type: `exe`, `msi`, `zip` |
| `install.silent_args` | string | Command-line arguments for silent install |

### Download Fields

| Field | Type | Description |
|-------|------|-------------|
| `url` | string | Direct download URL (required) |
| `version_url` | string | Page to scrape version from (optional) |
| `version_regex` | string | Regex to extract version (optional) |
| `size_mb` | number | Expected file size in MB (optional) |
| `checksum` | string | SHA256 hash for verification (optional) |
| `mirrors` | array | Fallback download URLs (optional) |

### Install Fields

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `type` | enum | - | `exe`, `msi`, or `zip` |
| `silent_args` | string | - | Silent install arguments |
| `install_dir` | string | - | Expected install directory |
| `timeout` | number | 600 | Install timeout in seconds |
| `requires_admin` | boolean | true | Require admin privileges |

### Detection Fields

| Field | Type | Description |
|-------|------|-------------|
| `method` | enum | `registry`, `file`, or `both` |
| `registry` | array | Registry paths to check |
| `file` | array | File paths to check |

## Installer Types

### EXE Installers

**Common silent arguments:**

| Installer | Silent Args | Example |
|-----------|-------------|---------|
| Inno Setup | `/VERYSILENT /NORESTART` | VSCode, Notepad++ |
| NSIS | `/S` | VLC, FileZilla |
| InstallShield | `/s /v"/qn"` | Adobe Reader |
| Custom | Varies | Check app documentation |

**Example:**
```yaml
install:
  type: exe
  silent_args: /VERYSILENT /NORESTART
```

### MSI Installers

MSI uses `msiexec` automatically. Only specify custom arguments if needed.

**Default:** `msiexec /i installer.msi /quiet /norestart`

**Example:**
```yaml
install:
  type: msi
  silent_args: /quiet /norestart INSTALLDIR="C:\\MyApp"
```

### ZIP (Portable)

For portable apps, extract to specified directory.

**Example:**
```yaml
install:
  type: zip
  install_dir: "%LocalAppData%\\PortableApps\\MyApp"
```

## Detection Methods

### Registry Detection

**Uninstall registry (most common):**
```yaml
detection:
  method: registry
  registry:
    - path: "HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\AppName"
      value: DisplayVersion
```

**App-specific registry:**
```yaml
detection:
  method: registry
  registry:
    - path: "HKLM\\SOFTWARE\\Company\\AppName"
      value: Version
```

### File Detection

**Check executable existence:**
```yaml
detection:
  method: file
  file:
    - path: "%ProgramFiles%\\App\\app.exe"
      version_source: file_properties
```

**Check version file:**
```yaml
detection:
  method: file
  file:
    - path: "%ProgramFiles%\\App\\version.txt"
      version_source: file_content
      version_regex: '^(\d+\.\d+\.\d+)'
```

### Combined Detection

```yaml
detection:
  method: both
  registry:
    - path: "HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\App"
      value: DisplayVersion
  file:
    - path: "%ProgramFiles%\\App\\app.exe"
```

## Environment Variables

Supported variables in paths:

| Variable | Expands To | Example |
|----------|------------|---------|
| `%ProgramFiles%` | C:\Program Files | |
| `%ProgramFiles(x86)%` | C:\Program Files (x86) | |
| `%LocalAppData%` | C:\Users\Name\AppData\Local | |
| `%AppData%` | C:\Users\Name\AppData\Roaming | |
| `%TEMP%` | C:\Users\Name\AppData\Local\Temp | |
| `%SystemRoot%` | C:\Windows | |
| `%UserProfile%` | C:\Users\Name | |

## Validation Rules

**ID format:**
- Lowercase letters, numbers, hyphens only
- Must start with letter
- Max 50 characters
- Examples: `chrome`, `visual-studio-code`, `7zip`

**URL format:**
- Must start with `https://` (HTTP not allowed for security)
- Must be valid URL format

**Version format:**
- Semantic versioning preferred: `1.2.3` or `1.2.3.4`
- Regex must capture version in group 1: `(\d+\.\d+\.\d+)`

## Examples by App Type

### Browser (Chrome)

```yaml
name: Google Chrome
id: chrome
homepage: https://www.google.com/chrome/

download:
  url: https://dl.google.com/chrome/install/latest/chrome_installer.exe

install:
  type: exe
  silent_args: /silent /install
  install_dir: "%ProgramFiles%\\Google\\Chrome"

detection:
  method: registry
  registry:
    - path: "HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\Google Chrome"
      value: DisplayVersion
```

### IDE (VS Code)

```yaml
name: Visual Studio Code
id: vscode
homepage: https://code.visualstudio.com/

download:
  url: https://code.visualstudio.com/sha/download?build=stable&os=win32-x64

install:
  type: exe
  silent_args: /VERYSILENT /NORESTART /MERGETASKS=!runcode
  install_dir: "%ProgramFiles%\\Microsoft VS Code"

detection:
  method: file
  file:
    - path: "%ProgramFiles%\\Microsoft VS Code\\Code.exe"
      version_source: file_properties
```

### Media Player (VLC)

```yaml
name: VLC Media Player
id: vlc
homepage: https://www.videolan.org/vlc/

download:
  url: https://get.videolan.org/vlc/last/win64/vlc-win64.exe

install:
  type: exe
  silent_args: /S
  install_dir: "%ProgramFiles%\\VideoLAN\\VLC"

detection:
  method: both
  registry:
    - path: "HKLM\\SOFTWARE\\VideoLAN\\VLC"
      value: Version
  file:
    - path: "%ProgramFiles%\\VideoLAN\\VLC\\vlc.exe"
```

### Portable App (Notepad++)

```yaml
name: Notepad++ Portable
id: notepadpp-portable
homepage: https://notepad-plus-plus.org/

download:
  url: https://github.com/notepad-plus-plus/notepad-plus-plus/releases/latest/download/npp.portable.x64.zip

install:
  type: zip
  install_dir: "%LocalAppData%\\Programs\\Notepad++"

detection:
  method: file
  file:
    - path: "%LocalAppData%\\Programs\\Notepad++\\notepad++.exe"
      version_source: file_properties
```

## Best Practices

1. **Always specify checksum** if app provides it on download page
2. **Use HTTPS only** for download URLs
3. **Test silent args** before publishing manifest
4. **Include mirrors** for popular apps (in case main URL is down)
5. **Document version_regex** with comment if it's complex
6. **Keep homepage up to date** so users can verify download source
7. **Add metadata** to help users find your app

## Troubleshooting

### Silent install not working?

1. Download installer manually
2. Run: `installer.exe /?` or check documentation
3. Test: `installer.exe /S` (or other silent flag)
4. Watch Task Manager to see if it actually runs silently

### Detection not working?

1. Install app manually
2. Check Registry Editor: `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall`
3. Look for app's registry key
4. Note the `DisplayVersion` or similar value
5. Use that path in `detection.registry`

### Download URL changes?

Some apps change URL per version. Options:
1. Use "latest" URL if available (e.g., Chrome)
2. Scrape version page with `version_url` + `version_regex`
3. Manually update manifest when new version releases

## Template

Copy this template to `apps/your-app.yaml`:

```yaml
name: Your App Name
id: your-app-id
homepage: https://example.com/

download:
  url: https://example.com/download/installer.exe
  # checksum: sha256-hash-here

install:
  type: exe  # or msi, zip
  silent_args: /S  # adjust based on installer type
  install_dir: "%ProgramFiles%\\YourApp"

detection:
  method: registry
  registry:
    - path: "HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\YourApp"
      value: DisplayVersion

metadata:
  category: Utility
  publisher: Company Name
  license: Freeware
  description: Short description of what the app does
  maintainer: Your Name
```
