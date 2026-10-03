# Quy ước xử lý lỗi và phản hồi API

Tài liệu này quy định cấu trúc dữ liệu trả về, cơ chế quản lý lỗi nghiệp vụ và chuẩn hóa phản hồi HTTP cho toàn bộ hệ thống SCDC.

---

## 1. Mô hình kết quả nghiệp vụ: `Result<T>`

Hệ thống áp dụng mẫu thiết kế **Result Pattern** tại tầng Domain và Application để kiểm soát các luồng thực thi thất bại có chủ đích mà không lạm dụng ném ngoại lệ (`Exception`).

Thành phần định nghĩa tại project `SCDC.BuildingBlocks`:
- `Result`: Đại diện cho kết quả thành công hoặc thất bại không kèm dữ liệu trả về.
- `Result<T>`: Đại diện cho kết quả kèm dữ liệu `Value` kiểu `T` khi thành công, hoặc đối tượng `Error` khi thất bại.
- `Error`: Đối tượng chứa mã lỗi (`Code`), thông điệp mô tả (`Description`) và loại lỗi (`ErrorType`).

### Phân loại lỗi (`ErrorType`)

| Loại lỗi | Mã trạng thái HTTP tương ứng | Mục đích sử dụng |
|---|:---:|---|
| `Failure` | 400 Bad Request | Lỗi logic nghiệp vụ thông thường. |
| `Validation` | 400 Bad Request | Dữ liệu đầu vào vi phạm quy tắc validation. |
| `NotFound` | 404 Not Found | Bản ghi hoặc tài nguyên yêu cầu không tồn tại. |
| `Conflict` | 409 Conflict | Xung đột trạng thái (ví dụ: email/username đã tồn tại). |
| `Unauthorized` | 401 Unauthorized | Chưa xác thực danh tính người dùng hoặc token không hợp lệ. |
| `Forbidden` | 403 Forbidden | Đã xác thực nhưng không đủ quyền thực hiện hành động. |

---

## 2. Chuẩn hóa phản hồi HTTP theo RFC 7807

Tất cả các lỗi trả về cho client đều tuân thủ chuẩn **RFC 7807 ProblemDetails**:

```json
{
  "type": "https://errors.scdc.dev/identity/invalid-credentials",
  "title": "Invalid Credentials",
  "status": 401,
  "detail": "Email hoặc mật khẩu không chính xác.",
  "instance": "/api/v1/identity/auth/login",
  "extensions": {
    "errorCode": "Identity.InvalidCredentials",
    "traceId": "00-84a1421e3d36b85e098be2a8a5b281b3-f09b552d80d2871b-00"
  }
}
```

Trường hợp lỗi xác thực nhiều trường (`Validation ProblemDetails`):

```json
{
  "type": "https://errors.scdc.dev/validation-error",
  "title": "Validation Failed",
  "status": 400,
  "detail": "Một hoặc nhiều trường dữ liệu không hợp lệ.",
  "instance": "/api/v1/identity/auth/register",
  "errors": {
    "Email": [
      "Email không đúng định dạng.",
      "Email không được để trống."
    ],
    "Password": [
      "Mật khẩu phải chứa ít nhất 8 ký tự."
    ]
  }
}
```

---

## 3. Xử lý ngoại lệ hệ thống (Global Exception Handler)

Các ngoại lệ không mong muốn (unhandled exceptions, lỗi CSDL, ngắt kết nối) được xử lý tập trung thông qua Middleware `GlobalExceptionHandler`:
- Bắt mọi ngoại lệ chưa được xử lý trong pipeline HTTP.
- Ghi log chi tiết kèm `TraceId` và StackTrace phục vụ tra cứu nội bộ.
- Trả về phản hồi HTTP 500 với mã lỗi chung `InternalServerError`. Tuyệt đối **không để lộ stack trace ra phía client** trên môi trường Production.
