.DEFAULT_GOAL := help

ENGINE ?= podman
COMPOSE ?= $(ENGINE) compose
COMPOSE_ARGS ?=
ENV_FILE ?= .env
PYTHON ?= python3
FROM ?= main
TO ?=
DEV ?= 1
SAMPLE ?= 0
ROLLBACK ?= 0
export COMPOSE COMPOSE_ARGS PYTHON SERVICE
export SCDC_DEV = $(DEV)
export SCDC_SAMPLE = $(SAMPLE)
export SCDC_ROLLBACK = $(ROLLBACK)
SCDC_REDIS_SECRET_DIR ?= $(HOME)/.local/share/scdc/redis-secrets
export SCDC_REDIS_SECRET_DIR
SCDC_RPC_SECRET_DIR ?= $(HOME)/.local/share/scdc/rpc-secrets
export SCDC_RPC_SECRET_DIR
SCDC_IDENTITY_DB_SECRET_DIR ?= $(HOME)/.local/share/scdc/identity-database-secrets
export SCDC_IDENTITY_DB_SECRET_DIR
SCDC_COMMUNITY_DB_SECRET_DIR ?= $(HOME)/.local/share/scdc/community-database-secrets
export SCDC_COMMUNITY_DB_SECRET_DIR

export SCDC_ENV_FILE = $(ENV_FILE)
export SCDC_DOCS_FROM = $(FROM)
export SCDC_DOCS_TO = $(TO)

STACK = $(PYTHON) ops/compose.py

.PHONY: help init setup up migrate preflight build config check status logs down docs-check docs-sync docs-sync-preview

help:
	@printf '%s\n' \
	  'SCDC — môi trường phát triển (chạy từ gốc repo)' \
	  '' \
	  '  make setup       Chuẩn bị .env, build, chạy và kiểm tra stack' \
	  '  make up          Kiểm tra database rồi chạy runtime bằng image hiện có' \
	  '  make build       Build lại image sau khi sửa code; sau đó chạy make up' \
	  '  make migrate     Dừng ứng dụng và nâng cấp database; không tự bật writer' \
	  '  make preflight   Kiểm tra cutover và phiên bản schema trước khi chạy' \
	  '  make config      Kiểm tra cấu hình runtime và công cụ' \
	  '  make check       Kiểm tra gateway, API, database và OpenObserve' \
	  '  make status      Xem các container runtime' \
	  '  make logs        Xem 100 dòng log cuối; SERVICE=chat-service để chọn service' \
	  '  make down        Dừng stack, giữ các volume' \
	  '' \
	  '  Docker là mặc định. Dùng ENGINE=podman cho mọi lệnh nếu chọn Podman.' \
	  '  make init chỉ tạo .env khi chưa có; không ghi đè file hiện hữu.' \
	  '  DEV=0 dùng image đã build; SAMPLE=1 bật service mẫu; ROLLBACK=1 dùng image rollback đã xác minh.' \
	  '  ENV_FILE và COMPOSE_ARGS dùng khi cần stack riêng, ví dụ COMPOSE_ARGS="-p scdc-demo".' \
	  '' \
	  '  Tài liệu: make docs-check (cần Python 3).' \
	  '  Nhánh docs cũ: make docs-sync-preview FROM=main TO=<nhánh>, rồi make docs-sync.' \
	  '  Hướng dẫn: docs/guides/development.md'

init:
	$(PYTHON) scripts/init_identity_database_secrets.py "$$SCDC_IDENTITY_DB_SECRET_DIR" --label-for-containers
	$(PYTHON) scripts/init_rpc_certificates.py "$$SCDC_RPC_SECRET_DIR" --add-community --label-for-containers
	$(PYTHON) scripts/init_redis_secrets.py "$$SCDC_REDIS_SECRET_DIR" --label-for-containers
	@set -eu; \
	  if [ -e "$$SCDC_ENV_FILE" ]; then \
	    printf 'Giữ cấu hình hiện có: %s\n' "$$SCDC_ENV_FILE"; \
	  else \
	    scdc_operation_key=$$(openssl rand -base64 32); \
	    (umask 077; set -C; printf 'COMMUNITY_OPERATION_KEY=%s\n' "$$scdc_operation_key" > "$$SCDC_ENV_FILE"); \
	    printf 'Đã tạo cấu hình: %s\n' "$$SCDC_ENV_FILE"; \
	  fi
	$(PYTHON) scripts/init_community_database_secrets.py "$$SCDC_COMMUNITY_DB_SECRET_DIR" --env-file "$$SCDC_ENV_FILE" --label-for-containers

setup: init
	sh ops/setup.sh
	$(MAKE) --no-print-directory check

up: init
	$(STACK) up

migrate: init
	sh ops/migrate.sh

preflight: init
	sh ops/preflight.sh

build: init
	$(STACK) build

config: init
	$(STACK) config

check:
	$(STACK) check

status:
	$(STACK) status

logs:
	$(STACK) logs

down:
	$(STACK) down

docs-check:
	$(PYTHON) scripts/check_docs.py

docs-sync:
	$(PYTHON) scripts/sync_docs.py

docs-sync-preview:
	$(PYTHON) scripts/sync_docs.py --dry-run
