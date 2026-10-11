# SCDC — Nền tảng giao tiếp trên web

SCDC là ứng dụng giao tiếp cho nhóm bạn và cộng đồng. [MVP](docs/releases/mvp.md) gồm Identity, Community và Direct Messaging trong một API host Modular Monolith. [v1](docs/releases/v1.md) chuyển sang microservice và hoàn thiện thoại/video/chia sẻ màn hình.

## Mục lục

- [Tài liệu dự án](docs/README.md)
- [Mục tiêu và phạm vi](docs/project/overview.md)
- [Kế hoạch và phân công](docs/project/planning.md)
- [Quy trình thiết kế trước code](docs/project/workflow.md)
- [Kiến trúc](docs/system/architecture.md)
- [UI/UX](docs/experience/overview.md)
- [Development và kiểm thử](docs/guides/development.md)
- [Community](docs/features/community/README.md), [Messaging](docs/features/messaging/README.md)
- [Nghiệm thu](docs/releases/acceptance.md), [Vận hành](docs/guides/operations.md)

## Khởi chạy

Cần Docker hoặc Podman với Compose. Trước lần chạy đầu, tạo khóa bằng `openssl rand -base64 32`, lưu vào `COMMUNITY_OPERATION_KEY` trong `.env` local. Git bỏ qua `.env`; giữ khóa và volume keyring qua các lần chạy. DB cũ cần [migration explicit](docs/guides/community-development.md#migration).

```bash
docker compose up -d --build
# Podman:
podman compose up -d --build
```

| Thành phần | Địa chỉ Development |
|---|---|
| WebClient — React 19 / Vite | `http://localhost:3000` |
| API — .NET 10 / Swagger | `http://localhost:5026/swagger` |
| Health | `http://localhost:5026/api/v1/health` |
| PostgreSQL 18 | `localhost:5432`; database `scdc_chat`; user/password `scdc` / `scdc_dev` |

Compose phục vụ Development. Tài khoản seed không dùng đăng nhập; tạo tài khoản bằng API theo [hướng dẫn](docs/guides/development.md#identity). Health hiện không kiểm tra DB.

## Lệnh

[Makefile](Makefile) cung cấp các tác vụ chạy/build/test và đồng bộ tài liệu. `ENGINE=docker` mặc định; dùng `ENGINE=podman` khi chọn Podman và dùng cùng engine cho các thao tác container.

```bash
make help
make docs-check
make docs-sync-preview FROM=main TO=feat/identity
```

`docs-sync` chép snapshot `docs/` đã commit từ nguồn sang nhánh đích local, gồm thêm/sửa/xóa và staging. Công cụ không tự commit/push. Cách dùng và giới hạn tại [đồng bộ tài liệu](docs/guides/development.md#short-commands).

## Kiểm tra

Backend cần PostgreSQL và DB kiểm thử riêng theo [bộ chạy](docs/guides/community-development.md#backend-tests):

```bash
dotnet test SCDC.slnx --configuration Release
```

Frontend, trong `clients/WebClient` sau `npm ci`:

```bash
npm test
npm run build
```

Link/anchor tài liệu, từ root repo:

```bash
python3 scripts/check_docs.py
```
