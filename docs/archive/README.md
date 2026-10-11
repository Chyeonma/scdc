# Lịch sử tài liệu SCDC

Tài liệu hiện hành bắt đầu từ [docs/README.md](../README.md). Nội dung được hợp nhất ngày 2026-10-03 và chuyển sang cấu trúc theo chủ đề ngày 2026-10-11. Các file dưới archive là bản lịch sử, kể cả những file còn dùng từ “hiện hành”, “đang áp dụng” hoặc đường dẫn của cấu trúc cũ.

## Các bản lưu

| Bản lưu | Nội dung |
|---|---|
| [Trước khi hợp nhất](2026-10-03-before-consolidation/ARCHIVE-NOTE.md) | README/docs hiện hành cũ và README module giữ nguyên byte; MANIFEST.sha256 để kiểm tra |
| [Hồ sơ quy trình gốc](project-specs/README.md) | Requirements, scope, quyết định, UX, kiến trúc, kế hoạch và kiểm thử trước hợp nhất; giữ nguyên nội dung |
| [Trước khi căn chỉnh quy trình](project-specs/archive/2026-10-03-before-process-alignment/ARCHIVE-NOTE.md) | Snapshot lịch sử đã có từ trước; không chỉnh sửa trong lần này |

## Bản đồ chuyển đổi

