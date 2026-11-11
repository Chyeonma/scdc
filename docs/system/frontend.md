# SCDC — Tổ chức frontend

WebClient là ứng dụng React 19 build bằng Vite. Trang này mô tả cấu trúc thư mục và các quy tắc khi viết code giao diện.

**Trạng thái:** trong nền `900e226`, [App.jsx](../../clients/WebClient/src/App.jsx) đã ghép điều hướng, phiên đăng nhập, Community và DM với API thật ([hồ sơ tích hợp](../records/verification/messaging/merge-2026-10-13.md)). Phiên bản thư viện xem [package.json](../../clients/WebClient/package.json).

## Cấu trúc hiện tại

```text
clients/WebClient/
├── src/
│   ├── App.jsx                 # Ghép toàn bộ ứng dụng
│   ├── api.js                  # Gọi HTTP, đăng nhập, làm mới token
│   ├── components/             # Thành phần giao diện
│   ├── community/              # API, hook và màn hình Community
│   ├── useConversationInbox.js # Hộp thư DM của người dùng hiện tại
│   ├── dmHistory.js            # Gộp lịch sử tin và phiên bản tin
│   ├── dmRealtime.js           # Hub, kết nối lại và bù dữ liệu
│   ├── dmRecovery.js           # Tải lại lịch sử sau khi kết nối lại
│   ├── dmSendOperation.js      # Gửi tin và gửi lại chủ động
│   ├── dmTextPolicy.js         # Kiểm tra nội dung tin
│   ├── styles.css              # Biến CSS và style
│   └── mockData.js             # Dữ liệu mẫu cho phần chưa nối API
├── tests/                      # Test Node
└── e2e/                        # Test Playwright, chạy với API và DB thật
```

## Trách nhiệm giao diện

Mỗi người thiết kế và làm giao diện cho mảng của mình ([phân công](../project/planning.md#team)). Trưởng nhóm thống nhất điều hướng, bố cục và thành phần dùng chung. Mỗi người tự chuẩn bị kiểm thử giao diện.

[Mobile](../experience/mobile.md) đã có trong kế hoạch nhưng chưa chọn nền tảng; cấu trúc WebClient hiện tại không bao gồm app mobile.

## Quy tắc khi viết code

- Màn hình, trạng thái và thông báo được mô tả trong tài liệu tính năng. Điều hướng xem [trải nghiệm tổng thể](../experience/overview.md); thành phần chung xem [quy ước giao diện](../experience/design-system.md).
- Khi một tính năng có nhiều màn hình hoặc cần xử lý tranh chấp, tách phần gọi API và quản lý trạng thái ra khỏi phần hiển thị.
- Bỏ qua phản hồi đến trễ nếu nó thuộc người dùng hoặc ngữ cảnh cũ. Khi đăng xuất hoặc mất quyền, xóa trạng thái của ngữ cảnh đó.
- Hàm gọi API hiện tự thử lại sau lỗi 401 (làm mới token). Thao tác cần chống trùng hoặc chỉ cho người dùng tự gửi lại phải cấu hình theo [vòng đời tin](../features/messaging/message-lifecycle.md).
- Không dùng dữ liệu mẫu để giả vờ một luồng API đã chạy. Dữ liệu mẫu phải phân biệt rõ với dữ liệu từ backend.

## Kiểm thử

- Test Node kiểm tra logic và hàm gọi API.
- Playwright kiểm tra hành trình người dùng trong môi trường được chỉ định.
- Build thành công không chứng minh giao diện, API, DB hay realtime chạy đúng.

Lệnh chạy xem [hướng dẫn phát triển](../guides/development.md#testing). Kết quả theo commit xem hồ sơ [Community](../records/verification/community/README.md), [DM](../records/verification/messaging/README.md) và [tích hợp](../records/verification/messaging/merge-2026-10-13.md).

---

Nguồn: [DEC-171](../records/decisions/README.md#dec-171), DEC-173.
