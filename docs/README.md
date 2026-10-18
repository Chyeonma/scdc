# SCDC — Tài liệu dự án

Thư mục `docs/` chứa tài liệu về sản phẩm, thiết kế hệ thống và cách phát triển, vận hành SCDC.

<a id="contents"></a>
<a id="sources"></a>

## Cấu trúc

```text
docs/
├── README.md                # Bản đồ tài liệu
├── project/                 # Mục tiêu, phạm vi, kế hoạch và quy trình
├── experience/              # Hành trình người dùng và giao diện
├── system/                  # Kiến trúc, dữ liệu và thiết kế kỹ thuật
├── features/                # Quy tắc và luồng của từng nhóm tính năng
│   ├── accounts/            # Tài khoản
│   ├── community/           # Server, thành viên, phòng và quyền
│   ├── messaging/           # Nhắn tin riêng và tin phòng
│   └── media/               # Gọi riêng, phòng thoại, video và chia sẻ màn hình
├── releases/                # Các mốc bàn giao và điều kiện nghiệm thu
├── guides/                  # Hướng dẫn phát triển và vận hành
├── contracts/               # Hợp đồng API và cấu trúc dữ liệu trao đổi
├── fixtures/                # Dữ liệu mẫu để kiểm chứng
├── records/                 # Hồ sơ quyết định, kế hoạch và kết quả
│   ├── decisions/           # Các quyết định và lý do
│   ├── verification/        # Kết quả kiểm thử theo phiên bản
│   ├── planning/            # Hồ sơ kế hoạch đã ghi nhận
│   ├── migrations/          # Lịch sử tổ chức lại tài liệu
│   └── templates/           # Mẫu hồ sơ bàn giao, phát hành và sự cố
└── archive/                 # Tài liệu cũ đã được thay thế
```

## Bắt đầu đọc

- [Tổng quan dự án](project/overview.md): sản phẩm phục vụ ai và có phạm vi gì.
- [Kiến trúc hệ thống](system/architecture.md): các thành phần và cách phối hợp.
- [Hướng dẫn phát triển](guides/development.md): chuẩn bị môi trường, chạy ứng dụng và kiểm thử.
