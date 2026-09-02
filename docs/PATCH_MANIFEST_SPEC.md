# Patch Manifest Specification

## Overview

Patch manifest định nghĩa một bản vá/mở rộng cho ứng dụng đã cài đặt. Mỗi patch có một thư mục riêng trong `patches/` chứa manifest và files thực tế.

## Directory Structure

```
patches/
├── chrome-vi/
│   ├── manifest.yaml        # Patch metadata
│   └── files/               # Actual patch files
│       ├── vi.pak
│       └── vi.dll
│
├── vscode-prettier/
│   ├── manifest.yaml
│   └── files/
│       └── prettier-3.2.5.vsix
│
└── _template/
    ├── manifest.yaml
    └── files/
        └── README.txt
```

**Naming convention:** `patch-name/` (lowercase, hyphens, no spaces)

## Schema

### Minimal Example

```yaml
name: Chrome Vietnamese Language Pack
id: chrome-vi
target_app: chrome

type: copy-files

files:
  - name: vi.pak
    destination: "{app_dir}/Locales/"
```

### Full Example

```yaml
# === REQUIRED FIELDS ===

name: Chrome Vietnamese Language Pack  # Display name
id: chrome-vi                          # Unique identifier
target_app: chrome                     # App ID from apps/*.yaml

# === COMPATIBILITY ===

compatibility:
  # Target app versions (glob patterns)
  app_versions:
    - "120.*"
    - "121.*"
  
  # Or specific version range
  # min_version: "120.0.0.0"
  # max_version: "121.9.9.9"

# === PATCH TYPE ===

# Type: copy-files | executable | registry | archive | download-extract
type: copy-files

# === FILE OPERATIONS (for type: copy-files) ===

files:
  - name: vi.pak                      # File in patches/chrome-vi/files/
    destination: "{app_dir}/Locales/" # Where to copy
    backup: true                      # Backup original file (default: true)
    overwrite: true                   # Overwrite if exists (default: true)
    
  - name: vi.dll
    destination: "{app_dir}/"
    backup: true

# === EXECUTABLE (for type: executable) ===

# execute:
#   file: patcher.exe                 # File in patches/xxx/files/
#   exec_args:                             # Command-line arguments
#     - --target
#     - "{app_dir}"
#   working_dir: "{app_dir}"          # Working directory
#   requires_admin: true                # Require admin (default: false)
#   timeout: 300                      # Timeout in seconds

# === REGISTRY (for type: registry) ===

# registry:
#   - action: set                     # set | delete
#     root: HKLM                      # HKLM | HKCU
#     path: "SOFTWARE\\Company\\App"
#     name: Language                  # Value name
#     value: vi-VN                    # Value data
#     type: string                    # string | dword | binary
#     
#   - action: delete
#     root: HKLM
#     path: "SOFTWARE\\Company\\App"
#     name: OldValue

# === ARCHIVE (for type: archive) ===

# archive:
#   file: plugin.zip                  # ZIP file in patches/xxx/files/
#   extract_dir: "{app_dir}/plugins/"  # Extract destination
#   overwrite: true

# === VERIFICATION (OPTIONAL) ===

verification:
  # Files that must exist after patch
  required_files:
    - "{app_dir}/Locales/vi.pak"
    - "{app_dir}/vi.dll"
  
  # Registry keys that must exist
  required_registry:
    - root: HKLM
      path: "SOFTWARE\\Company\\App"
      name: Language
      value: vi-VN

# === ROLLBACK (OPTIONAL) ===

rollback:
  # Enable automatic rollback on error
  enabled: true
  
  # Keep backup files for manual rollback
  keep_backups: true
  
  # Backup location (default: patches/patch-id/.backup/)
  backup_dir: ".backup"

# === METADATA ===

metadata:
  # Patch category
  category: Language Pack  # Language Pack | Plugin | Theme | Crack | Fix
  
  # Author
  author: Your Name
  
  # Version
  version: 1.0.0
  
  # Description
  description: Giao diện tiếng Việt cho Google Chrome
  
  # Source URL
  source_url: https://example.com/chrome-vi
  
  # Last updated (YYYY-MM-DD)
  last_updated: 2026-09-01
  
  # Tags
  tags:
    - vietnamese
    - language
    - localization

# === SECURITY WARNING ===

security:
  # Risk level: low | medium | high
  risk_level: low
  
  # Custom warning message
  warning: "This patch modifies Chrome language files. Safe for use."
```

