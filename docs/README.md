# SCDC — Tài liệu dự án

Thư mục `docs/` mô tả sản phẩm SCDC, thiết kế hệ thống và cách phát triển, vận hành.

<a id="contents"></a>
<a id="sources"></a>

## Bắt đầu đọc

- [Tổng quan dự án](project/overview.md): sản phẩm phục vụ ai và gồm những gì.
- [Tóm tắt kiến trúc v1](system/architecture.md#summary): mười điểm chính, kèm link tới sơ đồ.
- [Hướng dẫn phát triển](guides/development.md): chuẩn bị môi trường, chạy ứng dụng và kiểm thử.
- [Thuật ngữ](glossary.md): giải thích các từ kỹ thuật dùng trong tài liệu.
- [Quy ước viết tài liệu](guides/writing.md): đọc trước khi sửa hoặc thêm trang.

## Cấu trúc

```text
docs/
├── README.md                # Trang này
├── glossary.md              # Thuật ngữ
├── project/                 # Mục tiêu, phạm vi, kế hoạch và quy trình
├── experience/              # Hành trình người dùng và giao diện
├── system/                  # Thiết kế kỹ thuật dùng chung cho từ hai service trở lên
├── features/                # Từng nhóm tính năng: quy tắc, luồng, thiết kế riêng
│   ├── accounts/            # Tài khoản
│   ├── community/           # Server, thành viên, phòng và quyền
│   ├── messaging/           # Nhắn tin riêng và tin phòng
│   ├── media/               # Gọi riêng, phòng thoại, video và chia sẻ màn hình
│   ├── notifications/       # Thông báo (đang thiết kế)
│   └── files/               # Gửi file (đang thiết kế)
├── releases/                # Các mốc bàn giao và điều kiện nghiệm thu
├── guides/                  # Hướng dẫn phát triển, vận hành và viết tài liệu
├── contracts/               # Hợp đồng API và cấu trúc dữ liệu trao đổi
├── fixtures/                # Dữ liệu mẫu để kiểm thử
├── records/                 # Hồ sơ: quyết định, kết quả kiểm thử, mẫu biểu
│   ├── decisions/           # DEC-001 đến 173
│   ├── verification/        # Kết quả kiểm thử theo gói
│   └── templates/           # Mẫu phát hành, sự cố, Trello, họp tuần
└── archive/                 # Tài liệu cũ đã được thay thế
```

<a id="placement"></a>

## Đặt nội dung ở đâu

- Chỉ một tính năng dùng (kể cả thiết kế backend riêng của nó): `features/<nhóm>/`. Ví dụ [cơ chế Community](features/community/mechanics.md).
- Từ hai service trở lên dùng: `system/`. Ví dụ [thu hồi truy cập](system/access-revocation.md).
- Cách chạy, kiểm thử, vận hành: một trang trong `guides/`, chia mục theo phần; không tạo hướng dẫn riêng cho từng tính năng.
- Kết quả một lần chạy hay một quyết định: `records/`. Tài liệu không còn dùng: `archive/`.

## Hiện trạng (2026-11-28)

- [Kiến trúc hiện tại](system/architecture.md#current): Identity và Community có host/database riêng; Messaging tiếp tục ở API cũ, Media chưa tích hợp vào main.
- [Tổ chức backend](system/backend.md) ghi ranh giới nguồn và thư viện; [task](project/tasks/README.md) ghi phần đã hoàn tất và phụ thuộc còn lại.
- Các mốc dưới đây là lịch sử thay đổi tài liệu, không thay cho trạng thái triển khai hiện tại.

## Thay đổi 2026-11-11

- Đề xuất [lộ trình lên microservice](project/planning.md#roadmap) gồm 4 bước, thay các gói V1-ARCH; lộ trình đã được xác nhận ở S1-01 và đang thực hiện STEP-3.
- Gộp tóm tắt kiến trúc vào [kiến trúc](system/architecture.md#summary), hướng dẫn Community vào [hướng dẫn phát triển](guides/development.md#community).
- Chuyển cơ chế Community từ `system/` sang [features/community](features/community/mechanics.md).
- Chuyển kế hoạch và prompt DM cũ ([2026-10-planning](archive/2026-10-planning/messaging/direct-messaging.md)) cùng bản đồ chuyển đổi 2026-10-11 vào `archive/`.

## Thay đổi 2026-11-01

- Phân công mới theo dịch vụ, quản lý nhiệm vụ trên Trello và họp hằng tuần: xem [kế hoạch](project/planning.md#team) và [quy trình](project/workflow.md#trello).
- Ba phần mới được đưa vào kế hoạch, đang thiết kế: [mobile](experience/mobile.md), [thông báo](features/notifications/README.md) và [gửi file](features/files/README.md).
- Mẫu mới: [nhiệm vụ Trello](records/templates/trello-task.md) và [biên bản họp tuần](records/templates/weekly-meeting.md).
