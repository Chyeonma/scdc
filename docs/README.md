# SCDC — Tài liệu dự án

Bộ tài liệu hiện hành mô tả dự án với **v1 là sản phẩm hoàn thiện đầu tiên**. Nội dung tổ chức theo chủ đề; mỗi chủ đề giữ quy tắc, UI/UX, dữ liệu/API, thiết kế xử lý, tiêu chí kiểm chứng và trạng thái triển khai. Phần sau v1 được ghi rõ tại chủ đề tương ứng. Cập nhật định hướng tài liệu ngày 2026-10-13.

[MVP](releases/mvp.md) là hồ sơ mốc nội bộ, ghi snapshot nền phát triển và phần việc chuyển tiếp sang v1. Nhóm hoàn thiện tài liệu và triển khai các gói v1 từ nền đã bàn giao theo [quy trình](project/workflow.md#process).

<a id="contents"></a>

## Mục lục

| Nội dung | Tài liệu |
|---|---|
| Mục tiêu, yêu cầu và phạm vi | [Tổng quan dự án](project/overview.md) |
| Nhân sự, công suất, công việc, lịch và dự toán | [Kế hoạch](project/planning.md) |
| Trình tự thiết kế trước code và điều kiện triển khai | [Quy trình](project/workflow.md) |
| Vấn đề cần xác định | [Danh mục OQ](project/open-questions.md) |
| Hành trình, điều hướng và bố cục | [Trải nghiệm tổng thể](experience/overview.md) |
| Thành phần và quy ước UI dùng chung | [Quy ước giao diện](experience/design-system.md) |
| Thành phần hệ thống và sở hữu dữ liệu | [Kiến trúc](system/architecture.md) |
| Tổ chức code | [Backend](system/backend.md), [Frontend](system/frontend.md) |
| Thiết kế dữ liệu và môi trường | [Database](system/database.md), [Môi trường](system/environments.md) |
| HTTP, gRPC, lỗi và schema | [Quy ước API](system/api-conventions.md), [Hợp đồng máy đọc](contracts/README.md) |
| Phiên, quyền, retention và restore xuyên hệ thống | [Thu hồi truy cập](system/access-revocation.md), [Vòng đời dữ liệu](system/data-lifecycle.md) |
| Tài khoản | [Accounts](features/accounts/README.md) |
| Server, membership, lời mời, phòng và quyền | [Community](features/community/README.md) |
| DM, tin phòng và nền nhắn tin | [Messaging](features/messaging/README.md) |
| Gọi riêng, phòng thoại, video và chia sẻ màn hình | [Media](features/media/README.md) |
| Bàn giao sản phẩm v1 và điều kiện nghiệm thu | [v1](releases/v1.md), [Nghiệm thu](releases/acceptance.md) |
| Nền nội bộ và công việc chuyển tiếp sang v1 | [Snapshot MVP](releases/mvp.md) |
| Ngưỡng và cách đo | [Chất lượng](system/quality.md) |
| Thu thập log, chỉ số và trace | [Giám sát hệ thống](system/observability.md) |
| Dựng, chạy và kiểm thử | [Development](guides/development.md), [Community](guides/community-development.md) |
| Deploy, rollback, sự cố và restore | [Vận hành](guides/operations.md) |
| Quyết định và kết quả theo revision | [Quyết định](records/decisions/README.md), [Kiểm chứng Community](records/verification/community/README.md) |
| Mẫu hồ sơ | [Bàn giao/phát hành](records/templates/release-record.md), [Sự cố](records/templates/incident-record.md) |
| Dữ liệu biên và lịch sử bố cục | [Fixtures](fixtures/README.md), [Chuyển đổi tài liệu](records/migrations/2026-10-11.md), [Archive](archive/README.md) |

<a id="sources"></a>

## Nguồn nội dung

| Loại thông tin | Nơi định nghĩa |
|---|---|
| REQ/SCP/SUC và thuật ngữ sản phẩm | `project/overview.md` |
| Người, công suất, công việc, lịch và COST/AS/RSK | `project/planning.md` |
| Quy tắc, luồng, UI state, dữ liệu/API, transaction và AC/TC của chủ đề | Tài liệu chủ đề trong `features/` |
| Navigation, bố cục và component chung | `experience/` |
| Ranh giới, quy ước và cơ chế xuyên chủ đề | `system/` |
| Hồ sơ bàn giao v1, điều kiện nghiệm thu và snapshot nền nội bộ MVP | `releases/` |
| Các bước thao tác | `guides/`; README kỹ thuật giữ hướng dẫn riêng của thành phần |
| Schema máy đọc và dữ liệu kiểm chứng | `contracts/`, `fixtures/`; mỗi artefact có chủ đề sở hữu |
| SQL, Compose, cấu hình và khung code thực thi | `database/`, `services/`, `clients/`, scripts và cấu hình trong repo |
| Lý do/quyết định và bằng chứng lịch sử | `records/` |

Trạng thái hiện tại ghi theo khả năng trong tài liệu chủ đề; README không duy trì bảng tiến độ thứ hai. Hồ sơ kiểm chứng giữ commit/build, môi trường, phạm vi và actual result. Có code, schema, mockup hoặc fixture chưa đủ để ghi đạt nghiệm thu.

Hồ sơ bàn giao v1 dẫn tới yêu cầu, UC/AC và điều kiện nghiệm thu tại nguồn chuẩn. Snapshot MVP ghi nhận nền nội bộ và giới hạn kiểm chứng; phần việc còn thiếu tiếp tục trong v1. Quyết định lưu bối cảnh và lịch sử; hành vi đang có hiệu lực phải đọc được trong chủ đề. Giữ mã truy vết khi đổi vị trí.

<a id="cấu-trúc"></a>

## Cấu trúc

```text
docs/
├── README.md
├── project/                 # Mục tiêu, kế hoạch, quy trình, OQ
├── experience/              # UX tổng thể và UI dùng chung
├── system/                  # Kiến trúc, code, dữ liệu, môi trường, cơ chế chung
├── features/
│   ├── accounts/
│   ├── community/
│   ├── messaging/
│   └── media/
├── releases/                # Bàn giao v1, nghiệm thu và snapshot MVP nội bộ
├── guides/                  # Development và vận hành
├── contracts/               # OpenAPI, JSON Schema và nguồn Protobuf mục tiêu
├── fixtures/                # Dữ liệu kiểm chứng dùng chung
├── records/
│   ├── decisions/
│   ├── verification/
│   ├── planning/
│   ├── migrations/
│   └── templates/
└── archive/                 # Snapshot đã thay thế
```

## Trình tự phát triển

Phạm vi gói → hành vi/UX/AC → API/dữ liệu/module/transaction → rà soát nhất quán → SQL/cấu hình/khung code → kiểm tra nền → code nghiệp vụ/kiểm chứng. Chi tiết tại [workflow](project/workflow.md#process).

Chỉ hoàn thiện đầu vào trực tiếp của gói sắp làm. Tiêu đề dùng tên nội dung hoặc hành động trực tiếp; câu ngắn, thuật ngữ nhất quán, giả định/đề xuất có nhãn.
