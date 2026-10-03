# Hướng dẫn thiết lập môi trường phát triển

Tài liệu này hướng dẫn các bước cài đặt và khởi chạy hệ thống SCDC trên môi trường phát triển cục bộ (Local Development).

---

## 1. Yêu cầu hệ thống

- **Hệ điều hành:** Linux (khuyến nghị Fedora, Ubuntu), macOS hoặc Windows (WSL2).
- **.NET SDK:** Phiên bản 10.0 trở lên.
- **Node.js:** Phiên bản 20.x trở lên cùng npm/pnpm.
- **Container Engine:** Docker hoặc Podman kèm plugin Compose.
- **Công cụ cơ sở dữ liệu (tùy chọn):** DBeaver, DataGrip hoặc pgAdmin.

---

## 2. Khởi chạy nhanh bằng Docker / Podman Compose

Phương pháp nhanh nhất để khởi chạy toàn bộ dịch vụ (PostgreSQL, Backend API, React WebClient):

```bash
# Khởi chạy toàn bộ hệ thống ở chế độ nền
docker compose up -d --build

# Kiểm tra trạng thái các container
docker compose ps

# Xem log các dịch vụ
docker compose logs -f
```

**Các cổng dịch vụ mặc định:**
- Web Client (React): `http://localhost:3000`
- Backend API & Swagger: `http://localhost:5026/swagger`
- API Health Check: `http://localhost:5026/api/v1/health`
- PostgreSQL: `localhost:5432` (Database: `scdc_chat`, User: `scdc`, Password: `scdc_dev`)

---

## 3. Khởi chạy độc lập phục vụ Debug

Trong trường hợp cần gỡ lỗi (debug) mã nguồn C# hoặc React:

### Bước 1: Khởi động cơ sở dữ liệu PostgreSQL
```bash
docker compose up -d postgres
```

### Bước 2: Khởi chạy Backend API (.NET 10)
```bash
# Khôi phục dependencies và biên dịch solution
dotnet restore SCDC.slnx
dotnet build SCDC.slnx --no-restore

# Khởi chạy Host API
dotnet run --project services/SCDC.Api/SCDC.Api.csproj --launch-profile http
```
API sẽ lắng nghe tại `http://localhost:5026`.

### Bước 3: Khởi chạy WebClient (React / Vite)
```bash
cd clients/WebClient
npm install
npm run dev
```
Giao diện sẽ chạy tại `http://localhost:5000` hoặc cổng được cấu hình trong Vite.

---

## 4. Kiểm thử tự động (Unit & Integration Tests)

Chạy toàn bộ bộ test của solution:

```bash
dotnet test SCDC.slnx --configuration Release
```

*Lưu ý: Bộ kiểm thử tích hợp Identity (`IdentityV1FlowTests`) yêu cầu PostgreSQL đang chạy trên cổng 5432 để kiểm tra tương tác CSDL thực tế.*
