# Hướng dẫn thêm App và Patch

## 📱 Cách thêm ứng dụng mới

### Bước 1: Tạo file YAML mới

Trong thư mục `apps/`, tạo file mới theo tên app (ví dụ: `winrar.yaml`)

### Bước 2: Sao chép template

```yaml
name: Tên App Hiển Thị
id: app-id-unique
homepage: https://website-app.com/

download:
  url: https://link-download-truc-tiep.com/installer.exe
  checksum: ""  # SHA256 hash (tùy chọn)
  mirrors: []
  username: ""  # Nếu download cần đăng nhập (tùy chọn)
  password: ""  # Mật khẩu download (tùy chọn)

install:
  type: exe  # exe, msi, hoặc zip
  silent_args: /S  # Tham số cài đặt silent
  password: ""  # Mật khẩu để giải nén ZIP (nếu type=zip và có mật khẩu)
  pre_install:
    kill_processes:
      - app.exe  # Process cần đóng trước khi cài
  post_install:
    create_shortcuts: []

detection:
  method: both  # registry, file, hoặc both
  registry:
    - path: HKLM\SOFTWARE\AppName
      value: Version
  file:
    - path: C:\Program Files\AppName\app.exe

metadata:
  description: Mô tả ngắn về app
  publisher: Tên nhà phát hành
  category: Utility  # Browser, Development, Media, Utility
  license: Freeware
  tags:
    - tag1
    - tag2
```

### Bước 3: Điền thông tin

**Thông tin bắt buộc:**
- `name`: Tên hiển thị trong UI
- `id`: Mã duy nhất (chữ thường, không dấu, dùng dấu gạch ngang)
- `homepage`: Website chính thức
- `download.url`: Link download trực tiếp (HTTPS only)
- `install.type`: Loại installer (exe/msi/zip)
- `install.silent_args`: Tham số silent install
- `detection.method`: Cách phát hiện app đã cài

**Tìm silent args:**
- **NSIS installer**: thường là `/S`
- **Inno Setup**: `/VERYSILENT /NOCLOSEAPPLICATIONS`
- **MSI**: `/qn /norestart`
- Google: "[tên app] silent install parameters"

