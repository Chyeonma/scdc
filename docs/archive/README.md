# Lịch sử tài liệu SCDC

Tài liệu hiện hành bắt đầu từ [docs/README.md](../README.md). Nội dung đã được hợp nhất ngày 2026-10-03. Các file dưới archive là bản lịch sử, kể cả những file còn dùng từ “hiện hành”, “đang áp dụng” hoặc đường dẫn của cấu trúc cũ.

## Các bản lưu

| Bản lưu | Nội dung |
|---|---|
| [Trước khi hợp nhất](2026-10-03-before-consolidation/ARCHIVE-NOTE.md) | README/docs hiện hành cũ và README module giữ nguyên byte; MANIFEST.sha256 để kiểm tra |
| [Hồ sơ quy trình gốc](project-specs/README.md) | Requirements, scope, quyết định, UX, kiến trúc, kế hoạch và kiểm thử trước hợp nhất; giữ nguyên nội dung |
| [Trước khi căn chỉnh quy trình](project-specs/archive/2026-10-03-before-process-alignment/ARCHIVE-NOTE.md) | Snapshot lịch sử đã có từ trước; không chỉnh sửa trong lần này |

## Bản đồ chuyển đổi

| Mã hồ sơ gốc | Nguồn cũ | Nguồn hiện hành |
|---|---|---|
| SCDC-REQ-001 | `project-specs/01-initiation/00-customer-request.md` | [project.md](../project.md#requirements) |
| SCDC-BRF-001 | `project-specs/01-initiation/01-project-brief.md` | [project.md](../project.md#scope) |
| SCDC-EST-001 | `project-specs/01-initiation/02-budget-and-assumptions.md` | [project.md](../project.md#budget) |
| SCDC-ORG-001 | `project-specs/01-initiation/04-team-and-discovery-plan.md` | [project.md](../project.md#team) |
| SCDC-DIS-001 | `project-specs/02-discovery/01-users-and-needs.md` | [project.md](../project.md#needs) |
| SCDC-FR-INDEX-001 | `project-specs/03-requirements/00-requirements-coverage.md` | [project.md](../project.md#coverage) |
| SCDC-PLAN-001 | `project-specs/06-planning/01-delivery-plan.md` | [project.md](../project.md#delivery) |
| SCDC-READY-001 | `project-specs/06-planning/02-development-readiness.md` | [project.md](../project.md#readiness) |
| SCDC-PRC-001 | `project-specs/development-process.md` | [project.md](../project.md#process) |
| SCDC-LOG-001 | `project-specs/01-initiation/03-discovery-and-decision-log.md` | [decisions.md](../decisions.md#decisions) |
| SCDC-FR-DM-001 | `project-specs/03-requirements/01-direct-messaging.md` | [features/direct-messaging.md](../features/direct-messaging.md#requirements) |
| SCDC-FR-COM-001 | `project-specs/03-requirements/02-community-join-and-channels.md` | [features/community.md](../features/community/README.md#requirements) |
| SCDC-FR-ACC-001 | `project-specs/03-requirements/03-accounts.md` | [features/accounts.md](../features/accounts.md#requirements) |
| SCDC-FR-MEDIA-001 | `project-specs/03-requirements/04-voice-video-screen-sharing.md` | [features/voice-video.md](../features/voice-video.md#requirements) |
| SCDC-FR-ACL-001 | `project-specs/03-requirements/05-access-control-matrix.md` | [features/community.md](../features/community/specs/permissions.md#permissions) |
| SCDC-UX-001 | `project-specs/04-ux/01-core-user-flows.md` | [architecture.md](../architecture.md#journeys) |
| SCDC-UX-DM-001 | `project-specs/04-ux/02-account-dm-wireframes.md` | [features/direct-messaging.md](../features/direct-messaging.md#ux) |
| SCDC-UX-COM-001 | `project-specs/04-ux/03-community-wireframes.md` | [features/community.md](../features/community/specs/integration.md#ux) |
| SCDC-ARC-001 | `project-specs/05-architecture/01-solution-outline.md` | [architecture.md](../architecture.md#boundaries) |
| SCDC-API-DM-001 | `project-specs/05-architecture/02-account-dm-contracts.md` | [features/direct-messaging.md](../features/direct-messaging.md#contracts) |
| SCDC-QA-001 | `project-specs/07-quality/01-test-and-acceptance-plan.md` | [release-operations.md](../release-operations.md#testing) |
| SCDC-QA-DM-001 | `project-specs/07-quality/02-account-dm-test-cases.md` | [features/direct-messaging.md](../features/direct-messaging.md#tests) |
| SCDC-DIS-002 | `project-specs/02-discovery/02-interview-guide.md` | [Bộ câu hỏi dự phòng, giữ trong lịch sử](project-specs/02-discovery/02-interview-guide.md) |

Hợp đồng tài khoản/DM và wireframe cũ dùng chung một file đã được tách nội dung theo tính năng: phần Accounts ở [accounts.md](../features/accounts.md), phần DM ở [direct-messaging.md](../features/direct-messaging.md). Ca TC-ACC nằm ở Accounts, TC-DM/TEXT nằm ở DM, TC-ACL nằm ở Community. ACL-01 ở Accounts, ACL-02–05 ở DM, ACL-06–19 ở Community.

Các trang tóm tắt cũ trong `project/`, `research/`, `guides/`, `architecture/` và thư mục từng tính năng được lưu trong snapshot rồi thay bằng nguồn hiện hành. Ngân sách/nhân sự/kế hoạch/readiness nằm ở project.md. Quy tắc quyền Community được giữ theo DEC-055–058, không theo mô tả ALLOW/DENY không nhất quán của kiến trúc cũ.

REQ-012/SUC-005 được cập nhật theo DEC-060; DEC-007 giữ lịch sử bị thay thế. Định dạng lỗi cập nhật theo DEC-061; tên hiển thị bắt buộc khi đăng ký được đồng bộ theo source/DEC-062. Những đề xuất khác giữ trạng thái cần rà soát.

## Tổ chức lại Community — 2026-10-07

Trang `features/community.md` được tách thành [tổng quan](../features/community/README.md), [truy vết](../features/community/specs/README.md), [thiết kế](../features/community/design/README.md), [các gói](../features/community/delivery/README.md) và [trạng thái](../features/community/status.md). Năm thành phần cùng điều kiện/use case tích hợp chuyển vào `specs/`; phần dữ liệu/API/giao dịch chuyển vào `design/`.

`create-view-design.md` chuyển thành `design/create-view.md`; hai báo cáo backend/UI được hợp nhất vào `delivery/create-view/acceptance.md` với commit đã thử. Rà soát nghiệp vụ và scope/tiêu chí gói đầu nằm ở `delivery/create-view/plan.md`. Mã UC/COM/AC/TC/ACL/COM-SQL và quy tắc giữ nguyên. Git giữ bản trước chuyển đổi; các snapshot lịch sử và MANIFEST hiện có giữ nguyên byte.
