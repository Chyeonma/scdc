# SCDC — Hướng dẫn đọc tài liệu dự án

Tài liệu dự án SCDC được tổ chức theo **hai tầng**:

- **Tầng 1 (`docs/`):** Tài liệu tổng hợp theo tính năng, dễ tra cứu hàng ngày.
- **Tầng 2 (`docs/archive/project-specs/`):** Hồ sơ gốc theo quy trình phát triển, dùng khi cần truy xuất quyết định hoặc lịch sử.

---

## 📖 Bắt đầu từ đâu?

### Nếu bạn là **Dev Backend / Frontend**

1. Đọc [Tổng quan dự án](project/project-brief.md) để hiểu mục tiêu và phạm vi MVP.
2. Vào thư mục tính năng bạn được giao:
   - [`features/accounts/`](features/accounts/) — Đăng ký, Đăng nhập, Quản lý tài khoản
   - [`features/direct-messaging/`](features/direct-messaging/) — Chat 1-1 (DM)
   - [`features/community/`](features/community/) — Server, Channel, Phân quyền
   - [`features/voice-video/`](features/voice-video/) — Thoại, Video, Chia sẻ màn hình
3. Mỗi thư mục tính năng có đầy đủ: Yêu cầu → Giao diện → API → Test Cases.
4. Khi cần setup môi trường, xem [Hướng dẫn phát triển](guides/development.md).

### Nếu bạn là **Tester / QA**

1. Đọc [Tổng quan dự án](project/project-brief.md) để nắm phạm vi.
2. Vào thư mục tính năng cần test, mở file `test-cases.md`.
3. Đối chiếu với `requirements.md` trong cùng thư mục để hiểu kỳ vọng.

### Nếu bạn là **PM / BA / Stakeholder**

1. Đọc [Tổng quan dự án](project/project-brief.md).
2. Xem [Sổ quyết định](project/decision-log.md) để tra cứu mọi quyết định đã chốt (DEC-*).
3. Xem [Tiến độ chuẩn bị](project/development-readiness.md) để biết trạng thái hiện tại.

---

## 🗂️ Cấu trúc thư mục

```
docs/
├── README.md                          ← Bạn đang ở đây
│
├── project/                           ← Tài liệu cấp dự án (đọc 1 lần)
│   ├── project-brief.md               ← Mục tiêu, phạm vi, MVP
│   ├── decision-log.md                ← Sổ quyết định (DEC-*) & vấn đề mở (OQ-*)
│   ├── team-and-roles.md              ← Nhân sự, phân công
│   ├── budget.md                      ← Ngân sách, giả định, rủi ro
│   ├── process.md                     ← Quy trình phát triển phần mềm
│   ├── requirements-coverage.md       ← Bản đồ độ phủ đặc tả
│   └── development-readiness.md       ← Điều kiện sẵn sàng phát triển (Đợt 0)
│
├── features/                          ← Tài liệu theo TÍNH NĂNG (đọc hàng ngày)
│   ├── accounts/                      ← Tính năng: Tài khoản
│   │   ├── README.md                  ← Tổng quan + link nhanh
│   │   ├── requirements.md            ← Usecase, quy tắc, tiêu chí chấp nhận
│   │   ├── wireframes.md              ← Phác thảo giao diện
│   │   └── api-contracts.md           ← Hợp đồng API
│   │
│   ├── direct-messaging/              ← Tính năng: Nhắn tin riêng (DM)
│   │   ├── README.md
│   │   ├── requirements.md
│   │   ├── wireframes.md
│   │   ├── api-contracts.md
│   │   └── test-cases.md
│   │
│   ├── community/                     ← Tính năng: Cộng đồng & Phòng
│   │   ├── README.md
│   │   ├── requirements.md
│   │   ├── wireframes.md
│   │   └── access-control.md          ← Ma trận phân quyền
│   │
│   └── voice-video/                   ← Tính năng: Thoại / Video
│       ├── README.md
│       └── requirements.md
│
├── architecture/                      ← Tài liệu kỹ thuật xuyên suốt
│   ├── system-overview.md             ← Kiến trúc tổng thể + sơ đồ
│   └── solution-outline.md            ← Phác thảo giải pháp kỹ thuật
│
├── guides/                            ← Hướng dẫn thực hành
│   └── development.md                 ← Setup, quy ước code, Git workflow
│
├── research/                          ← Nghiên cứu (đọc khi cần)
│   ├── user-needs.md                  ← Người dùng và nhu cầu
│   └── customer-request.md            ← Yêu cầu ban đầu từ khách hàng
│
└── archive/project-specs/             ← Hồ sơ gốc theo quy trình (lưu trữ)
    ├── README.md                      ← Danh mục hồ sơ đầy đủ
    └── ...                            ← Tất cả tài liệu gốc giữ nguyên
```

---

## 🔗 Link nhanh

| Tài liệu | Mô tả |
|---|---|
| [Tổng quan dự án](project/project-brief.md) | Mục tiêu, phạm vi MVP, tiêu chí thành công |
| [Sổ quyết định](project/decision-log.md) | 59 quyết định (DEC-*) và 13 vấn đề mở (OQ-*) |
| [Tài khoản](features/accounts/) | Đăng ký, đăng nhập, xác minh email, hồ sơ |
| [Nhắn tin riêng (DM)](features/direct-messaging/) | Chat 1-1, sửa/xóa tin, chống trùng |
| [Cộng đồng](features/community/) | Server, Channel, lời mời, phân quyền Bitwise RBAC |
| [Thoại/Video](features/voice-video/) | Cuộc gọi riêng, phòng thoại, chia sẻ màn hình |
| [Kiến trúc](architecture/system-overview.md) | Modular Monolith, SignalR, Cursor Pagination |
| [Hướng dẫn Dev](guides/development.md) | Docker, quy ước C#, Result\<T\>, Git |