**Detection:**
- `registry`: Kiểm tra registry key (nhanh)
  - `path`: Đường dẫn registry (dùng `\\` thay vì `\`)
  - `value`: Tên value chứa version
- `file`: Kiểm tra file tồn tại
  - `path`: Đường dẫn file .exe

### Bước 4: Lưu và refresh

1. Lưu file YAML
2. Mở SilentSetup.exe
3. Click nút **Làm mới** để load app mới

---

## 🔧 Cách thêm Patch

### Bước 1: Tạo thư mục patch

Trong `patches/`, tạo thư mục mới (ví dụ: `vscode-vietnamese/`)

### Bước 2: Tạo manifest.yaml

```yaml
name: Vietnamese Language Pack
id: vscode-vietnamese
target_app: vscode  # ID của app đích

type: copy-files  # copy-files, executable, registry, archive

# Loại 1: Copy files
files:
  - name: language-pack-vi.vsix
    destination: "{app_dir}\resources\app\extensions\"
    backup: true
    overwrite: true

metadata:
  description: Gói ngôn ngữ tiếng Việt
  author: Your Name
  version: "1.0"
  category: Localization

security:
  risk_level: low  # low, medium, high
```

### Bước 3: Đặt file patch

Tạo thư mục `files/` bên trong thư mục patch và đặt files vào đó:

```
patches/
└── vscode-vietnamese/
    ├── manifest.yaml
    └── files/
        └── language-pack-vi.vsix
```

### Các loại patch:

#### 1. Copy Files (sao chép file)
```yaml
type: copy-files
files:
  - name: file.dll
    destination: "{app_dir}\plugins\"
    backup: true
```

#### 2. Executable (chạy patcher)
```yaml
type: executable
execute:
  file: crack.exe
  args:
    - /silent
  working_dir: "{app_dir}"
  run_as_admin: true
  timeout: 300
```

#### 3. Registry (sửa registry)
```yaml
type: registry
registry:
  - action: set
    root: HKLM
    path: SOFTWARE\App\Settings
    name: Language
    value: vi-VN
    type: string
```

#### 4. Archive (giải nén)
```yaml
type: archive
archive:
  file: plugin-pack.zip
  extract_to: "{app_dir}\plugins\"
  overwrite: true
  password: "secret123"  # Mật khẩu nếu ZIP có bảo vệ (tùy chọn)
```

### Placeholders có sẵn:

- `{app_dir}`: Thư mục cài đặt của app
- `{patch_files}`: Thư mục files/ của patch
- `{program_files}`: C:\Program Files
- `{temp}`: Thư mục temp

### Bước 4: Test

1. Mở SilentSetup.exe
2. Chọn app + patch tương ứng
3. Click **Cài đặt đã chọn**

---

## ✅ Ví dụ hoàn chỉnh

### Thêm WinRAR

**File: `apps/winrar.yaml`**
```yaml
name: WinRAR
id: winrar
homepage: https://www.win-rar.com/

download:
  url: https://www.win-rar.com/fileadmin/winrar-versions/winrar-x64-700.exe
  checksum: ""
  mirrors: []

install:
  type: exe
  silent_args: /S
  pre_install:
    kill_processes:
      - WinRAR.exe
  post_install:
    create_shortcuts: []

detection:
  method: both
  registry:
    - path: HKLM\SOFTWARE\WinRAR
      value: Version
  file:
    - path: C:\Program Files\WinRAR\WinRAR.exe

metadata:
  description: Powerful archive manager
  publisher: RARLAB
  category: Utility
  license: Shareware
  tags:
    - compression
    - archiver
```

### Thêm WinRAR Vietnamese Patch

**File: `patches/winrar-vietnamese/manifest.yaml`**
```yaml
name: WinRAR Tiếng Việt
id: winrar-vietnamese
target_app: winrar

type: copy-files
files:
  - name: RarExt.lng
    destination: "{app_dir}"
    backup: true
    overwrite: true
  - name: WinRAR.lng
    destination: "{app_dir}"
    backup: true
    overwrite: true

metadata:
  description: Giao diện tiếng Việt cho WinRAR
  author: Vietnamese Community
  version: "7.00"
  category: Localization

security:
  risk_level: low
```

**Files:**
```
patches/winrar-vietnamese/
├── manifest.yaml
└── files/
    ├── RarExt.lng
    └── WinRAR.lng
```

---

## 🚨 Lưu ý quan trọng

1. **YAML syntax**: Phải đúng cú pháp (indent = 2 spaces)
2. **Detection registry path**: Dùng `\\` thay vì `\` trong YAML
3. **File paths**: Dùng `\` trong đường dẫn Windows
4. **HTTPS only**: Download URLs phải dùng HTTPS
5. **Unique ID**: Mỗi app/patch phải có ID duy nhất
6. **Test kỹ**: Test trong máy ảo trước khi dùng thật
7. **Mật khẩu**: 
   - Download password: Dùng cho HTTP Basic Authentication
   - ZIP password: **Cần cài thêm SharpZipLib** (chưa hỗ trợ sẵn)
   - Mật khẩu lưu plain text trong YAML - cân nhắc bảo mật

---

## 🔍 Debug

Nếu app không hiển thị:
1. Check logs trong `logs/YYYY-MM-DD.log`
2. Kiểm tra YAML syntax (dùng online YAML validator)
3. Đảm bảo `detection` section đúng format (list)
4. Click **Làm mới** trong app

Nếu patch không hoạt động:
1. Kiểm tra `target_app` ID có khớp app manifest
2. Đảm bảo files tồn tại trong `files/` folder
3. Check logs để xem lỗi cụ thể
