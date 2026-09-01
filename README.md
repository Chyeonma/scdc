# SCDC

SCDC dang duoc xay lai theo modular monolith. Backend hien tai la foundation
sach, chua trien khai lai cac tinh nang nghiep vu cu.

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

1. Identity: register, verify email, login, session, refresh, logout.
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

## Podman Compose

```bash
podman compose up -d --build
```

- Web client: `http://localhost:3000`
- API: `http://localhost:5026`
- PostgreSQL: `localhost:5432`

Script trong `database/postgres` chi tu dong chay khi PostgreSQL khoi tao mot
data volume moi. Web client cu van duoc giu lai nhung cac luong dang nhap/chat
se chua hoat dong cho den khi cac vertical slice moi duoc trien khai.

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
