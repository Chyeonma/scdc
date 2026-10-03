# Quy ước lập trình và quy trình Git

Tài liệu này xác định các quy tắc viết mã nguồn, quy chuẩn đặt tên và quy trình quản lý phiên bản mã nguồn trong dự án SCDC.

---

## 1. Quy ước lập trình C# (.NET 10)

### 1.1. Kiến trúc phân tầng và ranh giới Module
- Mã nguồn nghiệp vụ nằm trong các Class Library tương ứng tại thư mục `services/Modules/`.
- Không tham chiếu chéo giữa các project module. Mọi tương tác liên module bắt buộc thông qua các Interface được định nghĩa tại `SCDC.Contracts`.
- Mỗi module tự quản lý `DbContext` và schema CSDL tương ứng. Tuyệt đối không thực hiện câu truy vấn `JOIN` xuyên schema trong mã ứng dụng.

### 1.2. Mẫu thiết kế và xử lý logic
- Sử dụng mẫu thiết kế **Result Pattern** (`Result<T>`) thay cho việc ném ngoại lệ (`throw Exception`) đối với các trường hợp lỗi nghiệp vụ dự đoán trước được.
- Mọi phương thức bất đồng bộ phải có hậu tố `Async` và nhận `CancellationToken`.
- Tận dụng tính năng C# hiện đại: `record`, `file-scoped namespace`, `primary constructor`, `pattern matching`.

### 1.3. Quy ước đặt tên
- **Class, Interface, Struct, Record, Enum:** `PascalCase` (ví dụ: `IUserDirectory`, `AuthSession`).
- **Method, Property:** `PascalCase` (ví dụ: `GetByIdAsync`, `DisplayName`).
- **Private Field:** `_camelCase` (ví dụ: `_dbContext`, `_tokenService`).
- **Parameter, Local Variable:** `camelCase` (ví dụ: `userId`, `cancellationToken`).

---

## 2. Quy trình làm việc với Git (Git Workflow)

### 2.1. Phân nhánh (Branching Strategy)
- Nhánh chính: `main` (mã nguồn ổn định, đã qua kiểm thử).
- Nhánh tính năng: `feature/<tên-module>-<tên-chức-năng>` (ví dụ: `feature/messaging-cursor-pagination`).
- Nhánh sửa lỗi: `fix/<tên-vấn-đề>` (ví dụ: `fix/token-rotation-race-condition`).

### 2.2. Quy ước thông điệp Commit (Conventional Commits)
Cấu trúc chuẩn của commit message:
```text
<loại>(<phạm vi>): <mô tả ngắn gọn bằng thể mệnh lệnh>

[nội dung chi tiết nếu có]
```

**Các loại commit hợp lệ:**
- `feat`: Thêm tính năng mới.
- `fix`: Sửa lỗi phát sinh.
- `refactor`: Tái cấu trúc mã nguồn (không thay đổi hành vi nghiệp vụ).
- `perf`: Tối ưu hóa hiệu năng, giảm độ trễ, tối ưu bộ nhớ.
- `test`: Thêm hoặc chỉnh sửa ca kiểm thử.
- `docs`: Thêm hoặc cập nhật tài liệu.
- `chore`: Thay đổi cấu hình build, dependencies, CI/CD.
