# Silent Setup - User Guide

## Giới thiệu

Silent Setup là công cụ portable giúp bạn cài đặt nhiều phần mềm Windows cùng lúc theo chế độ silent (không cần tương tác). Chỉ cần chọn app, nhấn Install, và để tool làm phần còn lại.

**Đặc điểm:**
- ✅ Không cần cài đặt - chạy trực tiếp
- ✅ Tự động download installers
- ✅ Cài đặt silent - không làm phiền
- ✅ Phát hiện app đã cài
- ✅ Hỗ trợ patches (Việt hóa, plugins)

## Bắt đầu nhanh

### 1. Download & Chạy

1. Download `SilentSetup.zip`
2. Giải nén ra folder bất kỳ
3. Chạy `SilentSetup.exe`

**Không cần cài đặt!** Tool chạy ngay.

### 2. Cài đặt Apps

1. Chọn apps từ danh sách (tick checkbox)
2. Nhấn **Install Selected**
3. Chờ tool download + cài đặt
4. Xong!

### 3. Áp dụng Patches (tùy chọn)

Mỗi app có thể có patches (Việt hóa, plugins):

1. Mở rộng app (click mũi tên)
2. Chọn patches muốn cài
3. Patches sẽ được áp dụng sau khi app cài xong

## Giao diện

```
┌─ Silent Setup ─────────────────────────────────────┐
│                                                     │
│ 📦 Available Apps (3 selected)                      │
│                                                     │
│ ☑ Google Chrome        [120.0.6099]  [Not inst.]   │
│   └─ ☑ Vietnamese Language Pack                    │
│                                                     │
│ ☑ Visual Studio Code   [1.85.2]      [Update]      │
│   └─ ☐ Prettier Plugin                             │
│                                                     │
│ ☐ VLC Media Player     [3.0.20]      [Installed]   │
│                                                     │
│ ☐ 7-Zip                [23.01]       [Not inst.]   │
│                                                     │
│ [Select All] [Deselect All] [Refresh]              │
│                                                     │
│ [Install Selected] [Settings] [View Logs]          │
│                                                     │
│ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│ Downloading Chrome 120.0.6099.129... 65%           │
│ [████████████████░░░░░░░░░░]  55 MB / 85 MB       │
└─────────────────────────────────────────────────────┘
```

### Trạng thái Apps

- **Not installed**: Chưa cài
- **Installed**: Đã cài, version hiện tại
- **Update**: Có version mới hơn
- **Unknown**: Không xác định được (có thể cài hoặc không)

## Chức năng chi tiết

### Install Apps

**Quy trình:**
1. Tool download installer vào `cache/`
2. Kiểm tra checksum (nếu có)
3. Chạy installer với silent mode
4. Verify app đã cài thành công
5. Áp dụng patches (nếu được chọn)

**Lưu ý:**
- UAC prompt có thể xuất hiện (cần admin để cài vào Program Files)
- Installers được lưu trong `cache/` - lần sau không tải lại
- Xem progress ở thanh dưới cùng

### Apply Patches

Patches được áp dụng **sau khi** app cài xong.

**Loại patches:**
- **Language Pack**: Việt hóa giao diện
- **Plugin**: Thêm chức năng
- **Theme**: Đổi giao diện
- **Fix**: Sửa lỗi, bỏ giới hạn

**⚠️ Cảnh báo:**
- Patches có thể thay đổi files hệ thống
- Chỉ dùng patches từ nguồn tin cậy
- Backup tự động được tạo (có thể rollback)

### Refresh Status

Nhấn **Refresh** để:
- Quét lại apps đã cài
- Cập nhật version hiện tại
- Kiểm tra updates

### Settings

**Download:**
- Cache size limit (MB)
- Download timeout
- Proxy settings

**Install:**
- Install timeout
- Always run as admin
- Verify after install

**UI:**
- Language (English / Tiếng Việt)
- Theme (Light / Dark)

