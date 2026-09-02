# WinRAR Vietnamese Language Pack

Patch này cài đặt giao diện tiếng Việt cho WinRAR.

## Yêu cầu

- WinRAR đã được cài đặt
- File ngôn ngữ `winrar_vi.lng` (bắt buộc)
- File license `rarreg.key` (tùy chọn)

## Cách lấy file ngôn ngữ

1. Truy cập: https://www.win-rar.com/downld.html?&L=9
2. Tìm mục "Language files"
3. Chọn "Vietnamese"
4. Download file `winrar_vi.lng`
5. Đặt file vào thư mục `files/` (cùng thư mục với README này)

## Cách thêm license (tùy chọn)

Nếu bạn đã mua license WinRAR:
1. Bạn sẽ nhận được file `rarreg.key` qua email
2. Đặt file `rarreg.key` vào thư mục `files/`
3. Patch sẽ tự động kích hoạt WinRAR khi cài đặt

Lưu ý:
- License là **tùy chọn**, không bắt buộc
- Nếu không có license, WinRAR hoạt động ở chế độ trial 40 ngày
- Không chia sẻ file license (vi phạm bản quyền)
- Mua license tại: https://www.win-rar.com/buy-winrar.html

## Cấu trúc thư mục

```
patches/winrar-vietnamese/
├── manifest.yaml          # Patch definition
├── README.md             # File này
└── files/
    ├── winrar_vi.lng     # File ngôn ngữ (cần tải về - BẮT BUỘC)
    └── rarreg.key        # File license (tùy chọn)
```

## Cách sử dụng

1. Download file `winrar_vi.lng` và đặt vào `files/`
2. Mở SilentSetup
3. Chọn WinRAR để cài đặt
4. Tick chọn patch "WinRAR Tiếng Việt"
5. Click "Cài đặt đã chọn"

## Patch thực hiện gì?

1. **Copy file ngôn ngữ**: Sao chép `winrar_vi.lng` vào thư mục cài đặt WinRAR
2. **Copy file license** (nếu có): Sao chép `rarreg.key` để kích hoạt WinRAR
3. **Cài đặt registry**: Đặt ngôn ngữ mặc định là Vietnamese

## Kiểm tra sau khi cài

1. Mở WinRAR
2. Giao diện sẽ hiển thị tiếng Việt
3. Nếu chưa, vào Options → General → Language → chọn Vietnamese

## Ghi chú

- File ngôn ngữ chính thức từ win-rar.com
- Miễn phí, không cần license
- Tương thích với WinRAR 7.x

## Xử lý sự cố

**Giao diện vẫn tiếng Anh?**
- Kiểm tra file `winrar_vi.lng` đã có trong `files/` chưa
- Khởi động lại WinRAR
- Kiểm tra Settings → Language

**File ngôn ngữ không tìm thấy?**
- Download lại từ trang chính thức
- Đảm bảo tên file chính xác: `winrar_vi.lng`
- Đặt đúng vị trí: `patches/winrar-vietnamese/files/`
