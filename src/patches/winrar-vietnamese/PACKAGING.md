# Hướng dẫn đóng gói Patch có mật khẩu

## Tại sao dùng password?

- Bảo vệ file license không bị chia sẻ trái phép
- Chỉ người có password mới cài được patch
- Phù hợp cho patch có nội dung nhạy cảm

## Cách 1: Dùng 7-Zip (Khuyến nghị)

### Bước 1: Cài đặt 7-Zip
Download từ: https://www.7-zip.org/

### Bước 2: Tạo archive có password
```bash
cd E:\GitHub\Silent-Setup\patches\winrar-vietnamese

# Nén files với password
7z a -tzip -p"your-password" -mhe=on winrar-patch.zip files\*
```

Giải thích tham số:
- `-tzip` : Định dạng ZIP
- `-p"password"` : Mật khẩu (thay "your-password" bằng mật khẩu thật)
- `-mhe=on` : Mã hóa cả tên file (header encryption)
- `winrar-patch.zip` : Tên file output
- `files\*` : Nén tất cả file trong thư mục files/

### Bước 3: Cập nhật manifest.yaml
```yaml
patches:
  - type: archive
    description: Giải nén file patch từ archive có mật khẩu
    archive: winrar-patch.zip
    password: "your-password"
    destination: '{patch_files}'
```

## Cách 2: Dùng WinRAR

```bash
cd E:\GitHub\Silent-Setup\patches\winrar-vietnamese

# Nén files với password
rar a -hp"your-password" winrar-patch.rar files\*
```

Giải thích tham số:
- `a` : Add to archive
- `-hp"password"` : Password với header encryption
- `winrar-patch.rar` : Tên file output

Sau đó đổi extension trong manifest từ `.zip` thành `.rar`

## Cách 3: Dùng PowerShell + DotNetZip

### Bước 1: Tải DotNetZip
```powershell
# Cài package DotNetZip
Install-Package DotNetZip -Force
```

### Bước 2: Chạy script
```powershell
Add-Type -Path "path\to\DotNetZip.dll"

$zip = New-Object Ionic.Zip.ZipFile
$zip.Password = "your-password"
$zip.Encryption = [Ionic.Zip.EncryptionAlgorithm]::WinZipAes256

# Add files
Get-ChildItem "files\*" | ForEach-Object {
    $zip.AddFile($_.FullName, "")
}

$zip.Save("winrar-patch.zip")
$zip.Dispose()
```

## Cấu trúc thư mục sau khi đóng gói

```
patches/winrar-vietnamese/
├── manifest.yaml          # Có cấu hình archive + password
├── README.md
├── PACKAGING.md          # File này
├── winrar-patch.zip      # Archive có mật khẩu
└── files/                # Giữ lại cho reference (hoặc xóa)
    ├── winrar_vi.lng
    └── rarreg.key
```

## Cấu hình Manifest đầy đủ

```yaml
name: WinRAR Tiếng Việt
target_app: winrar
version: 7.01
description: Gói ngôn ngữ tiếng Việt cho WinRAR

patches:
  # Bước 1: Giải nén archive có password
  - type: archive
    description: Giải nén file patch từ archive có mật khẩu
    archive: winrar-patch.zip
    password: "your-password"
    destination: '{patch_files}'

  # Bước 2: Copy file ngôn ngữ
  - type: copy-files
    description: Sao chép file ngôn ngữ tiếng Việt
    files:
      - source: winrar_vi.lng
        destination: '{app_dir}\winrar_vi.lng'

  # Bước 3: Copy license (nếu có)
  - type: copy-files
    description: Sao chép file license (nếu có)
    files:
      - source: rarreg.key
        destination: '{app_dir}\rarreg.key'
        optional: true

  # Bước 4: Cấu hình registry
  - type: registry
    description: Cài đặt ngôn ngữ mặc định
    keys:
      - path: HKCU\Software\WinRAR\Interface
        value: Language
        data: Vietnamese
        type: String
```

## Placeholder pattern

Hệ thống hỗ trợ các placeholder:
- `{patch_files}` - Thư mục tạm để giải nén
- `{app_dir}` - Thư mục cài đặt của app
- `{temp}` - Thư mục temp của system

## Lưu ý bảo mật

⚠️ **QUAN TRỌNG:**
- Mật khẩu được lưu plaintext trong manifest.yaml
- KHÔNG commit file manifest.yaml có password lên Git public
- Dùng password mạnh (ít nhất 12 ký tự)
- Chỉ chia sẻ password cho người được phép

## Kiểm tra

Sau khi đóng gói, test patch:
1. Mở SilentSetup
2. Chọn WinRAR để cài
3. Tick patch "WinRAR Tiếng Việt"
4. Click "Cài đặt đã chọn"
5. Hệ thống sẽ tự động:
   - Giải nén winrar-patch.zip với password
   - Copy files vào đúng vị trí
   - Cấu hình registry

## Ví dụ thực tế

```bash
# 1. Tạo archive có password
cd patches/winrar-vietnamese
7z a -tzip -p"MyStr0ngP@ss" -mhe=on winrar-patch.zip files\*

# 2. Cập nhật manifest.yaml
# Thay "your-password" thành "MyStr0ngP@ss"

# 3. Test trong SilentSetup
cd ../../
.\SilentSetup\bin\Debug\net8.0-windows\SilentSetup.exe
```

## Xóa files gốc (tùy chọn)

Sau khi đóng gói xong, có thể xóa thư mục files/:
```bash
Remove-Item files -Recurse -Force
```

Archive đã chứa mọi thứ cần thiết!
