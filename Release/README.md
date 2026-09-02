# Silent Setup

Ứng dụng Windows tự động cài đặt phần mềm miễn phí với chế độ silent (im lặng), hỗ trợ patch Việt hóa và plugin.

## Tính năng

✅ **Cài đặt tự động**: Cài đặt hàng loạt phần mềm mà không cần tương tác  
✅ **Phát hiện thông minh**: Tự động phát hiện phần mềm đã cài đặt  
✅ **Patch & Plugin**: Hỗ trợ Việt hóa, plugin, và các bản vá  
✅ **Portable**: Không cần cài đặt, chạy trực tiếp file .exe  
✅ **Dễ mở rộng**: Thêm app mới qua giao diện hoặc file YAML  
✅ **Tìm kiếm & Lọc**: Tìm kiếm theo tên, category, publisher  
✅ **100% Miễn phí**: Mã nguồn mở

## Yêu cầu hệ thống

- Windows 10/11 (64-bit)
- .NET 8 Runtime (ứng dụng sẽ tự nhắc cài nếu thiếu)

## Cách sử dụng

### Bước 1: Tải về

1. Tải file `SilentSetup-v1.0.zip` từ [Releases](../../releases)
2. Giải nén vào thư mục bất kỳ

### Bước 2: Chạy ứng dụng

1. Mở thư mục vừa giải nén
2. Double-click `SilentSetup.exe`
3. Giao diện chính sẽ hiện ra với danh sách phần mềm

### Bước 3: Cài đặt phần mềm

1. **Tìm kiếm**: Gõ tên phần mềm vào ô tìm kiếm
2. **Lọc**: Chọn category (Browser, Development, Media, Utility)
3. **Chọn app**: Tick vào các ứng dụng muốn cài
4. **Chọn patch** (tùy chọn): Tick vào bản Việt hóa hoặc plugin bên dưới tên app
5. **Cài đặt**: Click nút "Cài đặt đã chọn"
6. Chờ quá trình hoàn tất (thanh progress bar sẽ hiển thị tiến độ)

### Chức năng khác

- **Refresh**: Làm mới danh sách và kiểm tra app đã cài
- **Select All/Deselect All**: Chọn/bỏ chọn tất cả
- **Chuột phải vào app**: Chỉnh sửa, xóa, xem chi tiết, mở trang chủ
- **Thêm App**: Click nút "+" để thêm app mới qua giao diện
- **View Logs**: Xem nhật ký cài đặt

## Thêm phần mềm mới

### Cách 1: Qua giao diện (Dễ nhất)

1. Click nút **"+ Thêm App"**
2. Điền thông tin:
   - **Tên**: Tên hiển thị (VD: Google Chrome)
   - **ID**: Tên không dấu (VD: google-chrome)
   - **Website**: Trang chủ chính thức
   - **Link download**: Link trực tiếp file cài đặt (bắt buộc HTTPS)
   - **Loại installer**: exe / msi / zip
   - **Silent args**: Tham số cài đặt im lặng (VD: /S, /silent)
   - **Registry key**: Đường dẫn registry để phát hiện (VD: HKLM\SOFTWARE\AppName)
   - **Hoặc đường dẫn file**: File .exe sau khi cài (VD: C:\Program Files\App\app.exe)
3. Click **Lưu**

### Cách 2: Tạo file YAML thủ công

Tạo file mới trong thư mục `apps/` với tên `app-id.yaml`:

```yaml
name: Google Chrome
id: google-chrome
homepage: https://www.google.com/chrome/

download:
  url: https://dl.google.com/chrome/install/latest/chrome_installer.exe

install:
  type: exe
  silent_args: /silent /install

detection:
  method: both
  registry:
    - path: HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\chrome.exe
  file:
    - path: C:\Program Files\Google\Chrome\Application\chrome.exe

metadata:
  category: Browser
  publisher: Google LLC
  description: Trình duyệt web nhanh và an toàn
```

Xem thêm mẫu trong `apps/_template.yaml`

## Thêm Patch / Việt hóa

1. Tạo thư mục mới trong `patches/` (VD: `patches/chrome-vietnamese/`)
2. Tạo file `manifest.yaml`:

```yaml
name: Chrome Tiếng Việt
target_app: google-chrome

patches:
  - type: copy-files
    description: Copy language pack
    files:
      - source: vi.pak
        destination: '{app_dir}\Locales\vi.pak'
```

3. Đặt file patch vào thư mục `files/` (VD: `patches/chrome-vietnamese/files/vi.pak`)

Xem thêm: `docs/PATCH_MANIFEST_SPEC.md`

## Cấu trúc thư mục

```
SilentSetup/
├── SilentSetup.exe          # File chương trình chính
├── apps/                    # Định nghĩa các phần mềm
│   ├── google-chrome.yaml
│   ├── vlc.yaml
│   └── _template.yaml       # Mẫu để tạo app mới
├── patches/                 # Các bản patch
│   └── _template/
│       ├── manifest.yaml
│       └── files/
├── cache/                   # File download tạm (tự động tạo)
├── logs/                    # Nhật ký hoạt động (tự động tạo)
└── config.json             # Cấu hình ứng dụng
```

## Cấu hình nâng cao

Chỉnh sửa file `config.json`:

```json
{
  "download": {
    "cache_directory": "cache",
    "timeout_seconds": 300,
    "max_retries": 3
  },
  "install": {
    "default_timeout": 600,
    "verify_checksum": true
  }
}
```

## Các phần mềm được hỗ trợ sẵn

- **Browser**: Google Chrome, Mozilla Firefox
- **Development**: Visual Studio Code, Notepad++
- **Media**: VLC Media Player
- **Utility**: 7-Zip

## Khắc phục sự cố

### "App không tải được"
- Kiểm tra kết nối internet
- Xem logs trong `logs/YYYY-MM-DD.log`
- Click **View Logs** để xem chi tiết lỗi

### "Cài đặt thất bại"
- Chạy ứng dụng với quyền Administrator (chuột phải → Run as administrator)
- Kiểm tra antivirus có chặn không
- Đảm bảo đủ dung lượng ổ cứng

### "Không phát hiện app đã cài"
- Click **Refresh** để làm mới
- Kiểm tra đường dẫn registry/file trong manifest có đúng không
- Một số app cài vào thư mục khác với mặc định

## Hỗ trợ

- **Issues**: [GitHub Issues](../../issues)
- **Tài liệu đầy đủ**: Xem thư mục `docs/`
  - `USER_GUIDE.md` - Hướng dẫn chi tiết
  - `APP_MANIFEST_SPEC.md` - Định dạng file app
  - `PATCH_MANIFEST_SPEC.md` - Định dạng patch
  - `ARCHITECTURE.md` - Kiến trúc hệ thống

## License

MIT License - Sử dụng tự do cho mọi mục đích

## Credits

Phát triển bởi [Your Name]  
Powered by .NET 8 & WPF
