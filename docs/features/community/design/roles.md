# SCDC — Thiết kế gói vai trò và quyền quản lý

[Phạm vi](../delivery/roles/plan.md) chọn UC-COM-20/21; quy tắc chuẩn ở [Permissions](../specs/permissions.md), [catalog/API mục tiêu](permissions.md), [operation fingerprint](integration.md#operations).

## Dữ liệu và migration

003 thêm roles.name_key theo cùng .NET trim/NFC/ToLowerInvariant của search, unique C theo server; name 64 UTF-16, role.version int32 với một trigger tăng version/timestamp. Giữ normalized_name legacy nhưng bỏ unique theo locale cũ. @everyone được create writer ghi key explicit. Assignment/member override thêm membership_id, FK `(server_id,user_id,membership_id)`; join/rejoin dọn grants trước đổi epoch. Không suy epoch từ user khi xử lý request cũ.

Runner yêu cầu ledger/checksum 001/002, cùng advisory lock và table locks. Key backfill theo batch 500, validate tên/collision/20-role cap/catalog và chỉ channel_view override. Dữ liệu không hợp lệ báo cần sửa có review, rollback; không đổi tên/quyền/epoch âm thầm. Bootstrap DB mới có 001/002/003, seed dùng đúng catalog/key/epoch. Drain writer cũ → migrate 003 → deploy writer mới; không chạy bootstrap có DROP SCHEMA trên DB cần giữ dữ liệu.

Operation create_role scope là serverId; fingerprint name sau trim + permission mask byte theo thứ tự bit đã công bố. Permission array thứ tự khác cho cùng fingerprint, duplicate/unknown bị chặn. HMAC keys/rotation dùng cơ chế create-server. Retry kiểm tra owner hiện hành và resource còn tồn tại; role đã xóa không được tạo lại bằng key cũ.

## API và giao dịch

Các route/DTO theo OpenAPI: GET/POST roles, PATCH/DELETE role, GET members, GET/PUT member roles. Role/member pages mặc định 20, tối đa 50, keyset UUID tăng; cursor purpose riêng cho từng collection, actor/server/limit/position/24 giờ. Target roleIds chỉ custom cùng server, tối đa 20, không duplicate; PUT thay tập đầy đủ, @everyone không nằm trong roleIds. Version wire là chuỗi số dương; storage hiện hành int32, overflow dừng trước ghi.

Identity guard khóa account actor → server FOR UPDATE cho mutation/FOR SHARE cho read → membership/role. Server lock ổn định ownership, authorization và limit/CAS tới commit; lease kiểm tra lại trước commit. Private/nonmember/inactive trả 404; member thiếu quyền trả 403. Role/member catalog đọc bởi owner hoặc manage_channel_access; writer owner-only, không coi manage_channel_access là quyền quản lý role. Roster lấy UserSummary qua Identity contract, user null khi directory không còn account active; không trả email, không cần session của target để gán role cho membership active.

Create/sửa/xóa/assignment đổi dữ liệu bump server accessVersion và outbox Community.AccessChanged.v1 cùng transaction. Event chỉ scope/server/accessVersion/cause/resource IDs, không có tên/member roster hoặc nội dung; chưa dispatcher/Hub. Assignment tăng membership.version; delete role cascade assignment/role override và tăng version membership bị đổi, để request cũ không ghi đè. Role update tăng role.version đúng một lần. No-op CAS hợp lệ trả hiện trạng, không bump/outbox. DELETE dùng expectedVersion, role đã mất trả 404; legacy invite còn giữ custom role nhận 409 ROLE_IN_USE, không âm thầm đổi lời mời.

Management guard dùng catalog/evaluator hiện hành; ServerDetail/own list đọc assignment đúng epoch. Evaluator domain thuần giữ foundation → owner → default/role deny-wins → personal; management union độc lập view. Có fixture proof, chưa có checker/guard phòng hoặc endpoint ACL nên không nghiệm thu view/realtime.

## WebClient

Detail của owner có lối vào quản lý vai trò và thành viên. Role create giữ operation/body theo actor/server trong sessionStorage trước POST; lỗi không rõ giữ cùng key để retry chủ động, có thể phục hồi sau reload. Sửa/xóa/gán dùng version/epoch đã tải, retry:false; mất response hoặc conflict yêu cầu GET hiện trạng và người dùng lưu lại. Không tự replay mutation; actor/server đổi bỏ response cũ và reset state.

Tập role tải đầy đủ (tối đa 21 kể cả system), roster có phân trang. @everyone chỉ hiển thị trạng thái bảo vệ; chọn target tải epoch/version rồi gán role. Khi lỗi mất quyền, bỏ dữ liệu quản lý cũ; không dùng UI làm nguồn authorization. Mobile/keyboard/loading/empty/error được kiểm tra bằng Chromium với API thật.