| Mã hồ sơ gốc | Nguồn cũ | Nguồn hiện hành |
|---|---|---|
| SCDC-REQ-001 | `project-specs/01-initiation/00-customer-request.md` | [Dự án](../project/overview.md#requirements) |
| SCDC-BRF-001 | `project-specs/01-initiation/01-project-brief.md` | [Dự án](../project/overview.md#scope) |
| SCDC-EST-001 | `project-specs/01-initiation/02-budget-and-assumptions.md` | [Dự án](../project/planning.md#budget) |
| SCDC-ORG-001 | `project-specs/01-initiation/04-team-and-discovery-plan.md` | [Dự án](../project/planning.md#team) |
| SCDC-DIS-001 | `project-specs/02-discovery/01-users-and-needs.md` | [Dự án](../project/overview.md#needs) |
| SCDC-FR-INDEX-001 | `project-specs/03-requirements/00-requirements-coverage.md` | [Dự án](../README.md#sources) |
| SCDC-PLAN-001 | `project-specs/06-planning/01-delivery-plan.md` | [Dự án](../project/planning.md#delivery) |
| SCDC-READY-001 | `project-specs/06-planning/02-development-readiness.md` | [Dự án](../project/workflow.md#readiness) |
| SCDC-PRC-001 | `project-specs/development-process.md` | [Dự án](../project/workflow.md#process) |
| SCDC-LOG-001 | `project-specs/01-initiation/03-discovery-and-decision-log.md` | [Hồ sơ quyết định](../records/decisions/README.md#decisions) |
| SCDC-FR-DM-001 | `project-specs/03-requirements/01-direct-messaging.md` | [Nhắn tin riêng](../features/messaging/direct-messaging.md#requirements) |
| SCDC-FR-COM-001 | `project-specs/03-requirements/02-community-join-and-channels.md` | [Community](../features/community/README.md#mục-lục) |
| SCDC-FR-ACC-001 | `project-specs/03-requirements/03-accounts.md` | [Tài khoản](../features/accounts/README.md) |
| SCDC-FR-MEDIA-001 | `project-specs/03-requirements/04-voice-video-screen-sharing.md` | [Media](../features/media/README.md) |
| SCDC-FR-ACL-001 | `project-specs/03-requirements/05-access-control-matrix.md` | [Community](../features/community/access-control.md#permissions) |
| SCDC-UX-001 | `project-specs/04-ux/01-core-user-flows.md` | [Kiến trúc và UX](../experience/overview.md#journeys) |
| SCDC-UX-DM-001 | `project-specs/04-ux/02-account-dm-wireframes.md` | [Nhắn tin riêng](../features/messaging/direct-messaging.md#ux) |
| SCDC-UX-COM-001 | `project-specs/04-ux/03-community-wireframes.md` | [Community](../system/community.md#ux) |
| SCDC-ARC-001 | `project-specs/05-architecture/01-solution-outline.md` | [Kiến trúc và UX](../system/architecture.md#boundaries) |
| SCDC-API-DM-001 | `project-specs/05-architecture/02-account-dm-contracts.md` | [Nhắn tin riêng](../features/messaging/direct-messaging.md#contracts) |
| SCDC-QA-001 | `project-specs/07-quality/01-test-and-acceptance-plan.md` | [Nghiệm thu](../releases/acceptance.md#testing) |
| SCDC-QA-DM-001 | `project-specs/07-quality/02-account-dm-test-cases.md` | [Nhắn tin riêng](../features/messaging/direct-messaging.md#tests) |
| SCDC-DIS-002 | `project-specs/02-discovery/02-interview-guide.md` | [Bộ câu hỏi dự phòng, giữ trong lịch sử](project-specs/02-discovery/02-interview-guide.md) |

Hợp đồng tài khoản/DM và wireframe cũ dùng chung một file đã được tách nội dung theo tính năng: phần Accounts ở [Tài khoản](../features/accounts/README.md), phần DM ở [Nhắn tin riêng](../features/messaging/README.md). Ca TC-ACC nằm ở Accounts, TC-DM/TEXT nằm ở DM, TC-ACL nằm ở Community. ACL-01 ở Accounts, ACL-02–05 ở DM, ACL-06–19 ở Community.

Các trang tóm tắt cũ trong `project/`, `research/`, `guides/`, `architecture/` và thư mục từng tính năng được lưu trong snapshot rồi thay bằng nguồn hiện hành. Ngân sách/nhân sự/kế hoạch tại [kế hoạch](../project/planning.md); điều kiện triển khai tại [quy trình](../project/workflow.md#readiness). Quy tắc quyền Community được giữ theo DEC-055–058, không theo mô tả ALLOW/DENY không nhất quán của kiến trúc cũ.

REQ-012/SUC-005 được cập nhật theo DEC-060; DEC-007 giữ lịch sử bị thay thế. Định dạng lỗi cập nhật theo DEC-061; tên hiển thị bắt buộc khi đăng ký được đồng bộ theo source/DEC-062. Những đề xuất khác giữ trạng thái cần rà soát.

## Tổ chức lại Community — 2026-10-07

Trang `features/community.md` được tách thành [tổng quan](../features/community/README.md), [truy vết](../features/community/README.md), [thiết kế](../system/community.md), [các gói](../records/verification/community/README.md) và [trạng thái](../features/community/README.md). Năm thành phần cùng điều kiện/use case tích hợp chuyển vào `specs/`; phần dữ liệu/API/giao dịch chuyển vào `design/`.

`create-view-design.md` chuyển thành `design/create-view.md`; hai báo cáo backend/UI được hợp nhất vào `delivery/create-view/acceptance.md` với commit đã thử. Rà soát nghiệp vụ và scope/tiêu chí gói đầu nằm ở `delivery/create-view/plan.md`. Mã UC/COM/AC/TC/ACL/COM-SQL và quy tắc giữ nguyên. Git giữ bản trước chuyển đổi; các snapshot lịch sử và MANIFEST hiện có giữ nguyên byte.

## Thống nhất bố cục docs — 2026-10-07

| Nguồn trước chuyển đổi | Nguồn hiện hành |
|---|---|
| `docs/project.md`: needs/requirements/scope/success | [Tổng quan dự án](../project/overview.md) |
| `docs/project.md`: team/budget/delivery/capacity | [Kế hoạch và nguồn lực](../project/planning.md) |
| `docs/project.md`: coverage/readiness/preparation/technical-evidence | [Readiness](../project/workflow.md#readiness) |
| `docs/project.md`: process | [Quy trình](../project/workflow.md) |
| `docs/features/accounts.md` | [Accounts](../features/accounts/README.md): specs, design, status và delivery |
| `docs/features/direct-messaging.md`: hành trình/quyền/API/AC-DM/TC-DM | [DM](../features/messaging/README.md): specs, design, status và delivery |
| `docs/features/direct-messaging.md`: cơ chế tin dùng chung, TC-TEXT | [Messaging](../features/messaging/README.md): text, persistence và realtime |
| `docs/features/voice-video.md` | [Media](../features/media/README.md): specs, design, status và delivery |
| `docs/features/community/specs/README.md`: ma trận COM/UC/API/AC/TC | [Truy vết Community](../features/community/traceability.md) |

Giữ nguyên mã REQ/SCP/SUC/ACC/UC/DM/COM/AC/TC/ACL/DEC/OQ và nội dung quy tắc.
`decisions.md` được nhóm theo chủ đề; trạng thái implementation nằm tại status của từng tính năng.
Git giữ phiên bản trước chuyển đổi. Các snapshot lịch sử và MANIFEST hiện có giữ nguyên byte;
không tạo thêm bản sao toàn bộ docs cho đợt chuyển này.

## Cấu trúc theo chủ đề — 2026-10-11

[Bản đồ chuyển đổi](../records/migrations/2026-10-11.md) ghi nguồn và đích sau khi hợp nhất quy tắc, UX, dữ liệu/API, thiết kế và hiện trạng vào từng chủ đề. Hồ sơ kiểm chứng giữ revision và kết quả lịch sử. Các snapshot và MANIFEST dưới archive giữ nguyên.
