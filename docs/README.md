# Tài liệu SCDC

Bộ tài liệu hiện hành được tổ chức theo dự án, tính năng và mốc bàn giao. Mỗi nội dung có một nguồn chuẩn; các bản đã thay thế nằm trong archive. Hợp nhất: 2026-10-03; tách MVP/v1: 2026-10-06; áp dụng bố cục specs/design/delivery cho Community: 2026-10-07.

**MVP** là mốc nhỏ đầu tiên gồm Identity, Community và Direct Messaging trong một API host Modular Monolith. **v1** chuyển sang microservice và hoàn thiện phạm vi vốn được gọi là MVP trước đây, gồm cả gọi điện/video/chia sẻ màn hình. Bắt đầu từ [lộ trình](roadmap.md), rồi chọn [gói MVP](releases/mvp.md#packages) để đọc đúng phần đặc tả cần triển khai; [v1](releases/v1.md) giữ phạm vi đầy đủ.

## Bắt đầu từ đâu

| Bạn cần làm gì | Đọc |
|---|---|
| Phân biệt MVP/v1 và chọn phần cần làm ngay | [Lộ trình](roadmap.md), [MVP](releases/mvp.md), [v1](releases/v1.md) |
| Hiểu mục tiêu, requirement, scope và kế hoạch | [Dự án](project.md) |
| Biết ai làm gì và cân tải theo từng giai đoạn | [Phân công](project.md#team), [công suất và quy tắc cân tải](project.md#capacity) |
| Tra cứu quyết định, căn cứ hoặc vấn đề chưa chốt | [Quyết định](decisions.md) |
| Thiết kế/tích hợp xuyên module | [Kiến trúc](architecture.md) |
| Chạy repo, chuẩn bị dữ liệu hoặc kiểm thử kỹ thuật | [Phát triển](development.md) |
| Triển khai một tính năng | Chọn gói trong hồ sơ MVP/v1; với Community, đọc [status](features/community/status.md), scope gói và đặc tả được dẫn chiếu |
| Tra cứu vòng đời dữ liệu, cleanup và bảo vệ sau restore | [Vòng đời dữ liệu](data-lifecycle.md) |
| Bàn giao MVP hoặc nghiệm thu/phát hành v1 | [Điều kiện MVP](releases/mvp.md#acceptance), [v1](releases/v1.md#acceptance), [phát hành và vận hành](release-operations.md) |
| Làm theo hướng dẫn deploy/rollback/sự cố/restore | [Runbook vận hành](operations-runbook.md) |
| Ghi kết quả kiểm thử/bàn giao hoặc xử lý sự cố | [Mẫu hồ sơ phát hành](templates/release-record.md), [mẫu hồ sơ sự cố](templates/incident-record.md) |

## Đặc tả tính năng

Các file dưới đây giữ đặc tả đầy đủ cho v1. MVP chọn các luồng cần làm trước tại [hồ sơ MVP](releases/mvp.md); không tạo bản sao đặc tả hoặc coi mọi UC/AC trong một file là phải hoàn tất ngay ở gói đầu.

| Tài liệu | Scope | Nội dung |
|---|---|---|
| [Tài khoản](features/accounts.md) | SCP-002 | Đăng ký/xác minh, đăng nhập, hồ sơ, mật khẩu, phiên; [12 use case và đối chiếu source/test](features/accounts.md#use-cases), API hiện tại |
| [Nhắn tin riêng](features/direct-messaging.md) | Phần DM của SCP-005 | Tìm người, hội thoại hai người, tin văn bản, thử lại, đồng thời và reconnect |
| [Cộng đồng](features/community/README.md) | SCP-003/004 và phần tin phòng của SCP-005 | [5 thành phần nghiệp vụ](features/community/design/README.md#organization); [25 use case theo hành trình, module phụ trách và truy vết API/AC/TC](features/community/specs/README.md#use-cases); [nguồn chuẩn/phối hợp Messaging](features/community/specs/integration.md#responsibilities), [gói triển khai đầu tiên](features/community/delivery/README.md#use-case-delivery) |
| [Thoại/video](features/voice-video.md) | SCP-006/007 | Gọi riêng, phòng thoại, giới hạn, LiveKit tự host, chất lượng và kiểm thử |

Tình trạng sẵn sàng theo scope được quản lý tại [project.md](project.md#coverage); mã quyết định ở [decisions.md](decisions.md#decisions). Bảng [tiến độ hoàn thiện tài liệu](project.md#coverage) ghi phần đã viết; [phần còn cần hoàn thiện](project.md#documentation-remaining) tách việc soạn tiếp, quyết định còn cần và bằng chứng thuộc triển khai. Không duy trì bảng tiến độ độc lập tại mục lục.

## Cách quản lý thông tin

- Scope xác định phạm vi bàn giao; requirement xác định hành vi/điều kiện cần đáp ứng. REQ/SCP/SUC ở project.md; quy tắc và AC/TC chi tiết ở từng tính năng.
- Hồ sơ trong `releases/` xác định phần bàn giao theo mốc và cách kiểm chứng; roadmap dẫn đường. Giữ một nguồn đặc tả cho mỗi tính năng, giữ nguyên mã truy vết; bản MVP phát triển tiếp thành v1.
- DEC/OQ quản lý kết luận và nội dung cần làm rõ; đặc tả dẫn chiếu các mã này. Giữ nguyên mã khi chuyển file, không cấp lại mã đã loại.
- Community tách quy tắc/use case/UX/AC/TC vào `specs/`, cách thực hiện vào `design/`, scope và kết quả kiểm chứng theo gói vào `delivery/`. [status.md](features/community/status.md) giữ trạng thái hiện tại; trang tổng quan chỉ dẫn liên kết. Các tính năng khác tiếp tục dùng bố cục hiện có trước khi được tổ chức lại.
- Hồ sơ nghiệm thu gắn commit đã kiểm chứng; chỉ có code, fixture hoặc danh mục ca test chưa đủ để ghi nhận đạt.
- API đã triển khai được đối chiếu với source/OpenAPI; phương án tương lai có nhãn đề xuất. Kiến trúc hiện tại và định hướng đợt sau được ghi riêng.
- Nội dung thay đổi được cập nhật ở nguồn chuẩn và các phần phụ thuộc trong cùng thay đổi. Trang tổng quan dẫn link, không chép một phiên bản quy tắc khác.

## Cấu trúc

```text
docs/
├── README.md
├── project.md
├── roadmap.md
├── releases/
│   ├── mvp.md                  # Mốc nhỏ để bắt đầu làm/tích hợp
│   └── v1.md                   # Bản hoàn thiện kế thừa phạm vi cũ
├── decisions.md
├── architecture.md
├── data-lifecycle.md
├── development.md
├── release-operations.md
├── operations-runbook.md
├── features/
│   ├── accounts.md
│   ├── direct-messaging.md
│   ├── community/
│   │   ├── README.md          # Tổng quan, điều hướng
│   │   ├── status.md          # Trạng thái triển khai hiện tại
│   │   ├── specs/             # Quy tắc, use case, UX, AC/TC và truy vết
│   │   ├── design/            # Model, API, transaction, schema
│   │   └── delivery/          # Kế hoạch và hồ sơ nghiệm thu từng gói
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