## Patch Types

### 1. Copy Files

Copy files from `patches/xxx/files/` to target app directory.

**Use cases:** Language packs, DLL replacements, config files

```yaml
type: copy-files

files:
  - name: file1.dll
    destination: "{app_dir}/"
    backup: true
    overwrite: true
  
  - name: config.ini
    destination: "{app_data}/"
```

### 2. Executable

Run a patcher executable (crack, keygen, etc.)

**Use cases:** App patchers, license activators

```yaml
type: executable

execute:
  file: patcher.exe
  exec_args:
    - --silent
    - --target
    - "{app_dir}"
  working_dir: "{temp}"
  requires_admin: true
  timeout: 300

security:
  risk_level: high
  warning: "⚠️ This patch runs executable code. Only use from trusted sources!"
```

### 3. Registry

Modify Windows registry.

**Use cases:** Settings changes, license keys

```yaml
type: registry

registry:
  # Set value
  - action: set
    root: HKLM
    path: "SOFTWARE\\Company\\App"
    name: Language
    value: vi-VN
    type: string
  
  # Set DWORD
  - action: set
    root: HKCU
    path: "SOFTWARE\\Company\\App"
    name: TrialDays
    value: 9999
    type: dword
  
  # Delete value
  - action: delete
    root: HKLM
    path: "SOFTWARE\\Company\\App"
    name: LicenseCheck

security:
  risk_level: medium
  warning: "This patch modifies registry. Backup registry before applying."
```

### 4. Archive

Extract ZIP archive to target directory.

**Use cases:** Plugins, themes, asset packs

```yaml
type: archive

archive:
  file: plugin-pack.zip
  extract_dir: "{app_dir}/plugins/"
  overwrite: true
  
  # Optional: specific files to extract
  include:
    - "*.dll"
    - "config/*"
  
  # Optional: files to skip
  exclude:
    - "readme.txt"
```

### 5. Download & Extract

Download a ZIP file from URL and extract specific files to target directory.

**Use cases:** Plugins from GitHub releases, remote asset packs, auto-updating patches

```yaml
type: download-extract

download:
  url: https://github.com/user/plugin/releases/latest/download/plugin.zip
  checksum: sha256-hash-here  # Optional but recommended

files:
  - name: plugin.dll
    destination: "{app_dir}/plugins/PluginName/plugin.dll"
    backup: true
    overwrite: true
  
  - name: config.json
    destination: "{app_dir}/plugins/PluginName/config.json"

metadata:
  description: Automatically downloads latest version from GitHub
  source_url: https://github.com/user/plugin

security:
  risk_level: medium
  warning: "Downloads from external URL. Verify source before applying."
```

**Benefits:**
- No need to commit large binary files to repo
- Always gets latest version from official source
- Reduces repo size significantly

**Notes:**
- Downloads to temp directory, extracts needed files, then cleans up
- Only specified files are copied to destination
- Checksum validation recommended for security

## Placeholders

Available placeholders in paths:

| Placeholder | Description | Example |
|-------------|-------------|---------|
| `{app_dir}` | App install directory | C:\Program Files\Chrome |
| `{app_data}` | App data directory | C:\Users\Name\AppData\Roaming\Chrome |
| `{temp}` | Temp directory | C:\Users\Name\AppData\Local\Temp |
| `{patch_dir}` | This patch directory | E:\SilentSetup\patches\chrome-vi |
| `{patch_files}` | Patch files directory | E:\SilentSetup\patches\chrome-vi\files |

Plus all environment variables:
- `{ProgramFiles}` → `%ProgramFiles%`
- `{LocalAppData}` → `%LocalAppData%`
- `{AppData}` → `%AppData%`
- etc.

## Compatibility

### Version Matching

**Glob patterns:**
```yaml
compatibility:
  app_versions:
    - "120.*"      # Matches 120.0, 120.1, 120.999
    - "121.*"      # Matches all 121.x
    - "122.0.*"    # Matches 122.0.x only
```

**Version range:**
```yaml
compatibility:
  min_version: "120.0.0.0"
  max_version: "122.9.9.9"
```

**Both:**
```yaml
compatibility:
  app_versions: ["120.*", "121.*"]
  min_version: "120.0.0.0"
  max_version: "121.9.9.9"
```

## Security Levels

