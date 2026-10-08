# SCDC — Thiết kế Invitations

Thiết kế kỹ thuật của thành phần; quy tắc, use case và AC/TC ở [đặc tả](../specs/invitations.md). Phần được triển khai/kiểm chứng hiện tại được quản lý tại [tiến độ](../status.md); các route và cơ chế ngoài gói đã kiểm chứng tiếp tục là thiết kế mục tiêu.

<a id="contracts"></a>

## Thiết kế dữ liệu/API

HTTP mục tiêu và quy ước chung ở [tích hợp](integration.md#contracts); [OpenAPI Community](../../../contracts/community.openapi.json) chứa contract đầy đủ và metadata trạng thái từng operation. Các route Invitations ở trang này còn là mục tiêu; phần đã chạy của những thành phần khác được quản lý tại [status](../status.md). Mỗi use case ứng dụng phối hợp dữ liệu của các phần trong [transaction chung](integration.md#transactions).

| Method / đường dẫn | Đầu vào | Kết quả và kiểm tra quyền |
|---|---|---|
| `POST /servers/{id}/invites` | `{expiresInSeconds,maxUses?}` | 201 metadata/link; expiresInSeconds là 3600/86400/604800/null, bỏ trường dùng mặc định 604800; quyền tạo mời |
| `DELETE /servers/{id}/invites/{inviteId}` | ID link và expectedVersion | 204 thu hồi; đúng quyền, join sau thu hồi bị từ chối |
| `POST /invites/preview` | `{token}` | 200 summary khi link hợp lệ; không tạo membership/tiêu lượt; không trả phòng/tin |
| `POST /invites/join` | `{token}` | 200 membership; token trong body, kiểm tra hạn/thu hồi/lượt dưới khóa; không ghi body/token vào log |
| `POST /servers/{id}/member-invitations` | `{recipientUserId}` | 201 mời pending; owner/quyền tạo mời; chưa tạo membership |
| `POST /member-invitations/{invitationId}/accept` | Không có body | 200 membership; đúng người nhận, mời còn hợp lệ; không thêm bước duyệt |
| `POST /member-invitations/{invitationId}/reject` | `{expectedVersion}` | 200 rejected; đúng người nhận; chỉ pending còn hạn |
| `DELETE /servers/{id}/member-invitations/{invitationId}` | `expectedVersion` | 200 cancelled; quyền tạo mời; chỉ pending |

### Giao dịch của thành phần

- Mời đích danh: chỉ vào private theo DEC-074; unique pending/server/recipient, hạn 7 ngày, accept đúng recipient active/verified. Đang active thì create mời nhận 409 ALREADY_MEMBER; pending còn hạn thì POST lặp trả cùng mời, không kéo dài hạn. Accept và membership cùng transaction; accepted lặp chỉ trả membership nếu còn đúng membershipId đã tạo, không rejoin sau người nhận đã rời. Join bằng đường khác hủy mời pending với reason joined_elsewhere; sau trạng thái cuối hoặc expiry, lời mời cũ không được mở lại.

<a id="invite-secret"></a>

### Link mời, hiệu lực và secret

Link token kỹ thuật đề xuất là 32 byte ngẫu nhiên base64url, DB tra bằng SHA-256. Route SPA `/invite#token=…` lấy token vào RAM rồi bỏ fragment khỏi URL; API preview/join nhận token trong body. Điều chỉnh route draft cũ `/invites/{token}/join` để secret không nằm trong request path; không ghi body/token/URL mời trong log, audit hoặc outbox.

Theo DEC-097, hiệu lực link/mời đích danh không phụ thuộc creator còn là member/còn manage_invites. Revoke/cancel vẫn cần actor hiện có quyền; creator đã mất quyền không tự được hủy chỉ vì từng tạo. Không tự refresh hạn 7 ngày khi gọi lại POST mời đích danh. Pending quá hạn được chuyển expired dưới khóa trước khi tạo mời mới; partial index chỉ dùng trạng thái pending, không đặt `now()` trong predicate.

Preview chỉ trả server summary khi token còn hợp lệ, không có phòng/tin, không tiêu lượt và không giữ chỗ. Join khóa server/invite và kiểm tra `expiresAt > now`, revokedAt null, số lượt trước tạo membership; chỉ tăng lượt cho membership mới/reactivate. Rollback không tiêu lượt. Thành viên đang active gọi lại không tiêu lượt; link sai/không còn hợp lệ không cấp quyền mới. Người tranh lượt cuối chỉ một người join thành công; không hứa preview thành công là join chắc chắn còn lượt.

Để người có quyền sao chép lại link khi response tạo bị mất, đề xuất giữ token trong envelope Data Protection `Community.InviteSecret.v1`, ngoài hash tra cứu. Metadata/list không có token; `GET /servers/{id}/invites/{inviteId}/link` trả URL chỉ cho manage_invites khi link còn dùng được, `Cache-Control:no-store`. Purge envelope khi thu hồi/hết hạn/hết lượt; link không hết hạn cần giữ key ring tương ứng và backup liên quan. Key thiếu trả 503, không đổi token/link âm thầm.

Các cột/ràng buộc/mapping cần thay theo [COM-SQL-01–10](integration.md#schema-migration). Thiết kế chưa được coi triển khai trước khi có migration và proof của writer/guard.
