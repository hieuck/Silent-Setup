# Silent Setup - Architecture Design

## Overview

Silent Setup là công cụ portable giúp tự động hóa việc cài đặt phần mềm Windows theo chế độ silent (không tương tác), với hỗ trợ patches/plugins mở rộng.

**Đặc điểm:**
- ✅ Portable - không cần cài đặt, chạy trực tiếp
- ✅ Metadata-driven - thêm app bằng YAML files
- ✅ Extensible - patch system cho ngôn ngữ/plugins
- ✅ Smart caching - không tải lại installers
- ✅ Detection - phát hiện app đã cài + version

## Architecture

```
┌─────────────────────────────────────────────────┐
│              UI Layer (WPF)                     │
│  - MainWindow: App list, install buttons       │
│  - Progress: Download/install progress          │
└─────────────────┬───────────────────────────────┘
                  │
┌─────────────────┴───────────────────────────────┐
│           Service Layer                         │
│  ┌──────────────────────────────────────────┐  │
│  │ ManifestLoader                           │  │
│  │  - Load apps/*.yaml                      │  │
│  │  - Load patches/*/manifest.yaml          │  │
│  │  - Validate schemas                      │  │
│  └──────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────┐  │
│  │ DetectionService                         │  │
│  │  - Registry scan                         │  │
│  │  - File system check                     │  │
│  │  - Version extraction                    │  │
│  └──────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────┐  │
│  │ DownloadService                          │  │
│  │  - HTTP download with resume             │  │
│  │  - Checksum verification                 │  │
│  │  - Cache management                      │  │
│  └──────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────┐  │
│  │ InstallService                           │  │
│  │  - Silent install (.exe, .msi)           │  │
│  │  - Process management                    │  │
│  │  - Error handling                        │  │
│  └──────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────┐  │
│  │ PatchService                             │  │
│  │  - Apply patches after app install       │  │
│  │  - File copy/registry/execute            │  │
│  │  - Backup & rollback                     │  │
│  └──────────────────────────────────────────┘  │
└─────────────────┬───────────────────────────────┘
                  │
┌─────────────────┴───────────────────────────────┐
│           Data Layer                            │
│  - apps/*.yaml          (app definitions)       │
│  - patches/*/           (patch definitions)     │
│  - cache/               (downloaded installers) │
│  - logs/                (operation logs)        │
│  - config.json          (user settings)         │
└─────────────────────────────────────────────────┘
```

## Directory Structure

```
SilentSetup/
├── SilentSetup.exe          # Main executable
├── config.json              # User configuration
│
├── apps/                    # App definitions
│   ├── chrome.yaml
│   ├── vscode.yaml
│   ├── vlc.yaml
│   └── _template.yaml       # Template for new apps
│
├── patches/                 # Patch definitions
│   ├── chrome-vi/
│   │   ├── manifest.yaml    # Patch metadata
│   │   └── files/           # Actual patch files
│   │       ├── vi.pak
│   │       └── vi.dll
│   │
│   └── _template/
│       ├── manifest.yaml
│       └── files/
│           └── README.txt
│
├── cache/                   # Downloaded installers (auto-managed)
│   ├── chrome_120.0.6099.129.exe
│   ├── vscode_1.85.2.msi
│   └── .checksums.json      # SHA256 hashes
│
├── logs/                    # Operation logs
│   ├── 2026-09-01.log
│   └── install-history.json # Success/failure records
│
└── docs/                    # Documentation
    ├── ARCHITECTURE.md
    ├── APP_MANIFEST_SPEC.md
    ├── PATCH_MANIFEST_SPEC.md
    └── USER_GUIDE.md
```

## Core Components

### 1. ManifestLoader

**Responsibilities:**
- Load & parse YAML files
- Validate against schema
- Build in-memory app/patch registry

**Key methods:**
```csharp
List<AppManifest> LoadApps()
List<PatchManifest> LoadPatches()
void ValidateManifest(AppManifest app)
```

### 2. DetectionService

**Responsibilities:**
- Detect installed apps
- Extract version numbers
- Match with manifest definitions

**Detection strategies:**
- Registry: `HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall`
- File existence: Check known paths
- Version extraction: Read PE headers, file properties

**Key methods:**
```csharp
AppStatus DetectApp(AppManifest app)
string GetInstalledVersion(AppManifest app)
bool IsInstalled(AppManifest app)
```

### 3. DownloadService

**Responsibilities:**
- Download installers from URLs
- Resume interrupted downloads
- Verify checksums
- Manage cache

**Features:**
- HTTP range requests for resume
- SHA256 verification
- Cache hit: skip download if file exists + hash matches
- Parallel downloads (optional)

**Key methods:**
```csharp
Task<string> DownloadAsync(string url, IProgress<int> progress)
bool VerifyChecksum(string filePath, string expectedHash)
string GetCachedPath(string url)
```

### 4. InstallService

**Responsibilities:**
- Execute silent installers
- Monitor install process
- Handle errors & timeouts

**Supported formats:**
- `.exe` with silent args (`/S`, `/silent`, `/verysilent`, etc.)
- `.msi` with `msiexec /i /quiet`
- `.zip` portable extract

**Key methods:**
```csharp
Task<InstallResult> InstallAsync(AppManifest app, string installerPath)
Process StartSilentInstall(string path, string args)
bool WaitForInstallComplete(Process process, TimeSpan timeout)
```

### 5. PatchService

**Responsibilities:**
- Apply patches after app installation
- Support multiple patch types
- Backup original files
- Rollback on failure

