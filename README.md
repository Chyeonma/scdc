# SCDC — Nền tảng giao tiếp trên web

SCDC phát triển ứng dụng giao tiếp cho nhóm bạn và cộng đồng. [MVP](docs/releases/mvp.md) bàn giao trước với Identity, Community và Direct Messaging; [v1](docs/releases/v1.md) là bản hoàn thiện kế thừa phạm vi đầy đủ, gồm thoại/video và chia sẻ màn hình. [Lộ trình](docs/roadmap.md) giải thích cách đọc và quản lý tài liệu theo hai mốc.

MVP giữ một API host Modular Monolith; **microservice thuộc v1** theo [DEC-116](docs/decisions.md#dec-116) để đáp ứng môn học. Ranh giới service cụ thể còn cần chốt. [Trạng thái Community](docs/features/community/status.md) ghi phần đã triển khai, nhánh code và bằng chứng theo gói. [Kiến trúc](docs/architecture.md) ghi hiện trạng và phương án mục tiêu; [phân công](docs/project.md#team) và [công suất](docs/project.md#capacity) ghi gói việc của ba thành viên.

## Khởi chạy nhanh

Chuẩn bị Docker hoặc Podman với Compose, chạy từ root repo:

Trước lần chạy đầu, tạo một khóa bằng `openssl rand -base64 32`, lưu giá trị vào `COMMUNITY_OPERATION_KEY` trong `.env` local (Git bỏ qua file này). Giữ khóa qua các lần chạy; Compose dùng volume `scdc-community-keys` để giữ khóa cursor. Với DB cũ, áp [migration explicit](services/Modules/Community/README.md#migration) trước khi gọi API Community.

```bash
docker compose up -d --build
# Nếu dùng Podman:
podman compose up -d --build
```

| Thành phần | Địa chỉ |
|---|---|
| WebClient — React 19 / Vite | `http://localhost:3000` |
| Backend — .NET 10 / Swagger | `http://localhost:5026/swagger` |
| Health | `http://localhost:5026/api/v1/health` |
| PostgreSQL 18 | `localhost:5432`, database `scdc_chat`, user `scdc`, password `scdc_dev` |

Compose và các thông tin kết nối trên phục vụ Development. Hướng dẫn debug backend/frontend, tạo tài khoản local, dữ liệu và kiểm thử tại [development.md](docs/development.md).

## Lệnh ngắn

Repo có [Makefile](Makefile) để chạy các tác vụ thường dùng. Cần `make` và Python 3 cho công cụ đồng bộ tài liệu; các lệnh backend/frontend/container dùng môi trường tương ứng. Windows có thể chạy Make trong WSL hoặc dùng lệnh trực tiếp trong PowerShell theo [hướng dẫn môi trường](docs/development.md#container-engines).

```bash
make help
make up
make up ENGINE=podman
make compose-check ENGINE=podman
make docs-check
make docs-sync-preview FROM=main TO=feat/identity
make docs-sync FROM=main TO=feat/identity
```

Docker Desktop và Docker Engine dùng mặc định `ENGINE=docker`; Podman dùng `ENGINE=podman` và cần Compose provider. Dùng cùng engine cho `up`, `down`, `status`, `logs` và `db`.

`docs-sync` chép toàn bộ `docs/` đã commit từ nguồn sang nhánh đích local, gồm thêm/sửa/xóa, và đưa thay đổi vào staging. Bỏ `TO` để dùng nhánh hiện tại; cần working tree sạch khi chuyển sang nhánh khác. Review rồi commit/push theo nhu cầu. Hướng dẫn chi tiết tại [lệnh ngắn](docs/development.md#short-commands).

## Tài liệu

Điểm bắt đầu là [mục lục docs](docs/README.md). Đặc tả tính năng chứa phạm vi, requirements, UX, hợp đồng, ngoại lệ, tiêu chí chấp nhận và kiểm thử. [Community](docs/features/community/README.md) có trang tổng quan và đặc tả theo năm thành phần nghiệp vụ, cùng tài liệu tích hợp dùng chung.

- [Dự án: requirements, scope, nguồn lực và kế hoạch](docs/project.md)
- [Lộ trình MVP và v1](docs/roadmap.md)
- [Quyết định và vấn đề còn mở](docs/decisions.md)
- [Kiến trúc và quy ước tích hợp](docs/architecture.md)
- [Phát triển và kiểm thử kỹ thuật](docs/development.md)
- [Nghiệm thu, phát hành và vận hành](docs/release-operations.md)
- [Lịch sử và bản đồ chuyển đổi tài liệu](docs/archive/README.md)

## Kiểm tra

Backend (các test tích hợp cần PostgreSQL Development):

```bash
dotnet test SCDC.slnx --configuration Release
```

Frontend, chạy trong `clients/WebClient` sau `npm ci`:

```bash
npm test
npm run build
```

Link và anchor tài liệu, chạy từ root:

```bash
python3 scripts/check_docs.py
```
