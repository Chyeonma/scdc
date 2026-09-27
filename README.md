# SCDC

SCDC la modular monolith voi Identity, Community va Messaging API. Luong chat
da co DM, group, channel, realtime va attachment; ban phat hanh cong khai van
can hoan tat cac buoc trong `docs/messaging/release-handoff.md`.

Tai lieu tong quan: [`docs/architecture/overview.md`](docs/architecture/overview.md).

Tai lieu Identity v1: [`docs/identity/identity-v1.md`](docs/identity/identity-v1.md).

## Cau truc backend

```text
services/
├── SCDC.Api                 API host, middleware, Swagger va health
├── SCDC.BuildingBlocks      Kieu va abstraction dung chung
├── SCDC.Contracts           Contract giao tiep giua cac module
└── Modules/
    ├── Identity             Tai khoan va xac thuc
    ├── Community            Server, member, channel va permission
    └── Messaging            Chat space, message va realtime
```

Chi `SCDC.Api` la executable. Ba module la class library duoc host nap trong
cung mot process.

Huong dependency:

```text
SCDC.Api -> Identity, Community, Messaging
Identity, Community, Messaging -> BuildingBlocks, Contracts
```

Ba module khong tham chieu truc tiep nhau. Giao tiep cheo module di qua
`SCDC.Contracts`.

## Thu tu trien khai

Moi tinh nang duoc lam theo vertical slice:

```text
contract -> domain rule -> application handler -> persistence -> endpoint -> test
```

Thu tu module:

1. Identity: da co register, verify email, login, session, refresh, logout,
   profile va password lifecycle.
2. Community: server, membership, channel, role va permission.
3. Messaging: chat space, send/history message, outbox va SignalR.
4. Moderation se duoc them khi ba module cot loi da on dinh.

## Database

PostgreSQL schema va du lieu minh hoa nam tai:

- `database/postgres/schema.sql`
- `database/postgres/seed.sql`
- `database/postgres/README.md`

SQL hien la source of truth. Backend khong tu chay EF migration.

## Build va chay local

```bash
dotnet restore SCDC.slnx
dotnet build SCDC.slnx --no-restore
dotnet run --project services/SCDC.Api/SCDC.Api.csproj --launch-profile http
```

- API: `http://localhost:5026`
- Health: `http://localhost:5026/api/v1/health`
- Swagger: `http://localhost:5026/swagger`

Health response liet ke cac module da duoc host nap va database schema ma module
se huu.

## API response

Response thanh cong tra ve DTO dung kieu voi status `200`, `201` hoac `204`.
Loi API dung `ProblemDetails` va luon co `errorCode`, `traceId`. Validation error
co them dictionary `errors` theo ten field.

Application handler tra ve `Result` hoac `Result<T>` va khong phu thuoc HTTP.
API host anh xa error type thanh `400`, `401`, `403`, `404`, `409`, `429` hoac
`503`. Status `500` chi duoc tao boi global exception handler va khong tra stack
trace hay chi tiet exception cho client.

Tai lieu chi tiet: [`docs/api/error-handling.md`](docs/api/error-handling.md).

## Compose

```bash
docker compose up -d --build
```

- Web client: `http://localhost:3000`
- API: `http://localhost:5026`
- PostgreSQL: `localhost:5432`

`compose.yaml` chi danh cho phat trien local va co du lieu seed. Cau hinh
`compose.release.yaml` khong nap seed va yeu cau secret rieng. Xem
[`docs/messaging/release-handoff.md`](docs/messaging/release-handoff.md) truoc
khi nang cap, backup hoac phuc hoi. Health `/api/v1/health` kiem tra host;
`/api/v1/health/ready` kiem tra ket noi PostgreSQL.

## DBeaver

```text
Host: localhost
Port: 5432
Database: scdc_chat
Username: scdc
Password: scdc_dev
SSL mode: disable
```

Schema nghiep vu: `identity`, `community`, `messaging`, `moderation`, `audit`
va `integration`.
