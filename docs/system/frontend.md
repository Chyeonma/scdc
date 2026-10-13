# SCDC — Tổ chức frontend

## Cấu trúc hiện tại

WebClient nền hợp nhất `900e226` dùng React 19/Vite; phiên bản dependency tại [package.json](../../clients/WebClient/package.json). [App.jsx](../../clients/WebClient/src/App.jsx) ghép điều hướng, phiên, Community và DM thật theo [hồ sơ tích hợp](../records/verification/messaging/merge-2026-10-13.md).

```text
clients/WebClient/
├── src/
│   ├── App.jsx              # Ghép ứng dụng
│   ├── api.js               # HTTP, auth/session và refresh
│   ├── components/          # Thành phần giao diện
│   ├── community/           # API/hooks/views của Community
│   ├── useConversationInbox.js # Inbox DM theo actor
│   ├── dmHistory.js         # Hợp nhất lịch sử/version
│   ├── dmRealtime.js        # Hub, reconnect và bù dữ liệu
│   ├── dmRecovery.js        # Phục hồi lịch sử sau reconnect
│   ├── dmSendOperation.js   # Operation gửi/retry chủ động
│   ├── dmTextPolicy.js      # Validation nội dung DM
│   ├── styles.css           # Token và CSS hiện tại
│   └── mockData.js          # Dữ liệu mẫu của phần chưa tích hợp
├── tests/                   # Node tests
└── e2e/                     # Playwright với API/DB theo bộ chạy
```

## Quy tắc triển khai

- Màn hình, state và thông báo thuộc tài liệu chủ đề. Navigation tại [trải nghiệm tổng thể](../experience/overview.md); thành phần dùng chung tại [design system](../experience/design-system.md).
- Tách lời gọi API và xử lý trạng thái khỏi phần hiển thị khi một chủ đề cần nhiều màn hình hoặc concurrency recovery.
- Bỏ response trễ thuộc actor/scope cũ. Logout hoặc mất quyền dọn state của scope tương ứng.
- Wrapper refresh hiện có hành vi retry sau 401. Mutation có yêu cầu idempotency hoặc retry chủ động phải dùng cấu hình theo [vòng đời tin](../features/messaging/message-lifecycle.md).
- Không dùng mock data để báo một luồng API đã hoạt động. Dữ liệu mẫu phải phân biệt với kết quả từ backend.

## Kiểm chứng

Node tests kiểm tra logic/wrapper; Playwright kiểm tra hành trình trong môi trường được chỉ định. Build không thay bằng chứng UI/API/DB hoặc realtime. Lệnh chạy tại [Development](../guides/development.md#testing), kết quả gắn commit tại [hồ sơ Community](../records/verification/community/README.md), [DM](../records/verification/messaging/README.md) và [tích hợp](../records/verification/messaging/merge-2026-10-13.md).