**Patch types:**
- `copy-files`: Copy files to app directory
- `executable`: Run patcher executable
- `registry`: Modify registry keys
- `archive`: Extract ZIP to location

**Key methods:**
```csharp
Task<PatchResult> ApplyPatchAsync(PatchManifest patch, AppManifest app)
void BackupFiles(List<string> paths)
void RollbackPatch(PatchManifest patch)
```

## Data Flow

### Install Flow

```
1. User selects app(s) + optional patch(es)
   ↓
2. DetectionService: Check if already installed
   ↓ (if not installed or update available)
3. DownloadService: Download installer to cache/
   ↓
4. InstallService: Run silent installer
   ↓
5. DetectionService: Verify installation success
   ↓ (if patches selected)
6. PatchService: Apply each patch
   ↓
7. UI: Show success/failure + logs
```

### Detection Flow

```
1. ManifestLoader: Load apps/*.yaml
   ↓
2. For each app:
   ↓
3. DetectionService: Check registry/filesystem
   ↓
4. If found → extract version
   ↓
5. Compare with manifest.latest_version
   ↓
6. Return status: Not Installed | Installed | Update Available
```

## Configuration

`config.json` structure:

```json
{
  "download": {
    "cache_dir": "cache",
    "cache_size_limit_mb": 5000,
    "timeout_seconds": 300,
    "retry_count": 3
  },
  "install": {
    "timeout_seconds": 600,
    "run_as_admin": true,
    "verify_after_install": true
  },
  "proxy": {
    "enabled": false,
    "url": "http://proxy:8080"
  },
  "profiles": {
    "dev-machine": ["vscode", "git", "nodejs", "docker"],
    "media-pc": ["vlc", "spotify", "obs"]
  }
}
```

## Security Considerations

### Download Security
- ✅ HTTPS-only downloads
- ✅ SHA256 checksum verification
- ✅ Domain whitelist (optional)
- ⚠️ No signature verification (EXE Authenticode) - future enhancement

### Patch Security
- ⚠️ Patches can execute arbitrary code
- ✅ Warning dialog for `executable` type patches
- ✅ User must explicitly enable each patch
- 💡 Consider: digital signature for patch manifests

### Privilege Escalation
- App runs as user by default
- UAC prompt only when needed (install to Program Files)
- No persistent elevated privileges

## Error Handling

**Download errors:**
- Network timeout → retry with exponential backoff
- 404/403 → show error + link to app homepage
- Checksum mismatch → re-download (max 3 attempts)

**Install errors:**
- Non-zero exit code → check logs, show to user
- Timeout → kill process, mark as failed
- Disk full → cleanup cache, retry

**Patch errors:**
- File not found → skip patch, warn user
- Permission denied → prompt for admin
- Rollback on any error

## Logging

**Log levels:**
- `INFO`: Normal operations (download started, install complete)
- `WARN`: Recoverable errors (retry download)
- `ERROR`: Failed operations (install failed)

**Log locations:**
- Daily log: `logs/YYYY-MM-DD.log`
- Install history: `logs/install-history.json` (structured data)

**Log format:**
```
2026-09-01 14:45:23 [INFO] Download started: Chrome 120.0.6099.129
2026-09-01 14:45:45 [INFO] Download complete: cache/chrome_120.0.6099.129.exe
2026-09-01 14:45:46 [INFO] Starting silent install: chrome
2026-09-01 14:46:15 [INFO] Install complete: chrome (exit code: 0)
2026-09-01 14:46:16 [INFO] Applying patch: chrome-vi
2026-09-01 14:46:17 [INFO] Patch applied: chrome-vi
```

## Future Enhancements

**Phase 2:**
- [ ] Auto-update detection via web scraping
- [ ] Parallel downloads
- [ ] Uninstall support
- [ ] Export/import profiles as ZIP
- [ ] Dependency resolution (App A requires App B)

**Phase 3:**
- [ ] Plugin system for custom installers
- [ ] Web UI (local server)
- [ ] Community manifest repository
- [ ] Differential updates (binary diff patches)

## Technology Stack

**Language:** C# 10 (.NET 6)

**UI Framework:** WPF (Windows Presentation Foundation)

**Libraries:**
- `YamlDotNet` - YAML parsing
- `Newtonsoft.Json` - JSON handling
- `Microsoft.Win32.Registry` - Registry detection
- `System.Net.Http` - Downloads

**Build:**
- Single-file publish: `dotnet publish -c Release -r win-x64 --self-contained /p:PublishSingleFile=true`
- Output: ~15MB EXE (self-contained .NET runtime)

## Performance Targets

- Startup: < 1 second
- App list load: < 500ms (100 apps)
- Detection scan: < 2 seconds (100 apps)
- Download: Limited by network speed
- Install: Limited by installer speed
- Memory usage: < 100MB idle

## Testing Strategy

**Unit tests:**
- Manifest parsing & validation
- Detection logic (mocked registry)
- Checksum verification

**Integration tests:**
- Download → cache → verify
- Install → detect → verify

**Manual tests:**
- Test with 5-10 real apps
- Test patches: language pack, plugin
- Test error scenarios: network down, disk full

## Build & Release

**Build command:**
```powershell
dotnet publish -c Release -r win-x64 --self-contained /p:PublishSingleFile=true
```

**Release package:**
```
SilentSetup_v1.0.0.zip
├── SilentSetup.exe
├── config.json (default config)
├── apps/
│   └── _template.yaml
├── patches/
│   └── _template/
└── README.md
```

**Versioning:** Semantic versioning (Major.Minor.Patch)
- Major: Breaking changes to manifest format
- Minor: New features (backward compatible)
- Patch: Bug fixes
