.DEFAULT_GOAL := help

PYTHON ?= python3
ENGINE ?= docker
COMPOSE ?= $(ENGINE) compose
FROM ?= main
TO ?=

# Pass refs as environment values rather than interpolating them into shell code.
export SCDC_DOCS_FROM = $(FROM)
export SCDC_DOCS_TO = $(TO)

.PHONY: help up down status logs db compose-check api web build test test-api test-web docs-check docs-sync docs-sync-preview test-tools

help:
	@printf '%s\n' \
	  'SCDC — lệnh ngắn (chạy từ root repo)' \
	  '' \
	  '  make up                 Build và chạy stack Compose' \
	  '  make down               Dừng stack Compose, giữ volume' \
	  '  make status             Xem trạng thái container' \
	  '  make logs               Xem log API và PostgreSQL' \
	  '  make db                 Chạy riêng PostgreSQL' \
	  '  make compose-check      Kiểm tra cấu hình Compose, chưa chạy container' \
	  '  make api                Chạy backend local' \
	  '  make web                Cài dependency và chạy frontend local' \
	  '  make build              Build backend và frontend' \
	  '  make test               Chạy test backend và frontend' \
	  '  make test-api           Chạy test backend (cần PostgreSQL)' \
	  '  make test-web           Cài dependency và chạy test frontend' \
	  '  make docs-check         Kiểm tra link và anchor tài liệu' \
	  '  make test-tools         Kiểm thử công cụ đồng bộ trong repo Git tạm' \
	  '' \
	  '  ENGINE mặc định là docker (Docker Desktop hoặc Docker Engine).' \
	  '  make up ENGINE=podman   Dùng Podman và Compose provider đã cài.' \
	  '  Dùng cùng ENGINE cho down/status/logs/db/compose-check.' \
	  '  Có thể override COMPOSE, ví dụ COMPOSE="podman-compose".' \
	  '  Windows: chạy Make trong WSL; lệnh PowerShell xem docs/development.md.' \
	  '' \
	  '  make docs-sync FROM=main' \
	  '      Chép toàn bộ docs/ từ main vào nhánh hiện tại, đưa vào staging.' \
	  '  make docs-sync FROM=main TO=feat/identity' \
	  '      Chuyển sang nhánh đích local rồi chép docs/ (working tree phải sạch).' \
	  '  make docs-sync-preview FROM=main TO=feat/identity' \
	  '      Chỉ xem các file thêm/sửa/xóa; giữ nguyên nhánh và file.' \
	  '' \
	  '  FROM mặc định là main; có thể dùng origin/main sau khi git fetch.' \
	  '  Đồng bộ theo bản đã commit; file bị xóa ở nguồn cũng bị xóa ở đích.' \
	  '  Công cụ không tự commit hoặc push.'

up:
	$(COMPOSE) up -d --build

down:
	$(COMPOSE) down

status:
	$(COMPOSE) ps

logs:
	$(COMPOSE) logs --tail=100 chat-service postgres

db:
	$(COMPOSE) up -d postgres

compose-check:
	$(COMPOSE) config --quiet

api:
	dotnet run --project services/SCDC.Api/SCDC.Api.csproj --launch-profile http

web:
	npm --prefix clients/WebClient ci
	npm --prefix clients/WebClient run dev

build:
	dotnet build SCDC.slnx --configuration Release
	npm --prefix clients/WebClient ci
	npm --prefix clients/WebClient run build

test: test-api test-web

test-api:
	dotnet test SCDC.slnx --configuration Release

test-web:
	npm --prefix clients/WebClient ci
	npm --prefix clients/WebClient test

docs-check:
	$(PYTHON) scripts/check_docs.py

docs-sync:
	$(PYTHON) scripts/sync_docs.py

docs-sync-preview:
	$(PYTHON) scripts/sync_docs.py --dry-run

test-tools:
	$(PYTHON) -B -m unittest discover -s scripts/tests -p 'test_*.py' -v
