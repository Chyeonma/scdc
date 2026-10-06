# Tài liệu SCDC

Bộ tài liệu hiện hành được tổ chức theo dự án và tính năng. Mỗi nội dung có một nguồn chuẩn; các bản đã thay thế nằm trong archive. Hợp nhất: 2026-10-03; cập nhật quyết định: 2026-10-05; cập nhật đặc tả: 2026-10-06.

## Bắt đầu từ đâu

| Bạn cần làm gì | Đọc |
|---|---|
| Hiểu mục tiêu, requirement, scope và kế hoạch | [Dự án](project.md) |
| Tra cứu quyết định, căn cứ hoặc vấn đề chưa chốt | [Quyết định](decisions.md) |
| Thiết kế/tích hợp xuyên module | [Kiến trúc](architecture.md) |
| Chạy repo, chuẩn bị dữ liệu hoặc kiểm thử kỹ thuật | [Phát triển](development.md) |
| Triển khai một tính năng | Mở đặc tả tương ứng bên dưới; Community có trang tổng quan dẫn tới đặc tả từng thành phần, gồm nghiệp vụ, UX, hợp đồng, AC và test |
| Tra cứu vòng đời dữ liệu, cleanup và bảo vệ sau restore | [Vòng đời dữ liệu](data-lifecycle.md) |
| Nghiệm thu, phát hành, vận hành | [Phát hành và vận hành](release-operations.md) |
| Làm theo hướng dẫn deploy/rollback/sự cố/restore | [Runbook vận hành](operations-runbook.md) |
| Ghi kết quả kiểm thử/bàn giao hoặc xử lý sự cố | [Mẫu hồ sơ phát hành](templates/release-record.md), [mẫu hồ sơ sự cố](templates/incident-record.md) |

## Đặc tả tính năng

| Tài liệu | Scope | Nội dung |
|---|---|---|
| [Tài khoản](features/accounts.md) | SCP-002 | Đăng ký/xác minh, đăng nhập, hồ sơ, mật khẩu, phiên; [12 use case và đối chiếu source/test](features/accounts.md#use-cases), API hiện tại |
| [Nhắn tin riêng](features/direct-messaging.md) | Phần DM của SCP-005 | Tìm người, hội thoại hai người, tin văn bản, thử lại, đồng thời và reconnect |
| [Cộng đồng](features/community.md) | SCP-003/004 và phần tin phòng của SCP-005 | [5 thành phần nghiệp vụ](features/community.md#organization), tích hợp dùng chung; [25 use case và đối chiếu API/AC/TC](features/community.md#use-cases), [gói triển khai đầu tiên](features/community.md#use-case-delivery) |
| [Thoại/video](features/voice-video.md) | SCP-006/007 | Gọi riêng, phòng thoại, giới hạn, LiveKit tự host, chất lượng và kiểm thử |

Tình trạng sẵn sàng theo scope được quản lý tại [project.md](project.md#coverage); mã quyết định ở [decisions.md](decisions.md#decisions). Bảng [tiến độ hoàn thiện tài liệu](project.md#coverage) ghi phần đã viết; [phần còn cần hoàn thiện](project.md#documentation-remaining) tách việc soạn tiếp, quyết định còn cần và bằng chứng thuộc triển khai. Không duy trì bảng tiến độ độc lập tại mục lục.

## Cách quản lý thông tin

- Scope xác định phạm vi bàn giao; requirement xác định hành vi/điều kiện cần đáp ứng. REQ/SCP/SUC ở project.md; quy tắc và AC/TC chi tiết ở từng tính năng.
- DEC/OQ quản lý kết luận và nội dung cần làm rõ; đặc tả dẫn chiếu các mã này. Giữ nguyên mã khi chuyển file, không cấp lại mã đã loại.
- Mỗi đặc tả ghi riêng mức xác nhận nghiệp vụ, trạng thái thiết kế, implementation và bằng chứng kiểm thử. “Có code” hoặc “có ca test” không đồng nghĩa “đã nghiệm thu”.
- API đã triển khai được đối chiếu với source/OpenAPI; phương án tương lai có nhãn đề xuất. Kiến trúc hiện tại và định hướng đợt sau được ghi riêng.
- Nội dung thay đổi được cập nhật ở nguồn chuẩn và các phần phụ thuộc trong cùng thay đổi. Trang tổng quan dẫn link, không chép một phiên bản quy tắc khác.

## Cấu trúc

```text
docs/
├── README.md
├── project.md
├── decisions.md
├── architecture.md
├── data-lifecycle.md
├── development.md
├── release-operations.md
├── operations-runbook.md
├── features/
│   ├── accounts.md
│   ├── direct-messaging.md
│   ├── community.md
│   ├── community/             # Servers, Memberships, Invitations, Channels, Permissions và tích hợp
│   └── voice-video.md
├── contracts/                 # Schema API; có nhãn draft khi chưa khóa thiết kế
├── fixtures/                  # Dữ liệu biên dùng chung; không phải kết quả test
├── templates/                 # Mẫu hồ sơ nghiệm thu/phát hành/sự cố, chưa có kết quả
└── archive/
```

Các artefact hiện có:

- [OpenAPI xác minh/khôi phục](contracts/account-recovery.openapi.json): 4 thao tác mục tiêu, tách rõ endpoint hiện có và resend chưa triển khai.
- [OpenAPI DM](contracts/direct-messaging.openapi.json): 7 thao tác REST, có cursor/resume và schema tin/tombstone.
- [Schema thông điệp chat](contracts/chat-realtime.schema.json): catalogue ứng dụng cho SignalR, không phải wire frame.
- [OpenAPI cộng đồng/quyền/tin phòng](contracts/community.openapi.json): 45 thao tác mục tiêu, gồm role/ACL, lời mời/yêu cầu, membership và channel text.
- [Schema realtime cộng đồng](contracts/community-realtime.schema.json): 9 loại thông điệp bổ sung cho Hub chat, có định tuyến/thu hồi theo scope.
- [OpenAPI media](contracts/media.openapi.json): 16 thao tác điều khiển công khai và 3 nội bộ; mọi endpoint chưa triển khai.
- [Schema realtime media](contracts/media-realtime.schema.json): 8 loại thông điệp trạng thái trên Hub chat; không chứa token/grant provider.
- [Fixture media](fixtures/media-lifecycle.json): 16 vector quota, 14 vector admission, 8 kịch bản transition và 4 byte/HMAC kỳ vọng; không phải bằng chứng SFU thật.
- [Fixture quyền cộng đồng](fixtures/community-permissions.json) và [fixture operation](fixtures/community-operations.json): 18 kết quả quyền, 6 kịch bản transition và 8 byte/hash HMAC kỳ vọng.
- [Schema sổ bảo vệ dữ liệu](contracts/data-protection.schema.json) và [fixture vòng đời](fixtures/data-lifecycle.json): record mục tiêu, 49 vector policy/access/restore, 10 trường hợp schema message và 6 kịch bản transition; chưa có kho sổ/worker thật.
- [Fixture nội dung](fixtures/text-validation.json), [bảng Unicode](fixtures/text-policy.json) và [fixture fingerprint](fixtures/dm-fingerprint.json): dữ liệu đối chiếu, không phải kết quả nghiệm thu.

[Bản đồ chuyển đổi và lịch sử](archive/README.md) cho biết nội dung file cũ đã chuyển về đâu. Các bản lưu dùng để tra cứu lịch sử, không dùng làm nguồn yêu cầu hiện hành.
