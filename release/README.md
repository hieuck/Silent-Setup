# Silent Setup

Ứng dụng Windows tự động cài đặt phần mềm miễn phí với chế độ silent (không cần tương tác). Hỗ trợ patch Việt hóa và plugin mở rộng.

## Tính năng

- ✅ **Cài đặt Silent**: Tự động cài đặt phần mềm không cần can thiệp
- ✅ **Patch System**: Hỗ trợ Việt hóa, plugin, và các bản vá tùy chỉnh
- ✅ **Phát hiện tự động**: Kiểm tra phần mềm đã cài, phiên bản hiện tại
- ✅ **Download Manager**: Tải xuống với progress bar, checksum verification, resume capability
- ✅ **Portable**: Không cần cài đặt, chạy trực tiếp file .exe
- ✅ **Tùy biến dễ dàng**: Thêm app/patch qua file YAML, không cần lập trình

## Yêu cầu hệ thống

- Windows 10/11 (64-bit)
- .NET 8 Runtime (tự động có trong bản single-file)
- Quyền Administrator (để cài đặt phần mềm)

## Cấu trúc thư mục

```
Silent-Setup/
├── SilentSetup.exe          # Ứng dụng chính
├── config.json              # Cấu hình download, install
├── apps/                    # App manifests (YAML)
│   ├── chrome.yaml
│   ├── firefox.yaml
│   ├── vscode.yaml
│   ├── vlc.yaml
│   ├── 7zip.yaml
│   ├── notepadplusplus.yaml
│   └── _template.yaml       # Template để thêm app mới
├── patches/                 # Patch manifests
│   └── _template/
│       ├── manifest.yaml    # Template patch manifest
│       └── files/           # Đặt file patch vào đây
├── cache/                   # Download cache (tự động tạo)
├── logs/                    # Log files (tự động tạo)
└── docs/                    # Tài liệu
    ├── ARCHITECTURE.md
    ├── APP_MANIFEST_SPEC.md
    ├── PATCH_MANIFEST_SPEC.md
    └── USER_GUIDE.md
```

## Hướng dẫn sử dụng

### 1. Chạy ứng dụng

Chỉ cần double-click `SilentSetup.exe` - không cần cài đặt.

### 2. Cài đặt phần mềm

1. Chọn các ứng dụng muốn cài (checkbox)
2. Chọn patches nếu có (Việt hóa, plugin)
3. Click nút **Cài đặt đã chọn**
4. Ứng dụng tự động:
   - Tải xuống từ trang chủ chính thức
   - Kiểm tra checksum (nếu có)
   - Cài đặt silent
   - Áp dụng patches đã chọn

### 3. Thêm ứng dụng mới

Copy `apps/_template.yaml` thành file mới và chỉnh sửa:

```yaml
name: Tên App
id: app-id
homepage: https://...

download:
  url: https://...
  
install:
  type: exe  # exe, msi, zip
  silent_args: /S
  
detection:
  method: both  # registry, file, both
  registry:
    - key: HKLM\SOFTWARE\...
      value: Version
  file:
    path: C:\Program Files\...\app.exe
```

Chi tiết xem: `docs/APP_MANIFEST_SPEC.md`

### 4. Thêm patch

1. Tạo thư mục mới trong `patches/` (ví dụ: `patches/vscode-vietnamese/`)
2. Tạo `manifest.yaml` (xem `patches/_template/manifest.yaml`)
3. Đặt file patch vào `files/` subdirectory
4. Patch sẽ tự động xuất hiện trong UI

Chi tiết xem: `docs/PATCH_MANIFEST_SPEC.md`

## Ứng dụng mẫu có sẵn

- **Google Chrome** - Trình duyệt web
- **Mozilla Firefox** - Trình duyệt web
- **Visual Studio Code** - Code editor
- **VLC Media Player** - Media player
- **7-Zip** - File archiver
- **Notepad++** - Text editor

## Công nghệ sử dụng

- **.NET 8** - Framework
- **WPF** - User interface
- **YamlDotNet** - YAML parsing
- **C# 10** - Programming language

## Phát triển

### Build từ source

```powershell
cd SilentSetup
dotnet build
```

### Tạo single-file executable

```powershell
dotnet publish -c Release -r win-x64 --self-contained /p:PublishSingleFile=true
```

Output: `publish/SilentSetup.exe` (~72 MB)

### Chạy từ source

```powershell
cd SilentSetup
dotnet run
```

## Tài liệu

- [Hướng dẫn sử dụng](docs/USER_GUIDE.md) (Tiếng Việt)
- [Architecture](docs/ARCHITECTURE.md)
- [App Manifest Specification](docs/APP_MANIFEST_SPEC.md)
- [Patch Manifest Specification](docs/PATCH_MANIFEST_SPEC.md)

## Bảo mật

- Chỉ download từ HTTPS
- SHA256 checksum verification
- Registry/file patches cần quyền Administrator
- Risk level warning cho patches nguy hiểm

## License

MIT License - Free for personal and commercial use.

## Tác giả

Created with Claude Code (Anthropic).
