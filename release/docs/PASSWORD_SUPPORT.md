# Password Support Documentation

## Tính năng hỗ trợ mật khẩu

Silent Setup hỗ trợ mật khẩu cho các trường hợp sau:

### 1. Download với Authentication

**Use case**: Download từ server yêu cầu đăng nhập (HTTP Basic Authentication)

**Cấu hình trong app manifest:**

```yaml
download:
  url: https://protected-server.com/software.exe
  username: your_username
  password: your_password
  checksum: ""
```

**Cách hoạt động**: Username và password được encode Base64 và gửi trong header `Authorization: Basic`

### 2. ZIP có mật khẩu (App Installation)

**Use case**: App được đóng gói dạng ZIP có password protection

**Cấu hình:**

```yaml
install:
  type: zip
  install_dir: "%ProgramFiles%\\MyApp"
  password: "archive_password"
```

**⚠️ Lưu ý**: Hiện tại **chưa hỗ trợ đầy đủ**. Cần cài thêm **SharpZipLib** package:

```powershell
dotnet add package SharpZipLib
```

### 3. ZIP có mật khẩu (Patch Archive)

**Use case**: Patch được đóng gói dạng ZIP có password protection

**Cấu hình trong patch manifest:**

```yaml
type: archive
archive:
  file: vietnamese-pack.zip
  extract_to: "{app_dir}\\lang"
  overwrite: true
  password: "pack_password"
```

**⚠️ Lưu ý**: Tương tự app ZIP, cần **SharpZipLib** để hỗ trợ.

## Triển khai SharpZipLib (Optional)

Nếu cần hỗ trợ ZIP có mật khẩu:

### Bước 1: Cài package

```powershell
cd SilentSetup
dotnet add package SharpZipLib
```

### Bước 2: Update InstallService.cs

```csharp
using ICSharpCode.SharpZipLib.Zip;

// Trong InstallZip method:
if (!string.IsNullOrWhiteSpace(app.Install.Password))
{
    using (var zipFile = new ZipFile(installerPath))
    {
        zipFile.Password = app.Install.Password;
        foreach (ZipEntry entry in zipFile)
        {
            if (entry.IsFile)
            {
                var entryPath = Path.Combine(targetDir, entry.Name);
                Directory.CreateDirectory(Path.GetDirectoryName(entryPath));
                
                using (var stream = zipFile.GetInputStream(entry))
                using (var fileStream = File.Create(entryPath))
                {
                    stream.CopyTo(fileStream);
                }
            }
        }
    }
}
```

### Bước 3: Update PatchService.cs

Tương tự cho `ApplyArchive` method.

### Bước 4: Rebuild

```powershell
dotnet build
dotnet publish -c Release -r win-x64 --self-contained /p:PublishSingleFile=true
```

## Bảo mật mật khẩu

### ⚠️ Cảnh báo bảo mật

- Mật khẩu lưu **plain text** trong YAML files
- Có thể đọc được bởi bất kỳ ai có quyền truy cập file
- **KHÔNG NÊN** dùng cho mật khẩu quan trọng

### Best practices

1. **Chỉ dùng cho**:
   - Archive passwords của public patches
   - Download authentication cho internal tools
   - Temporary/disposable passwords

2. **KHÔNG dùng cho**:
   - Personal passwords
   - Production credentials
   - Sensitive system passwords

3. **Cải thiện bảo mật** (nâng cao):
   - Encrypt YAML files
   - Use Windows Credential Manager
   - Prompt password at runtime
   - Environment variables

### Ví dụ: Dùng environment variable

**Trong manifest:**

```yaml
download:
  url: https://server.com/app.exe
  username: "${DOWNLOAD_USER}"
  password: "${DOWNLOAD_PASS}"
```

**Trước khi chạy app:**

```powershell
$env:DOWNLOAD_USER = "myuser"
$env:DOWNLOAD_PASS = "mypass"
.\SilentSetup.exe
```

**Note**: Cần implement placeholder resolution trong ManifestLoader để hỗ trợ `${VAR}`.

## Ví dụ thực tế

### Example 1: Download với Basic Auth

```yaml
name: Company Internal Tool
id: internal-tool
homepage: https://company.com/

download:
  url: https://downloads.company.com/tool.exe
  username: employee
  password: temp_pass_2026
  checksum: ""

install:
  type: exe
  silent_args: /S

detection:
  method: registry
  registry:
    - path: HKLM\SOFTWARE\Company\Tool
      value: Version
```

### Example 2: Password-protected patch

```yaml
name: Premium Plugin Pack
id: app-premium-plugins
target_app: myapp

type: archive
archive:
  file: plugins.zip
  extract_to: "{app_dir}\\plugins"
  overwrite: true
  password: "premium2026"

metadata:
  description: Premium features pack
  author: Vendor
  version: "1.0"

security:
  risk_level: low
  warning: "Archive password required. Ensure SharpZipLib is installed."
```

## Troubleshooting

### Error: "Password-protected ZIP extraction not yet implemented"

**Giải pháp**: Cài SharpZipLib package và update code như hướng dẫn trên.

### Error: "401 Unauthorized" khi download

**Kiểm tra**:
1. Username/password có đúng?
2. Server có support HTTP Basic Authentication?
3. URL có yêu cầu HTTPS?

### Password không hoạt động

**Kiểm tra**:
1. Mật khẩu có chứa ký tự đặc biệt? Cần escape trong YAML
2. Encoding đúng? (UTF-8 recommended)
3. Trailing spaces trong password field?

## Status

✅ **Implemented**: HTTP Basic Authentication for downloads  
⚠️ **Partial**: ZIP password support (requires SharpZipLib)  
📋 **Planned**: Environment variable resolution, encrypted storage