### View Logs

Xem chi tiết quá trình cài đặt:
- Download logs
- Install logs
- Error messages

Logs được lưu trong `logs/YYYY-MM-DD.log`

## Thêm Apps mới

Bạn có thể tự thêm app bằng cách tạo file YAML.

### Bước 1: Copy template

```
apps/_template.yaml  →  apps/my-app.yaml
```

### Bước 2: Sửa thông tin

```yaml
name: My Application
id: my-app
homepage: https://example.com/

download:
  url: https://example.com/download/installer.exe

install:
  type: exe
  silent_args: /S
  install_dir: "%ProgramFiles%\\MyApp"

detection:
  method: registry
  registry:
    - path: "HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\MyApp"
      value: DisplayVersion
```

### Bước 3: Restart app

Silent Setup sẽ tự động load app mới.

**Chi tiết:** Xem [APP_MANIFEST_SPEC.md](APP_MANIFEST_SPEC.md)

## Thêm Patches

Tạo patch cho app đã có.

### Bước 1: Tạo folder structure

```
patches/my-patch/
├── manifest.yaml
└── files/
    └── your-patch-file.dll
```

### Bước 2: Tạo manifest

```yaml
name: My Patch
id: my-patch
target_app: chrome  # App ID từ apps/

type: copy-files

files:
  - name: your-patch-file.dll
    destination: "{app_dir}/"

metadata:
  category: Plugin
  author: Your Name

security:
  risk_level: low
```

### Bước 3: Bỏ files vào `files/`

Copy patch files vào `patches/my-patch/files/`

### Bước 4: Restart app

Patch xuất hiện dưới target app.

**Chi tiết:** Xem [PATCH_MANIFEST_SPEC.md](PATCH_MANIFEST_SPEC.md)

## Profiles

Lưu danh sách apps để cài nhanh sau này.

### Tạo Profile

**Trong config.json:**

```json
{
  "profiles": {
    "dev-machine": ["vscode", "git", "nodejs", "docker"],
    "media-pc": ["vlc", "spotify", "obs"],
    "office": ["chrome", "office365", "teams"]
  }
}
```

### Dùng Profile

**Qua UI:**
1. Menu → Profiles → Select profile
2. Apps tự động được chọn
3. Nhấn Install

**Qua command line:**
```bash
SilentSetup.exe --profile dev-machine
```

## Command Line

Silent Setup hỗ trợ CLI để automation.

### Cài đặt specific apps

```bash
SilentSetup.exe --install chrome,vscode,vlc
```

### Cài đặt profile

```bash
SilentSetup.exe --profile dev-machine
```

### Cài tất cả apps

```bash
SilentSetup.exe --install-all
```

### Không hiện UI (silent mode)

```bash
SilentSetup.exe --install chrome --silent
```

### List available apps

```bash
SilentSetup.exe --list
```

**Output:**
```
Available apps:
  chrome          Google Chrome               [Not installed]
  vscode          Visual Studio Code          [Installed: 1.85.2]
  vlc             VLC Media Player            [Update: 3.0.18 -> 3.0.20]
  7zip            7-Zip                       [Not installed]
```

### Refresh & check updates

```bash
SilentSetup.exe --refresh
```

## Troubleshooting

### App không cài được?

**Kiểm tra logs:**
1. Nhấn **View Logs**
2. Xem error message
3. Tìm dòng "ERROR" hoặc "Exit code:"

**Nguyên nhân thường gặp:**
- **Exit code non-zero**: Installer failed
  - Thử cài manual để xem lỗi gì
  - Sửa `silent_args` trong manifest
  
- **Timeout**: Installer chạy quá lâu
  - Tăng `timeout` trong manifest
  
- **Permission denied**: Cần admin
  - Click phải → Run as Administrator

### Download bị lỗi?

**404 / 403:**
- URL đã thay đổi
- Cập nhật `download.url` trong manifest

