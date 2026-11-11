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

## Thay đổi gần đây (2026-11-11)

- Đề xuất [lộ trình lên microservice](project/planning.md#roadmap) gồm 4 bước, thay các gói V1-ARCH; chờ nhóm xác nhận.
- Gộp tóm tắt kiến trúc vào [kiến trúc](system/architecture.md#summary), hướng dẫn Community vào [hướng dẫn phát triển](guides/development.md#community).
- Chuyển cơ chế Community từ `system/` sang [features/community](features/community/mechanics.md).
- Chuyển kế hoạch và prompt DM cũ ([2026-10-planning](archive/2026-10-planning/messaging/direct-messaging.md)) cùng bản đồ chuyển đổi 2026-10-11 vào `archive/`.

## Thay đổi 2026-11-01

- Phân công mới theo dịch vụ, quản lý nhiệm vụ trên Trello và họp hằng tuần: xem [kế hoạch](project/planning.md#team) và [quy trình](project/workflow.md#trello).
- Ba phần mới được đưa vào kế hoạch, đang thiết kế: [mobile](experience/mobile.md), [thông báo](features/notifications/README.md) và [gửi file](features/files/README.md).
- Mẫu mới: [nhiệm vụ Trello](records/templates/trello-task.md) và [biên bản họp tuần](records/templates/weekly-meeting.md).


docs/
  ├── README.md              Trang bắt đầu: đọc gì trước, sơ đồ thư mục
  ├── glossary.md            Thuật ngữ (epoch, outbox, guard, lease…)
  │
  ├── project/               QUẢN LÝ DỰ ÁN
  │   ├── overview.md        Người dùng, yêu cầu REQ, phạm vi SCP, tiêu chí thành công SUC
  │   ├── planning.md        Phân công (nguồn duy nhất), công suất, ngân sách, rủi ro, các gói việc
  │   ├── workflow.md        Các bước làm một gói, điều kiện READY, review, Trello, họp tuần
  │   └── open-questions.md  Câu hỏi còn mở OQ-001 đến 016
  │
  ├── experience/            TRẢI NGHIỆM CHUNG
  │   ├── overview.md        Hành trình người dùng, bố cục, điều hướng
  │   ├── design-system.md   Thành phần giao diện dùng chung, quy tắc tương tác
  │   └── mobile.md          App mobile (đang thiết kế)
  │
  ├── features/              ĐẶC TẢ TỪNG TÍNH NĂNG (quy tắc, ca sử dụng, giao diện, API, AC/TC)
  │   ├── accounts/          Đăng ký, xác minh, phiên, mật khẩu, hồ sơ, khóa tài khoản, email
  │   ├── community/         Server, thành viên, lời mời, phòng, vai trò và quyền, bảng truy vết
  │   ├── messaging/         DM, tin phòng, vòng đời tin, đồng bộ, quy tắc nội dung
  │   ├── media/             Gọi riêng, phòng thoại, camera và chia sẻ, cơ chế tham gia chung
  │   ├── notifications/     Thông báo (đang thiết kế)
  │   └── files/             Gửi file (đang thiết kế)
  │
  ├── system/                THIẾT KẾ KỸ THUẬT XUYÊN TÍNH NĂNG
  │   ├── architecture-summary.md  Tóm tắt kiến trúc v1 (một trang)
  │   ├── architecture.md    Kiến trúc hiện tại và v1, ranh giới dữ liệu, sơ đồ, bảng quyết định
  │   ├── api-conventions.md Quy ước API, lỗi; gRPC, Gateway, mTLS, JWT (D04, D09, D10, D20, D27–29, D46)
  │   ├── access-revocation.md  Thu hồi phiên và quyền; hàng rào ghi; Redis (D05–07, D30, D31)
  │   ├── backend.md         Cấu trúc code, project, worker, Hub (D12, D13, D39–45, D47)
  │   ├── frontend.md        Cấu trúc WebClient, quy tắc viết code giao diện
  │   ├── database.md        Schema, migration, tách DB, chuyển dữ liệu MVP (D02, D48–52)
  │   ├── data-lifecycle.md  Thời hạn lưu, khóa tài khoản, xóa, dọn dữ liệu, khôi phục
  │   ├── community.md       Cơ chế dùng chung của Community (transaction, chống trùng, cursor)
  │   ├── environments.md    Môi trường, hạ tầng, bố trí container (D03, D21–26, D32, D36, D38)
  │   ├── observability.md   Log, metric, trace (D33–35)
  │   └── quality.md         Mục tiêu chất lượng và cách đo
  │
  ├── releases/              MỐC BÀN GIAO
  │   ├── README.md          MVP và v1 khác nhau thế nào
  │   ├── mvp.md             Bản chụp `900e226`: đã có gì, bằng chứng, việc chuyển sang v1
  │   ├── v1.md              Điểm bắt đầu của v1, dẫn tới các trang liên quan
  │   └── acceptance.md      Khi nào v1 xong, phân loại lỗi, 10 điều kiện phát hành, kiểm tra nhanh
  │
  ├── guides/                HƯỚNG DẪN THAO TÁC
  │   ├── development.md     Cài đặt, chạy, cấu hình, kiểm thử, đồng bộ tài liệu
  │   ├── community-development.md  Chạy và kiểm thử Community, nâng cấp database
  │   ├── operations.md      Runbook: triển khai, rollback, sự cố, khóa tài khoản
  │   └── writing.md         Quy ước viết tài liệu
  │
  ├── contracts/             OpenAPI và JSON Schema (sau này thêm `.proto` ở grpc/)
  ├── fixtures/              Dữ liệu kiểm thử dạng JSON và SQL
  │
  ├── records/               HỒ SƠ (lịch sử, không sửa nội dung cũ)
  │   ├── decisions/         Hồ sơ quyết định DEC-001 đến 173
  │   ├── verification/      Kết quả kiểm thử theo gói (community/, messaging/)
  │   ├── planning/          Kế hoạch và prompt cũ của DM
  │   ├── migrations/        Lịch sử tổ chức lại tài liệu
  │   └── templates/         Mẫu: hồ sơ phát hành, sự cố, thẻ Trello, họp tuần
  │
  └── archive/               Tài liệu cũ đã bị thay thế