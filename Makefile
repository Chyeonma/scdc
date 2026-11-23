.DEFAULT_GOAL := help

ENGINE ?= docker
COMPOSE ?= $(ENGINE) compose
COMPOSE_ARGS ?=
ENV_FILE ?= .env
PYTHON ?= python3
FROM ?= main
TO ?=
SCDC_REDIS_SECRET_DIR ?= $(HOME)/.local/share/scdc/redis-secrets
export SCDC_REDIS_SECRET_DIR

export SCDC_ENV_FILE = $(ENV_FILE)
export SCDC_DOCS_FROM = $(FROM)
export SCDC_DOCS_TO = $(TO)

COMPOSE_COMMAND = $(COMPOSE) --env-file "$$SCDC_ENV_FILE" $(COMPOSE_ARGS)

.PHONY: help init setup up build check status logs down docs-check docs-sync docs-sync-preview

help:
	@printf '%s\n' \
	  'SCDC — môi trường phát triển (chạy từ gốc repo)' \
	  '' \
	  '  make setup       Chuẩn bị .env, build, chạy và kiểm tra stack' \
	  '  make up          Chạy lại stack, giữ dữ liệu và khóa' \
	  '  make build       Build lại image sau khi sửa code; sau đó chạy make up' \
	  '  make check       Kiểm tra gateway, API, service mẫu, database và OpenObserve' \
	  '  make status      Xem container, gồm các job khởi tạo khóa' \
	  '  make logs        Xem 100 dòng log cuối; SERVICE=chat-service để chọn service' \
	  '  make down        Dừng stack, giữ các volume' \
	  '' \
	  '  Docker là mặc định. Dùng ENGINE=podman cho mọi lệnh nếu chọn Podman.' \
	  '  make init chỉ tạo .env khi chưa có; không ghi đè file hiện hữu.' \
	  '  ENV_FILE và COMPOSE_ARGS dùng khi cần stack riêng, ví dụ COMPOSE_ARGS="-p scdc-demo".' \
	  '' \
	  '  Tài liệu: make docs-check (cần Python 3).' \
	  '  Nhánh docs cũ: make docs-sync-preview FROM=main TO=<nhánh>, rồi make docs-sync.' \
	  '  Hướng dẫn: docs/guides/development.md'

init:
	$(PYTHON) scripts/init_redis_secrets.py "$$SCDC_REDIS_SECRET_DIR" --label-for-containers
	@set -eu; \
	  if [ -e "$$SCDC_ENV_FILE" ]; then \
	    printf 'Giữ cấu hình hiện có: %s\n' "$$SCDC_ENV_FILE"; \
	  else \
	    scdc_operation_key=$$(openssl rand -base64 32); \
	    (umask 077; set -C; printf 'COMMUNITY_OPERATION_KEY=%s\n' "$$scdc_operation_key" > "$$SCDC_ENV_FILE"); \
	    printf 'Đã tạo cấu hình: %s\n' "$$SCDC_ENV_FILE"; \
	  fi

setup: init
	$(COMPOSE_COMMAND) config --quiet
	$(COMPOSE_COMMAND) up -d --build
	$(MAKE) --no-print-directory check

up: init
	$(COMPOSE_COMMAND) up -d

build: init
	$(COMPOSE_COMMAND) build

check:
	@set -eu; \
	  for scdc_url in \
	    http://localhost:3000/ \
	    http://localhost:3000/healthz \
	    http://localhost:3000/health \
	    http://localhost:3000/api/v1/health \
	    http://localhost:3000/api/v1/health/access \
	    http://localhost:5080/healthz; do \
	    printf 'Kiểm tra %s\n' "$$scdc_url"; \
	    curl --fail --silent --show-error --output /dev/null \
	      --connect-timeout 2 --max-time 5 --retry 15 --retry-delay 2 --retry-connrefused \
	      "$$scdc_url"; \
	  done
	$(COMPOSE_COMMAND) exec -T postgres pg_isready -U scdc -d scdc_chat
	$(COMPOSE_COMMAND) exec -T web-client nginx -t

status:
	$(COMPOSE_COMMAND) ps -a

logs:
	$(COMPOSE_COMMAND) logs --tail=100 $(SERVICE)

down:
	$(COMPOSE_COMMAND) down

docs-check:
	$(PYTHON) scripts/check_docs.py

docs-sync:
	$(PYTHON) scripts/sync_docs.py

docs-sync-preview:
	$(PYTHON) scripts/sync_docs.py --dry-run