**Checksum mismatch:**
- File bị corrupt
- Xóa file trong `cache/` và tải lại

**Timeout:**
- Mạng chậm
- Tăng `download.timeout_seconds` trong config.json

### Detection không hoạt động?

App đã cài nhưng hiện "Not installed":

1. Cài app manual
2. Mở Registry Editor
3. Tìm: `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall`
4. Tìm app → note registry path
5. Cập nhật `detection.registry.path` trong manifest

### Patch không apply?

**File not found:**
- Kiểm tra file có trong `patches/xxx/files/`
- Kiểm tra tên file trong manifest

**Permission denied:**
- Run as Administrator

**Incompatible version:**
- Kiểm tra `compatibility.app_versions`
- App version có match không?

## Bảo mật

### Download Safety

Silent Setup chỉ download từ:
- HTTPS URLs (không hỗ trợ HTTP)
- Nguồn được chỉ định trong manifests
- Checksum verification (nếu có)

**⚠️ Lưu ý:**
- Tool không verify code signature
- Chỉ thêm apps từ nguồn tin cậy
- Kiểm tra `homepage` URL trước khi cài

### Patch Safety

**Risk levels:**
- 🟢 **Low**: An toàn (language packs, themes)
- 🟡 **Medium**: Cẩn thận (registry, system files)
- 🔴 **High**: Nguy hiểm (executables, cracks)

**Khi cài High-risk patch:**
- Tool hiện warning rõ ràng
- Phải tick "I understand the risks"
- Backup tự động được tạo

**Best practices:**
- Chỉ dùng patches từ nguồn tin cậy
- Đọc description trước khi cài
- Backup quan trọng trước khi patch

### UAC Prompts

Tool yêu cầu admin khi:
- Cài vào `Program Files`
- Patch system files
- Modify registry (HKLM)

**Bình thường:**
- 1-2 UAC prompts cho mỗi app
- Có thể disable trong Settings (không khuyến khích)

## Cache Management

Installers được lưu trong `cache/` để tái sử dụng.

### Xóa cache

**Qua UI:**
Settings → Cache → Clear Cache

**Manual:**
Xóa files trong `cache/` folder

**Size limit:**
- Default: 5GB
- Có thể thay đổi trong Settings
- Tool tự xóa files cũ nhất khi đầy

### Verify cache

```bash
SilentSetup.exe --verify-cache
```

Tool kiểm tra checksums và xóa corrupted files.

## FAQ

### Có cần Internet không?

Có, để download installers lần đầu. Sau đó dùng cache offline được.

### Có cần cài .NET không?

Không. App đã bundle .NET runtime (self-contained).

### Có thể dùng trên Windows 7 không?

Không. Yêu cầu Windows 10 trở lên.

### Tool có free không?

Có, hoàn toàn miễn phí và open source.

### Có thu thập data không?

Không. Tool chạy hoàn toàn offline (trừ download installers).

### App manifest có tự update không?

Không. Bạn phải update manual hoặc tải manifest mới từ community.

### Có thể share manifests không?

Có! Export folder `apps/` và `patches/` để share với người khác.

### Có hỗ trợ portable apps không?

Có. Dùng `install.type: zip` cho portable apps.

## Liên hệ & Hỗ trợ

**Issues:** https://github.com/your-repo/silent-setup/issues

**Documentation:**
- [Architecture](ARCHITECTURE.md)
- [App Manifest Spec](APP_MANIFEST_SPEC.md)
- [Patch Manifest Spec](PATCH_MANIFEST_SPEC.md)

**Community:**
- Share manifests
- Report bugs
- Request features

## Changelog

### v1.0.0 (2026-09-01)
- Initial release
- Support EXE, MSI, ZIP installers
- Patch system (copy-files, executable, registry, archive)
- Detection via registry & filesystem
- Cache management
- Profile support
- CLI support
