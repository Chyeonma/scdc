# Cộng đồng — Ma trận phân quyền

> Tài liệu gốc: [SCDC-FR-ACL-001](../../archive/project-specs/03-requirements/05-access-control-matrix.md)

## Thuật toán tính quyền xem phòng

1. Kiểm tra phiên hợp lệ, tài khoản OK, cộng đồng/phòng tồn tại, còn là thành viên.
2. Nếu là **chủ sở hữu** → cho xem (DEC-056).
3. Lấy **quyền mặc định** phòng (phòng mới = mọi thành viên xem được).
4. Áp **vai trò**: có từ chối → từ chối; chỉ có cho phép → cho phép.
5. Áp **ngoại lệ cá nhân** sau cùng (cho phép/từ chối thay kết quả vai trò).
6. Có quyền xem + đã xác minh email → được gửi tin.

## Bảng tóm tắt

| Mặc định/vai trò | Ngoại lệ cá nhân | Kết quả |
|---|---|---|
| Mọi thành viên xem; không cấu hình | Kế thừa | ✅ Cho phép |
| Vai trò cho phép + vai trò từ chối | Kế thừa | ❌ Từ chối |
| Vai trò từ chối | Cho phép | ✅ Cho phép |
| Vai trò cho phép | Từ chối | ❌ Từ chối |
| Phòng giới hạn; không cấu hình | Kế thừa | ❌ Từ chối |

## Ma trận quyền đầy đủ

Do bảng quyền rất chi tiết (ACL-01 đến ACL-19), vui lòng xem đầy đủ tại:

👉 **[Ma trận quyền gốc](../../archive/project-specs/03-requirements/05-access-control-matrix.md)**

---

📎 Chi tiết: [Ma trận gốc](../../archive/project-specs/03-requirements/05-access-control-matrix.md)
