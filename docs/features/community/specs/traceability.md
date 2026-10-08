# SCDC — Truy vết Community

Ma trận COM/UC/API/AC/TC dẫn tới nguồn định nghĩa ở từng thành phần. Danh mục hành trình tại [đặc tả](README.md#use-cases); kết quả thực thi dẫn từ [status](../status.md).

<a id="use-case-rules"></a>

## Truy vết quy tắc cộng đồng

Giới hạn và thứ tự ưu tiên giữ ở bảng COM/ACL; bảng này chỉ xác định UC áp dụng. [COM-009](integration.md#com-009) và phần scope của [COM-039](servers.md#com-039) được ghi thành giới hạn, không tạo UC chức năng ngoài v1.

| Quy tắc | Use case áp dụng | Điểm cần đối chiếu |
|---|---|---|
| [COM-001](memberships.md#com-001) | [UC-COM-02](servers.md#uc-com-02), [UC-COM-06](memberships.md#uc-com-06), [UC-COM-07](memberships.md#uc-com-07), [UC-COM-11](invitations.md#uc-com-11) | Tìm kiếm hoặc lời mời dẫn tới đúng nhánh tham gia |
| [COM-002](servers.md#com-002) | [UC-COM-02](servers.md#uc-com-02), [UC-COM-04](servers.md#uc-com-04) | Chỉ public xuất hiện trong tìm kiếm |
| [COM-003](servers.md#com-003) | [UC-COM-01](servers.md#uc-com-01), [UC-COM-05](servers.md#uc-com-05), [UC-COM-06](memberships.md#uc-com-06), [UC-COM-07](memberships.md#uc-com-07) | Mặc định vào ngay; approval tạo pending |
| [COM-004](invitations.md#com-004) | [UC-COM-11](invitations.md#uc-com-11) | Link hợp lệ bỏ qua chờ duyệt |
| [COM-005](invitations.md#com-005) | [UC-COM-09](invitations.md#uc-com-09) | Người tạo chọn hạn link |
| [COM-006](servers.md#com-006) | [UC-COM-05](servers.md#uc-com-05) | Đúng quyền đổi join mode |
| [COM-007](channels.md#com-007) | [UC-COM-16](channels.md#uc-com-16) | Đúng quyền tạo phòng |
| [COM-008](channels.md#com-008) | [UC-COM-17](channels.md#uc-com-17), [UC-COM-25](integration.md#uc-com-25) | Danh sách, nội dung và cập nhật đều theo view |
| [COM-009](integration.md#com-009) | [UC-COM-23](integration.md#uc-com-23) | Composer chỉ gửi văn bản; file trong phòng ở đợt sau |
| [COM-010](invitations.md#com-010) | [UC-COM-09](invitations.md#uc-com-09) | Đúng quyền tạo lời mời |
| [COM-011](memberships.md#com-011) | [UC-COM-08](memberships.md#uc-com-08) | Đúng quyền duyệt |
| [COM-012](channels.md#com-012) | [UC-COM-16](channels.md#uc-com-16), [UC-COM-22](permissions.md#uc-com-22) | Phòng mới mặc định cho mọi thành viên xem |
| [COM-013](integration.md#com-013) | [UC-COM-23](integration.md#uc-com-23), [UC-COM-24](integration.md#uc-com-24) | Gửi, sửa, xóa và trạng thái tương tự DM |
| [COM-014](integration.md#com-014) | [UC-COM-17](channels.md#uc-com-17), [UC-COM-25](integration.md#uc-com-25) | Tin lưu bền đọc lại khi còn quyền |
| [COM-015](channels.md#com-015) | [UC-COM-17](channels.md#uc-com-17) | Thành viên mới được xem lịch sử cũ theo view |
| [COM-016](permissions.md#com-016) | [UC-COM-22](permissions.md#uc-com-22) | Đúng quyền thay ACL |
| [COM-017](integration.md#com-017) | [UC-COM-17](channels.md#uc-com-17), [UC-COM-23](integration.md#uc-com-23) | Có view thì gửi text khi đủ điều kiện tài khoản |
| [COM-018](integration.md#com-018) | [UC-COM-23](integration.md#uc-com-23) | Thử lại cùng thao tác không tạo tin trùng |
| [COM-019](invitations.md#com-019) | [UC-COM-11](invitations.md#uc-com-11), [UC-COM-12](invitations.md#uc-com-12), [UC-COM-13](invitations.md#uc-com-13) | Private tham gia bằng lời mời hợp lệ |
| [COM-020](invitations.md#com-020) | [UC-COM-10](invitations.md#uc-com-10), [UC-COM-11](invitations.md#uc-com-11) | Link thu hồi không cấp membership |
| [COM-021](memberships.md#com-021) | [UC-COM-15](memberships.md#uc-com-15) | Thành viên thường tự rời |
| [COM-022](integration.md#com-022) | [UC-COM-23](integration.md#uc-com-23) | Chưa xác minh không được dùng ứng dụng/gửi tin |
| [COM-023](integration.md#com-023) | [UC-COM-24](integration.md#uc-com-24) | Sửa chỉ giữ nội dung hiện hành |
| [COM-024](integration.md#com-024) | [UC-COM-23](integration.md#uc-com-23), [UC-COM-24](integration.md#uc-com-24) | Validation nội dung dùng chung DM |
| [COM-025](permissions.md#com-025) | [UC-COM-20](permissions.md#uc-com-20), [UC-COM-21](permissions.md#uc-com-21), [UC-COM-22](permissions.md#uc-com-22) | Vai trò quản lý và ngoại lệ view cá nhân |
| [COM-026](permissions.md#com-026) | [UC-COM-17](channels.md#uc-com-17), [UC-COM-22](permissions.md#uc-com-22) | Owner luôn view sau điều kiện nền |
| [COM-027](permissions.md#com-027) | [UC-COM-17](channels.md#uc-com-17), [UC-COM-22](permissions.md#uc-com-22), [UC-COM-25](integration.md#uc-com-25) | Role deny thắng, cá nhân sau cùng |
| [COM-028](permissions.md#com-028) | [UC-COM-20](permissions.md#uc-com-20), [UC-COM-21](permissions.md#uc-com-21) | Chỉ owner quản lý vai trò/assignment |
| [COM-029](servers.md#com-029) | [UC-COM-01](servers.md#uc-com-01), [UC-COM-04](servers.md#uc-com-04) | Tạo bởi account đã xác minh, metadata chỉ owner sửa |
| [COM-030](memberships.md#com-030) | [UC-COM-07](memberships.md#uc-com-07), [UC-COM-08](memberships.md#uc-com-08) | Hủy/từ chối và gửi yêu cầu mới |
| [COM-031](invitations.md#com-031) | [UC-COM-12](invitations.md#uc-com-12), [UC-COM-13](invitations.md#uc-com-13) | Nhận mời đích danh mới tạo membership |
| [COM-032](invitations.md#com-032) | [UC-COM-09](invitations.md#uc-com-09), [UC-COM-10](invitations.md#uc-com-10), [UC-COM-11](invitations.md#uc-com-11) | Hạn/lượt và thu hồi link |
| [COM-033](servers.md#com-033) | [UC-COM-14](servers.md#uc-com-14), [UC-COM-15](memberships.md#uc-com-15) | Chuyển ngay; owner phải chuyển trước rời |
| [COM-034](channels.md#com-034) | [UC-COM-16](channels.md#uc-com-16), [UC-COM-18](channels.md#uc-com-18), [UC-COM-19](channels.md#uc-com-19) | Tạo/sửa/xóa phòng và vòng đời |
| [COM-035](memberships.md#com-035) | [UC-COM-06](memberships.md#uc-com-06), [UC-COM-07](memberships.md#uc-com-07), [UC-COM-11](invitations.md#uc-com-11), [UC-COM-13](invitations.md#uc-com-13), [UC-COM-15](memberships.md#uc-com-15) | Rejoin dùng epoch mới và role mặc định |
| [COM-036](invitations.md#com-036) | [UC-COM-12](invitations.md#uc-com-12), [UC-COM-13](invitations.md#uc-com-13) | Mời đích danh hết hạn hoặc terminal không accept được |
| [COM-037](permissions.md#com-037) | [UC-COM-01](servers.md#uc-com-01), [UC-COM-20](permissions.md#uc-com-20), [UC-COM-21](permissions.md#uc-com-21) | @everyone và giới hạn custom role, union management |
| [COM-038](servers.md#com-038) | [UC-COM-02](servers.md#uc-com-02) | Search/UTF-16/khớp/phân trang |
| [COM-039](servers.md#com-039) | [UC-COM-01](servers.md#uc-com-01), [UC-COM-04](servers.md#uc-com-04), [UC-COM-16](channels.md#uc-com-16), [UC-COM-18](channels.md#uc-com-18), [UC-COM-20](permissions.md#uc-com-20) | Tên Unicode; xóa toàn server ngoài v1 |
| [COM-040](servers.md#com-040) | [UC-COM-04](servers.md#uc-com-04), [UC-COM-07](memberships.md#uc-com-07), [UC-COM-08](memberships.md#uc-com-08) | Private switch kết thúc pending nguyên tử |
| [COM-041](invitations.md#com-041) | [UC-COM-09](invitations.md#uc-com-09), [UC-COM-10](invitations.md#uc-com-10), [UC-COM-11](invitations.md#uc-com-11), [UC-COM-12](invitations.md#uc-com-12), [UC-COM-13](invitations.md#uc-com-13) | Hiệu lực mời độc lập creator; actor thao tác xét quyền hiện hành |
| [COM-042](permissions.md#com-042) | [UC-COM-16](channels.md#uc-com-16), [UC-COM-17](channels.md#uc-com-17), [UC-COM-18](channels.md#uc-com-18), [UC-COM-19](channels.md#uc-com-19), [UC-COM-22](permissions.md#uc-com-22) | Quản lý phòng có sẵn cần view |

<a id="use-case-coverage"></a>

## Đối chiếu use case với API và kiểm thử

API trong bảng là hợp đồng mục tiêu tại [community.openapi.json](../../../contracts/community.openapi.json); metadata từng operation ghi phần đã triển khai/partial và nhánh tương ứng. Không suy toàn bộ contract đã chạy hoặc chưa chạy. Các đường dẫn dùng prefix `/api/v1`; `{id}` là serverId, `{channelId}` là phòng đích. AC và TC dẫn tới [tiêu chí chấp nhận](#acceptance) và [ca kiểm thử](#tests) hiện có. Bảng này giữ phạm vi đầy đủ; test thực tế/kết quả/phần chưa chứng minh ở [verification](../delivery/verification.md), tiến độ tại [status.md](../status.md).

| Use case | API/thành phần mục tiêu | AC-COM | TC hiện có và phụ thuộc |
|---|---|---|---|
| [UC-COM-01](servers.md#uc-com-01) | POST /servers | [AC-COM-29](servers.md#ac-com-29), [AC-COM-36](permissions.md#ac-com-36), [AC-COM-38](servers.md#ac-com-38) | [TC-COM-02](servers.md#tc-com-02), [TC-COM-19](servers.md#tc-com-19), [TC-COM-23](integration.md#tc-com-23), [TC-COM-26](integration.md#tc-com-26); account guard, server/owner/@everyone/operation nguyên tử |
| [UC-COM-02](servers.md#uc-com-02) | GET /servers/search; GET /servers/{id} | [AC-COM-01](servers.md#ac-com-01), [AC-COM-19](invitations.md#ac-com-19), [AC-COM-37](servers.md#ac-com-37) | [TC-COM-01](servers.md#tc-com-01), [TC-COM-18](servers.md#tc-com-18); search key/cursor/visibility |
| [UC-COM-03](servers.md#uc-com-03) | GET /servers; GET /servers/{id}; GET /servers/{id}/membership/me | [AC-COM-06](permissions.md#ac-com-06), [AC-COM-19](invitations.md#ac-com-19), [AC-COM-21](memberships.md#ac-com-21), [AC-COM-35](memberships.md#ac-com-35) | [TC-COM-11](memberships.md#tc-com-11), [TC-COM-16](memberships.md#tc-com-16), [TC-COM-22](memberships.md#tc-com-22); cần bổ sung assertion list/detail/own status sau left |
| [UC-COM-04](servers.md#uc-com-04) | PATCH /servers/{id} | [AC-COM-29](servers.md#ac-com-29), [AC-COM-38](servers.md#ac-com-38), [AC-COM-39](servers.md#ac-com-39), [AC-COM-40](servers.md#ac-com-40) | [TC-COM-02](servers.md#tc-com-02), [TC-COM-19](servers.md#tc-com-19), [TC-COM-20](servers.md#tc-com-20), [TC-COM-27](servers.md#tc-com-27); private switch tranh approve/cancel |
| [UC-COM-05](servers.md#uc-com-05) | PATCH /servers/{id}/join-mode | [AC-COM-08](servers.md#ac-com-08) | [TC-COM-07](invitations.md#tc-com-07); quyền/version và ảnh hưởng request pending cần assertion riêng |
| [UC-COM-06](memberships.md#uc-com-06) | POST /servers/{id}/join → membership | [AC-COM-02](memberships.md#ac-com-02), [AC-COM-35](memberships.md#ac-com-35) | [TC-COM-03](memberships.md#tc-com-03), [TC-COM-16](memberships.md#tc-com-16), [TC-COM-22](memberships.md#tc-com-22); membership epoch/unique/role mặc định |
| [UC-COM-07](memberships.md#uc-com-07) | POST /servers/{id}/join → pending; GET .../join-requests/me; DELETE .../join-requests/{requestId} | [AC-COM-03](memberships.md#ac-com-03), [AC-COM-30](memberships.md#ac-com-30), [AC-COM-40](servers.md#ac-com-40) | [TC-COM-04](memberships.md#tc-com-04), [TC-COM-05](memberships.md#tc-com-05), [TC-COM-20](servers.md#tc-com-20); pending unique/transition |
| [UC-COM-08](memberships.md#uc-com-08) | GET .../join-requests; POST .../{requestId}/approve hoặc /reject | [AC-COM-10](memberships.md#ac-com-10), [AC-COM-30](memberships.md#ac-com-30), [AC-COM-40](servers.md#ac-com-40) | [TC-COM-04](memberships.md#tc-com-04), [TC-COM-05](memberships.md#tc-com-05), [TC-COM-20](servers.md#tc-com-20); guard actor/target và membership cùng commit |
| [UC-COM-09](invitations.md#uc-com-09) | GET/POST .../invites; GET .../invites/{inviteId}/link | [AC-COM-09](invitations.md#ac-com-09), [AC-COM-32](invitations.md#ac-com-32), [AC-COM-41](invitations.md#ac-com-41) | [TC-COM-07](invitations.md#tc-com-07), [TC-COM-21](invitations.md#tc-com-21), [TC-COM-23](integration.md#tc-com-23), [TC-COM-24](invitations.md#tc-com-24); secret/key ring/operation |
| [UC-COM-10](invitations.md#uc-com-10) | DELETE .../invites/{inviteId} | [AC-COM-20](invitations.md#ac-com-20), [AC-COM-32](invitations.md#ac-com-32), [AC-COM-41](invitations.md#ac-com-41) | [TC-COM-06](invitations.md#tc-com-06), [TC-COM-07](invitations.md#tc-com-07), [TC-COM-21](invitations.md#tc-com-21), [TC-COM-24](invitations.md#tc-com-24); revoke tranh join |
| [UC-COM-11](invitations.md#uc-com-11) | POST /invites/preview; POST /invites/join | [AC-COM-04](invitations.md#ac-com-04), [AC-COM-05](invitations.md#ac-com-05), [AC-COM-19](invitations.md#ac-com-19), [AC-COM-20](invitations.md#ac-com-20), [AC-COM-32](invitations.md#ac-com-32), [AC-COM-35](memberships.md#ac-com-35), [AC-COM-41](invitations.md#ac-com-41) | [TC-COM-06](invitations.md#tc-com-06), [TC-COM-13](invitations.md#tc-com-13), [TC-COM-16](memberships.md#tc-com-16), [TC-COM-21](invitations.md#tc-com-21), [TC-COM-24](invitations.md#tc-com-24); lượt cuối/rollback, pending joined_elsewhere |
| [UC-COM-12](invitations.md#uc-com-12) | GET/POST .../member-invitations; DELETE .../{invitationId} | [AC-COM-19](invitations.md#ac-com-19), [AC-COM-31](invitations.md#ac-com-31), [AC-COM-35](memberships.md#ac-com-35), [AC-COM-41](invitations.md#ac-com-41) | [TC-COM-08](invitations.md#tc-com-08), [TC-COM-17](invitations.md#tc-com-17), [TC-COM-21](invitations.md#tc-com-21), [TC-COM-23](integration.md#tc-com-23); unique pending/expiry/operation |
| [UC-COM-13](invitations.md#uc-com-13) | GET /member-invitations; POST .../{invitationId}/accept hoặc /reject | [AC-COM-19](invitations.md#ac-com-19), [AC-COM-31](invitations.md#ac-com-31), [AC-COM-35](memberships.md#ac-com-35), [AC-COM-41](invitations.md#ac-com-41) | [TC-COM-08](invitations.md#tc-com-08), [TC-COM-17](invitations.md#tc-com-17), [TC-COM-21](invitations.md#tc-com-21); đúng recipient/terminal/epoch |
| [UC-COM-14](servers.md#uc-com-14) | POST /servers/{id}/ownership-transfer; GET .../members | [AC-COM-33](servers.md#ac-com-33) | [TC-COM-14](servers.md#tc-com-14); Identity→server→membership lock order |
| [UC-COM-15](memberships.md#uc-com-15) | DELETE /servers/{id}/members/me | [AC-COM-21](memberships.md#ac-com-21), [AC-COM-35](memberships.md#ac-com-35) | [TC-COM-11](memberships.md#tc-com-11), [TC-COM-14](servers.md#tc-com-14), [TC-COM-16](memberships.md#tc-com-16), [TC-COM-22](memberships.md#tc-com-22); epoch/clear role/override/revocation |
| [UC-COM-16](channels.md#uc-com-16) | POST /servers/{id}/channels | [AC-COM-07](channels.md#ac-com-07), [AC-COM-11](permissions.md#ac-com-11), [AC-COM-38](servers.md#ac-com-38), [AC-COM-42](permissions.md#ac-com-42) | [TC-ACL-01](permissions.md#tc-acl-01), [TC-ACL-06](permissions.md#tc-acl-06), [TC-ACL-08](permissions.md#tc-acl-08); [TC-COM-19](servers.md#tc-com-19), [TC-COM-23](integration.md#tc-com-23), [TC-COM-26](integration.md#tc-com-26), [TC-COM-27](servers.md#tc-com-27); lifecycle Messaging/Media |
| [UC-COM-17](channels.md#uc-com-17) | GET .../channels; GET .../channels/{channelId}; GET .../messages | [AC-COM-06](permissions.md#ac-com-06), [AC-COM-11](permissions.md#ac-com-11), [AC-COM-13](integration.md#ac-com-13), [AC-COM-14](channels.md#ac-com-14), [AC-COM-15](channels.md#ac-com-15), [AC-COM-25](permissions.md#ac-com-25), [AC-COM-26](permissions.md#ac-com-26), [AC-COM-27](permissions.md#ac-com-27), [AC-COM-42](permissions.md#ac-com-42) | [TC-ACL-01](permissions.md#tc-acl-01), [TC-ACL-02](permissions.md#tc-acl-02), [TC-ACL-03](permissions.md#tc-acl-03), [TC-ACL-04](permissions.md#tc-acl-04), [TC-ACL-05](permissions.md#tc-acl-05), [TC-ACL-08](permissions.md#tc-acl-08), [TC-ACL-12](permissions.md#tc-acl-12); [TC-COM-09](channels.md#tc-com-09), [TC-COM-12](integration.md#tc-com-12), [TC-COM-25](integration.md#tc-com-25), [TC-COM-27](servers.md#tc-com-27); lịch sử theo guard/projection |
| [UC-COM-18](channels.md#uc-com-18) | PATCH .../channels/{channelId} | [AC-COM-34](channels.md#ac-com-34), [AC-COM-38](servers.md#ac-com-38), [AC-COM-42](permissions.md#ac-com-42) | [TC-ACL-12](permissions.md#tc-acl-12); [TC-COM-19](servers.md#tc-com-19); cần assertion sửa tên/topic/version riêng |
| [UC-COM-19](channels.md#uc-com-19) | DELETE .../channels/{channelId} | [AC-COM-34](channels.md#ac-com-34), [AC-COM-39](servers.md#ac-com-39), [AC-COM-42](permissions.md#ac-com-42) | [TC-ACL-12](permissions.md#tc-acl-12); [TC-COM-15](channels.md#tc-com-15), [TC-COM-25](integration.md#tc-com-25), [TC-COM-27](servers.md#tc-com-27); lifecycle/retention, cutoff media riêng |
| [UC-COM-20](permissions.md#uc-com-20) | GET/POST .../roles; PATCH/DELETE .../roles/{roleId} | [AC-COM-28](permissions.md#ac-com-28), [AC-COM-36](permissions.md#ac-com-36), [AC-COM-38](servers.md#ac-com-38) | [TC-ACL-06](permissions.md#tc-acl-06), [TC-ACL-08](permissions.md#tc-acl-08), [TC-ACL-09](permissions.md#tc-acl-09), [TC-ACL-11](permissions.md#tc-acl-11); [TC-COM-19](servers.md#tc-com-19), [TC-COM-23](integration.md#tc-com-23), [TC-COM-26](integration.md#tc-com-26); limit/system role/version |
| [UC-COM-21](permissions.md#uc-com-21) | GET .../members; GET/PUT .../members/{userId}/roles | [AC-COM-28](permissions.md#ac-com-28), [AC-COM-35](memberships.md#ac-com-35), [AC-COM-36](permissions.md#ac-com-36) | [TC-ACL-06](permissions.md#tc-acl-06), [TC-ACL-08](permissions.md#tc-acl-08); [TC-COM-16](memberships.md#tc-com-16), [TC-COM-22](memberships.md#tc-com-22), [TC-COM-25](integration.md#tc-com-25); đúng epoch/union quyền |
| [UC-COM-22](permissions.md#uc-com-22) | GET/PUT .../channels/{channelId}/access; GET .../roles; GET .../members | [AC-COM-11](permissions.md#ac-com-11), [AC-COM-16](permissions.md#ac-com-16), [AC-COM-25](permissions.md#ac-com-25), [AC-COM-26](permissions.md#ac-com-26), [AC-COM-27](permissions.md#ac-com-27), [AC-COM-42](permissions.md#ac-com-42) | [TC-ACL-02](permissions.md#tc-acl-02), [TC-ACL-03](permissions.md#tc-acl-03), [TC-ACL-04](permissions.md#tc-acl-04), [TC-ACL-05](permissions.md#tc-acl-05), [TC-ACL-08](permissions.md#tc-acl-08), [TC-ACL-10](permissions.md#tc-acl-10), [TC-ACL-12](permissions.md#tc-acl-12); [TC-COM-22](memberships.md#tc-com-22), [TC-COM-25](integration.md#tc-com-25); ACL snapshot/guard |
| [UC-COM-23](integration.md#uc-com-23) | POST .../channels/{channelId}/messages | [AC-COM-12](integration.md#ac-com-12), [AC-COM-17](permissions.md#ac-com-17), [AC-COM-18](integration.md#ac-com-18), [AC-COM-22](integration.md#ac-com-22), [AC-COM-24](integration.md#ac-com-24) | [TC-COM-10](integration.md#tc-com-10), [TC-COM-12](integration.md#tc-com-12), [TC-COM-25](integration.md#tc-com-25), [TC-COM-27](servers.md#tc-com-27); TC-TEXT ở DM, HMAC/sequence/outbox |
| [UC-COM-24](integration.md#uc-com-24) | PATCH/DELETE .../messages/{messageId} | [AC-COM-12](integration.md#ac-com-12), [AC-COM-13](integration.md#ac-com-13), [AC-COM-23](integration.md#ac-com-23), [AC-COM-24](integration.md#ac-com-24) | [TC-COM-10](integration.md#tc-com-10); TC-TEXT ở DM, tác giả/version/tombstone |
| [UC-COM-25](integration.md#uc-com-25) | /hubs/chat; REST bù lịch sử và đọc lại resource | [AC-COM-06](permissions.md#ac-com-06), [AC-COM-11](permissions.md#ac-com-11), [AC-COM-13](integration.md#ac-com-13), [AC-COM-14](channels.md#ac-com-14), [AC-COM-18](integration.md#ac-com-18), [AC-COM-21](memberships.md#ac-com-21), [AC-COM-25](permissions.md#ac-com-25), [AC-COM-26](permissions.md#ac-com-26), [AC-COM-34](channels.md#ac-com-34), [AC-COM-35](memberships.md#ac-com-35), [AC-COM-42](permissions.md#ac-com-42) | [TC-ACL-07](permissions.md#tc-acl-07), [TC-ACL-11](permissions.md#tc-acl-11); [TC-COM-09](channels.md#tc-com-09), [TC-COM-11](memberships.md#tc-com-11), [TC-COM-15](channels.md#tc-com-15), [TC-COM-22](memberships.md#tc-com-22), [TC-COM-25](integration.md#tc-com-25); cần proof dispatch/reconnect/notification routing và DEC-083 |

42 AC-COM và 42 quy tắc COM đều được truy vết; [COM-009](integration.md#com-009), [COM-039](servers.md#com-039) có phần giới hạn scope. Mỗi gói phải có assertion cho endpoint/nhánh nó thực hiện: list/detail và metadata edit đã có test thực tế; lịch sử/đối soát leave của [UC-COM-03](servers.md#uc-com-03), join mode [UC-COM-05](servers.md#uc-com-05) và định tuyến [UC-COM-25](integration.md#uc-com-25) còn proof theo [verification](../delivery/verification.md). [TC-COM-23](integration.md#tc-com-23), [TC-COM-26](integration.md#tc-com-26) cùng fixtures operation/quyền kiểm chứng cơ chế dùng chung; fixture khớp không chứng minh UC/API đã đạt. Phương pháp ghi kết quả và đo tải/thu hồi theo [nghiệm thu](../../../release-operations.md#testing).

<a id="acceptance"></a>

## Tiêu chí chấp nhận

42 AC-COM giữ nguyên nội dung, được đặt ở thành phần chủ trì và dẫn chiếu từ [ma trận UC/API/AC/TC](#use-case-coverage).

| Mã | Nguồn chuẩn |
|---|---|
| [AC-COM-01](servers.md#ac-com-01) | [Servers — Cộng đồng và chủ sở hữu](servers.md#ac-com-01) |
| [AC-COM-02](memberships.md#ac-com-02) | [Memberships — Thành viên và yêu cầu tham gia](memberships.md#ac-com-02) |
| [AC-COM-03](memberships.md#ac-com-03) | [Memberships — Thành viên và yêu cầu tham gia](memberships.md#ac-com-03) |
| [AC-COM-04](invitations.md#ac-com-04) | [Invitations — Link mời và lời mời đích danh](invitations.md#ac-com-04) |
| [AC-COM-05](invitations.md#ac-com-05) | [Invitations — Link mời và lời mời đích danh](invitations.md#ac-com-05) |
| [AC-COM-06](permissions.md#ac-com-06) | [Permissions — Vai trò, ACL và kiểm tra quyền](permissions.md#ac-com-06) |
| [AC-COM-07](channels.md#ac-com-07) | [Channels — Phòng và vòng đời phòng](channels.md#ac-com-07) |
| [AC-COM-08](servers.md#ac-com-08) | [Servers — Cộng đồng và chủ sở hữu](servers.md#ac-com-08) |
| [AC-COM-09](invitations.md#ac-com-09) | [Invitations — Link mời và lời mời đích danh](invitations.md#ac-com-09) |
| [AC-COM-10](memberships.md#ac-com-10) | [Memberships — Thành viên và yêu cầu tham gia](memberships.md#ac-com-10) |
| [AC-COM-11](permissions.md#ac-com-11) | [Permissions — Vai trò, ACL và kiểm tra quyền](permissions.md#ac-com-11) |
| [AC-COM-12](integration.md#ac-com-12) | [Community — Giao dịch và tích hợp dùng chung](integration.md#ac-com-12) |
| [AC-COM-13](integration.md#ac-com-13) | [Community — Giao dịch và tích hợp dùng chung](integration.md#ac-com-13) |
| [AC-COM-14](channels.md#ac-com-14) | [Channels — Phòng và vòng đời phòng](channels.md#ac-com-14) |
| [AC-COM-15](channels.md#ac-com-15) | [Channels — Phòng và vòng đời phòng](channels.md#ac-com-15) |
| [AC-COM-16](permissions.md#ac-com-16) | [Permissions — Vai trò, ACL và kiểm tra quyền](permissions.md#ac-com-16) |
| [AC-COM-17](permissions.md#ac-com-17) | [Permissions — Vai trò, ACL và kiểm tra quyền](permissions.md#ac-com-17) |
| [AC-COM-18](integration.md#ac-com-18) | [Community — Giao dịch và tích hợp dùng chung](integration.md#ac-com-18) |
| [AC-COM-19](invitations.md#ac-com-19) | [Invitations — Link mời và lời mời đích danh](invitations.md#ac-com-19) |
| [AC-COM-20](invitations.md#ac-com-20) | [Invitations — Link mời và lời mời đích danh](invitations.md#ac-com-20) |
| [AC-COM-21](memberships.md#ac-com-21) | [Memberships — Thành viên và yêu cầu tham gia](memberships.md#ac-com-21) |
| [AC-COM-22](integration.md#ac-com-22) | [Community — Giao dịch và tích hợp dùng chung](integration.md#ac-com-22) |
| [AC-COM-23](integration.md#ac-com-23) | [Community — Giao dịch và tích hợp dùng chung](integration.md#ac-com-23) |
| [AC-COM-24](integration.md#ac-com-24) | [Community — Giao dịch và tích hợp dùng chung](integration.md#ac-com-24) |
| [AC-COM-25](permissions.md#ac-com-25) | [Permissions — Vai trò, ACL và kiểm tra quyền](permissions.md#ac-com-25) |
| [AC-COM-26](permissions.md#ac-com-26) | [Permissions — Vai trò, ACL và kiểm tra quyền](permissions.md#ac-com-26) |
| [AC-COM-27](permissions.md#ac-com-27) | [Permissions — Vai trò, ACL và kiểm tra quyền](permissions.md#ac-com-27) |
| [AC-COM-28](permissions.md#ac-com-28) | [Permissions — Vai trò, ACL và kiểm tra quyền](permissions.md#ac-com-28) |
| [AC-COM-29](servers.md#ac-com-29) | [Servers — Cộng đồng và chủ sở hữu](servers.md#ac-com-29) |
| [AC-COM-30](memberships.md#ac-com-30) | [Memberships — Thành viên và yêu cầu tham gia](memberships.md#ac-com-30) |
| [AC-COM-31](invitations.md#ac-com-31) | [Invitations — Link mời và lời mời đích danh](invitations.md#ac-com-31) |
| [AC-COM-32](invitations.md#ac-com-32) | [Invitations — Link mời và lời mời đích danh](invitations.md#ac-com-32) |
| [AC-COM-33](servers.md#ac-com-33) | [Servers — Cộng đồng và chủ sở hữu](servers.md#ac-com-33) |
| [AC-COM-34](channels.md#ac-com-34) | [Channels — Phòng và vòng đời phòng](channels.md#ac-com-34) |
| [AC-COM-35](memberships.md#ac-com-35) | [Memberships — Thành viên và yêu cầu tham gia](memberships.md#ac-com-35) |
| [AC-COM-36](permissions.md#ac-com-36) | [Permissions — Vai trò, ACL và kiểm tra quyền](permissions.md#ac-com-36) |
| [AC-COM-37](servers.md#ac-com-37) | [Servers — Cộng đồng và chủ sở hữu](servers.md#ac-com-37) |
| [AC-COM-38](servers.md#ac-com-38) | [Servers — Cộng đồng và chủ sở hữu](servers.md#ac-com-38) |
| [AC-COM-39](servers.md#ac-com-39) | [Servers — Cộng đồng và chủ sở hữu](servers.md#ac-com-39) |
| [AC-COM-40](servers.md#ac-com-40) | [Servers — Cộng đồng và chủ sở hữu](servers.md#ac-com-40) |
| [AC-COM-41](invitations.md#ac-com-41) | [Invitations — Link mời và lời mời đích danh](invitations.md#ac-com-41) |
| [AC-COM-42](permissions.md#ac-com-42) | [Permissions — Vai trò, ACL và kiểm tra quyền](permissions.md#ac-com-42) |

<a id="tests"></a>

## Ca kiểm thử

[TC-ACL-01–12](permissions.md#tests) thuộc Permissions. 27 TC-COM nằm ở nguồn chủ trì dưới đây; ca xuyên phần được dẫn chiếu cùng AC trong bảng UC. Kết quả từng phạm vi được dẫn chiếu từ [status.md](../status.md); danh mục ca kiểm thử không tự ghi nhận đạt. [Dữ liệu và cách ghi bằng chứng](integration.md#evidence) áp dụng chung.

| Mã | Nguồn chuẩn |
|---|---|
| [TC-COM-01](servers.md#tc-com-01) | [Servers — Cộng đồng và chủ sở hữu](servers.md#tc-com-01) |
| [TC-COM-02](servers.md#tc-com-02) | [Servers — Cộng đồng và chủ sở hữu](servers.md#tc-com-02) |
| [TC-COM-03](memberships.md#tc-com-03) | [Memberships — Thành viên và yêu cầu tham gia](memberships.md#tc-com-03) |
| [TC-COM-04](memberships.md#tc-com-04) | [Memberships — Thành viên và yêu cầu tham gia](memberships.md#tc-com-04) |
| [TC-COM-05](memberships.md#tc-com-05) | [Memberships — Thành viên và yêu cầu tham gia](memberships.md#tc-com-05) |
| [TC-COM-06](invitations.md#tc-com-06) | [Invitations — Link mời và lời mời đích danh](invitations.md#tc-com-06) |
| [TC-COM-07](invitations.md#tc-com-07) | [Invitations — Link mời và lời mời đích danh](invitations.md#tc-com-07) |
| [TC-COM-08](invitations.md#tc-com-08) | [Invitations — Link mời và lời mời đích danh](invitations.md#tc-com-08) |
| [TC-COM-09](channels.md#tc-com-09) | [Channels — Phòng và vòng đời phòng](channels.md#tc-com-09) |
| [TC-COM-10](integration.md#tc-com-10) | [Community — Giao dịch và tích hợp dùng chung](integration.md#tc-com-10) |
| [TC-COM-11](memberships.md#tc-com-11) | [Memberships — Thành viên và yêu cầu tham gia](memberships.md#tc-com-11) |
| [TC-COM-12](integration.md#tc-com-12) | [Community — Giao dịch và tích hợp dùng chung](integration.md#tc-com-12) |
| [TC-COM-13](invitations.md#tc-com-13) | [Invitations — Link mời và lời mời đích danh](invitations.md#tc-com-13) |
| [TC-COM-14](servers.md#tc-com-14) | [Servers — Cộng đồng và chủ sở hữu](servers.md#tc-com-14) |
| [TC-COM-15](channels.md#tc-com-15) | [Channels — Phòng và vòng đời phòng](channels.md#tc-com-15) |
| [TC-COM-16](memberships.md#tc-com-16) | [Memberships — Thành viên và yêu cầu tham gia](memberships.md#tc-com-16) |
| [TC-COM-17](invitations.md#tc-com-17) | [Invitations — Link mời và lời mời đích danh](invitations.md#tc-com-17) |
| [TC-COM-18](servers.md#tc-com-18) | [Servers — Cộng đồng và chủ sở hữu](servers.md#tc-com-18) |
| [TC-COM-19](servers.md#tc-com-19) | [Servers — Cộng đồng và chủ sở hữu](servers.md#tc-com-19) |
| [TC-COM-20](servers.md#tc-com-20) | [Servers — Cộng đồng và chủ sở hữu](servers.md#tc-com-20) |
| [TC-COM-21](invitations.md#tc-com-21) | [Invitations — Link mời và lời mời đích danh](invitations.md#tc-com-21) |
| [TC-COM-22](memberships.md#tc-com-22) | [Memberships — Thành viên và yêu cầu tham gia](memberships.md#tc-com-22) |
| [TC-COM-23](integration.md#tc-com-23) | [Community — Giao dịch và tích hợp dùng chung](integration.md#tc-com-23) |
| [TC-COM-24](invitations.md#tc-com-24) | [Invitations — Link mời và lời mời đích danh](invitations.md#tc-com-24) |
| [TC-COM-25](integration.md#tc-com-25) | [Community — Giao dịch và tích hợp dùng chung](integration.md#tc-com-25) |
| [TC-COM-26](integration.md#tc-com-26) | [Community — Giao dịch và tích hợp dùng chung](integration.md#tc-com-26) |
| [TC-COM-27](servers.md#tc-com-27) | [Servers — Cộng đồng và chủ sở hữu](servers.md#tc-com-27) |