| Level | Description | Use Cases |
|-------|-------------|-----------|
| `low` | Safe file operations | Language packs, themes |
| `medium` | Registry changes, system files | Settings patches, config mods |
| `high` | Executable code, cracks | Patchers, keygens, license activators |

**UI behavior:**
- `low`: No warning
- `medium`: Yellow warning icon
- `high`: Red warning + checkbox "I understand the risks"

## Examples

### Language Pack

```yaml
name: Chrome Vietnamese
id: chrome-vi
target_app: chrome

type: copy-files

files:
  - name: vi.pak
    destination: "{app_dir}/Locales/"

metadata:
  category: Language Pack
  author: Community
  description: Giao diện tiếng Việt

security:
  risk_level: low
```

### Plugin Installation

```yaml
name: VS Code Prettier Extension
id: vscode-prettier
target_app: vscode

type: archive

archive:
  file: prettier-3.2.5.vsix
  extract_dir: "{app_data}/extensions/"

compatibility:
  app_versions: ["1.85.*", "1.86.*"]

metadata:
  category: Plugin
  author: Prettier Team
  version: 3.2.5

security:
  risk_level: low
```

### App Patcher (High Risk)

```yaml
name: Adobe Acrobat Patcher
id: acrobat-patcher
target_app: acrobat

type: executable

execute:
  file: patch.exe
  exec_args: ["--silent"]
  working_dir: "{app_dir}"
  requires_admin: true

metadata:
  category: Crack
  author: Unknown
  description: Remove trial limitations

security:
  risk_level: high
  warning: |
    ⚠️ WARNING: This patch modifies Adobe Acrobat executable.
    - May violate software license agreement
    - Use at your own risk
    - Only for testing purposes
```

### Registry Modification

```yaml
name: Chrome Auto-Update Disabler
id: chrome-no-update
target_app: chrome

type: registry

registry:
  - action: set
    root: HKLM
    path: "SOFTWARE\\Policies\\Google\\Chrome"
    name: UpdateDefault
    value: 0
    type: dword

metadata:
  category: Fix
  description: Disable automatic Chrome updates

security:
  risk_level: medium
  warning: "Modifies system registry. May affect system stability."
```

## Validation Rules

**ID format:**
- Lowercase, hyphens only
- Must start with letter
- Max 50 characters
- Must be unique across all patches

**File references:**
- All files in `files:` must exist in `patches/xxx/files/`
- Paths must use forward slashes or double backslashes
- No absolute paths (use placeholders)

**Compatibility:**
- Must specify either `app_versions` or `min_version`/`max_version`
- Version patterns must be valid glob or semantic version

## Best Practices

1. **Test patches thoroughly** before distributing
2. **Always enable backup** for file operations
3. **Set appropriate risk_level** - users trust this
4. **Document side effects** in description
5. **Include rollback instructions** for high-risk patches
6. **Version your patches** - use `metadata.version`
7. **Keep files small** - don't bundle unnecessary files
8. **Use verification** to ensure patch applied correctly

## Troubleshooting

### Patch not applying?

1. Check `{app_dir}` resolves correctly:
   - Install app manually
   - Note the install path
   - Verify detection works in app manifest

2. Check file permissions:
   - Admin needed for Program Files
   - User can write to AppData

3. Check compatibility:
   - Does app version match `app_versions`?
   - Is version detection working?

### Rollback not working?

1. Check backup location: `patches/patch-id/.backup/`
2. Manually restore files from backup
3. For registry: export key before patching

### High risk warning?

If your patch is actually safe but marked high risk:
```yaml
security:
  risk_level: low  # Set appropriately
  warning: "Safe operation: only copies language files"
```

## Template

Copy to `patches/your-patch/manifest.yaml`:

```yaml
name: Your Patch Name
id: your-patch-id
target_app: target-app-id

compatibility:
  app_versions:
    - "*"  # All versions, or specify like "1.0.*"

type: copy-files  # or executable, registry, archive

files:
  - name: file-in-files-folder.dll
    destination: "{app_dir}/"
    backup: true

metadata:
  category: Plugin  # Language Pack | Plugin | Theme | Crack | Fix
  author: Your Name
  version: 1.0.0
  description: What this patch does
  last_updated: 2026-09-01

security:
  risk_level: low  # low | medium | high
```

Don't forget to put actual files in `patches/your-patch/files/`!
