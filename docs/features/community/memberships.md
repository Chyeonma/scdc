# SCDC — Thành viên và yêu cầu tham gia

Người dùng tham gia server công khai (vào ngay hoặc gửi yêu cầu chờ duyệt), và có thể rời server. Phần Memberships của Community sở hữu tư cách thành viên, epoch thành viên và các thao tác tham gia, xin duyệt, duyệt, rời.

<a id="implementation"></a>

**Trạng thái** (nền `900e226`, 2026-10-13):

| Chức năng | Trạng thái | Bằng chứng |
|---|---|---|
| Tham gia server công khai vào ngay (một phần UC-COM-06) | Đã có API và giao diện; tham gia lặp hay đồng thời giữ cùng epoch | [Tham gia trực tiếp](../../records/verification/community/direct-join.md) |
| Tham gia lại sau khi rời | Đã kiểm thử qua fixture: epoch mới, dọn vai trò và ngoại lệ cũ. Chưa có API rời | [Tham gia trực tiếp](../../records/verification/community/direct-join.md) |
| Xin duyệt, duyệt, rời (UC-COM-07, 08, 15; nhánh chờ duyệt của UC-COM-06) | Chỉ có thiết kế | [Kế hoạch](../../project/planning.md#community-work-items) |

Thông tin server và chủ sở hữu: [Servers](servers.md). Gán vai trò và ngoại lệ phải dùng đúng epoch: [quyền](access-control.md). Điều kiện chung: [cơ chế Community](mechanics.md#use-case-conditions). Cách chạy: [hướng dẫn](../../guides/development.md#community).

<a id="requirements"></a>

<a id="phạm-vi-và-quy-tắc"></a>

## Quy tắc

| Mã | Quy tắc |
|---|---|
| <a id="com-001"></a> COM-001 | Tham gia qua tìm kiếm hoặc link mời. |
| <a id="com-011"></a> COM-011 | Chủ server hoặc người có quyền duyệt được duyệt yêu cầu tham gia. |
| <a id="com-021"></a> COM-021 | Thành viên thường tự rời được. |
| <a id="com-030"></a> COM-030 | Người duyệt từ chối được. Người gửi xem trạng thái và hủy được yêu cầu đang chờ; sau khi bị từ chối hay đã hủy thì gửi lại được. |
| <a id="com-035"></a> COM-035 | Tham gia lại theo cách tham gia hiện tại của server và vai trò mặc định; không khôi phục vai trò cũ. Được xem lịch sử các phòng hiện được phép xem. |

<a id="use-cases"></a>

<a id="use-case"></a>

## Ca sử dụng

Điều kiện, version, epoch, gửi lại và lỗi dùng chung: [quy ước tích hợp](mechanics.md#use-case-conditions). Quy tắc, AC, TC của từng ca: [bảng truy vết](traceability.md#use-case-coverage).

<a id="uc-com-06"></a>

<a id="uc-com-06--tham-gia-cộng-đồng-công-khai-vào-ngay"></a>

### UC-COM-06 — Tham gia server công khai (vào ngay)

**Ai làm:** người dùng đủ điều kiện, chưa là thành viên.

**Điều kiện trước:** server công khai, đang hoạt động, cách tham gia là "vào ngay" tại thời điểm bấm.

**Khi nào:** bấm Tham gia ở COM-S02.

**Các bước:**

1. Trong một transaction, kiểm tra lại chế độ công khai, cách tham gia, người dùng và tư cách hiện tại.
2. Tạo hoặc kích hoạt lại tư cách với `membershipId` mới; chỉ áp vai trò mặc định. Kết thúc yêu cầu đang chờ cũ (lý do `joined_elsewhere`).
3. Sau commit, trả tư cách và mở các phòng được xem ([UC-COM-17](channels.md#uc-com-17)).

**Ngoại lệ:**

- Người đã là thành viên bấm lặp hay đồng thời: trả tư cách hiện tại, không tạo thêm.
- Server vừa đổi sang chờ duyệt: chuyển sang [UC-COM-07](#uc-com-07). Đã đổi sang riêng tư: chặn.
- Mất phản hồi: đối chiếu qua [UC-COM-03](servers.md#uc-com-03) trước khi làm tiếp.

**Kết quả:** một tư cách thành viên. Tham gia lại không khôi phục vai trò hay ngoại lệ cũ. Lịch sử phòng đọc theo quyền xem hiện tại.

<a id="uc-com-07"></a>

<a id="uc-com-07--gửi-xem-và-hủy-yêu-cầu-tham-gia"></a>

### UC-COM-07 — Gửi, xem, hủy yêu cầu tham gia

**Ai làm:** người dùng đủ điều kiện, chưa là thành viên. Chỉ người gửi quản lý yêu cầu của mình.

**Điều kiện trước:** gửi mới cần server công khai, đang hoạt động, bật chờ duyệt. Xem hay hủy không cần là thành viên.

**Khi nào:** gửi yêu cầu, xem trạng thái, hoặc hủy ở COM-S02 hay COM-S06.

**Các bước:**

1. **Gửi:** kiểm tra cách tham gia và chế độ công khai; trả yêu cầu đang chờ duy nhất của cặp server và người dùng, chưa có thì tạo mới.
2. **Xem:** trả trạng thái mới nhất; đang chờ thì chưa có quyền phòng. Chưa từng gửi thì trả null.
3. **Hủy:** kiểm tra `expectedVersion`, chuyển sang đã hủy. Sau khi bị từ chối hay đã hủy, người dùng gửi lại được nếu server vẫn cho phép.

**Ngoại lệ:**

- Gửi lặp trả lại yêu cầu đang chờ.
- Hủy tranh với duyệt, từ chối, hay đổi sang riêng tư: chỉ một thao tác thắng.
- Server đổi sang riêng tư: người gửi vẫn thấy trạng thái đã hủy (`server_private`), nhưng không gửi mới qua đường công khai được.
- Vào bằng cách khác: yêu cầu đang chờ đóng lại (`joined_elsewhere`); giao diện cho biết đã vào bằng đường nào.

**Kết quả:** người gửi theo dõi được yêu cầu của mình. Đang chờ, bị từ chối, đã hủy đều không tạo thành viên. Yêu cầu mới có ID mới, không mở lại yêu cầu cũ.

<a id="uc-com-08"></a>

<a id="uc-com-08--duyệt-hoặc-từ-chối-yêu-cầu-tham-gia"></a>

### UC-COM-08 — Duyệt hoặc từ chối yêu cầu

**Ai làm:** chủ server hoặc thành viên có `review_join_requests`.

**Điều kiện trước:** yêu cầu đang chờ; người duyệt còn quyền tại thời điểm xử lý.

**Khi nào:** mở COM-S06, chọn Duyệt hoặc Từ chối.

**Các bước:**

1. Hệ thống chỉ trả danh sách yêu cầu cho người có quyền duyệt.
2. Người duyệt gửi quyết định kèm `expectedVersion`; hệ thống đọc lại trạng thái và quyền.
3. **Duyệt:** chuyển trạng thái và tạo hoặc kích hoạt tư cách thành viên cùng transaction. **Từ chối:** chỉ chuyển sang bị từ chối. Sau commit, người gửi và người duyệt nhận trạng thái mới.

**Ngoại lệ:**

- Yêu cầu đã hủy, bị từ chối hay đã xử lý: không chuyển nữa; xung đột thì tải lại.
- Người được duyệt phải đủ điều kiện tại lúc duyệt.
- Người đó đã vào bằng cách khác: đóng yêu cầu (`joined_elsewhere`), giữ tư cách hiện có.
- Đổi sang riêng tư hay người gửi hủy không thể cùng thắng với duyệt.

**Kết quả:** duyệt thì có một tư cách thành viên với vai trò mặc định; từ chối thì không. Yêu cầu đã kết thúc vẫn giữ để người gửi xem.

<a id="uc-com-15"></a>

<a id="uc-com-15--rời-cộng-đồng"></a>

### UC-COM-15 — Rời server

**Ai làm:** thành viên thường, rời tư cách của chính mình.

**Điều kiện trước:** phiên hợp lệ; có `membershipId` của lần tham gia muốn rời; không phải chủ server.

**Khi nào:** chọn Rời và xác nhận ở COM-S03.

**Các bước:**

1. Kiểm tra lại `membershipId` và xem có phải chủ server không.
2. Trong cùng transaction: chuyển sang đã rời, dọn gán vai trò và ngoại lệ của epoch hiện tại, ghi thay đổi quyền.
3. Sau commit: chặn truy cập nội dung thành viên; thu hồi đăng ký realtime ([UC-COM-25](../messaging/channel-messaging.md#uc-com-25)); giao diện đóng nội dung và bản nháp của server.

**Ngoại lệ:**

- Chủ server phải chuyển quyền chủ trước ([UC-COM-14](servers.md#uc-com-14)).
- Rời lặp với epoch đã rời: trả thành công. Request rời cũ sau khi đã tham gia lại: `MEMBERSHIP_CHANGED`, không làm rời lần tham gia mới.
- Rời tranh với HTTP hay gửi tin: xử lý bằng guard và transaction.

**Kết quả:** mất quyền với nội dung server; DM vẫn dùng được; tin đã viết vẫn giữ. Muốn vào lại thì đi qua [UC-COM-06](#uc-com-06), [07](#uc-com-07), [11](invitations.md#uc-com-11) hoặc [13](invitations.md#uc-com-13), với `membershipId` mới và vai trò mặc định.

<a id="ux"></a>

<a id="ux-và-trạng-thái"></a>

## Giao diện

| Màn hình | Ai dùng | Nội dung |
|---|---|---|
| COM-S02 | Người muốn tham gia | Tham gia, gửi yêu cầu, xem trạng thái ([servers](servers.md#ux)) |
| COM-S06 Yêu cầu tham gia | Chủ server, người có quyền duyệt; người gửi | Danh sách, duyệt, từ chối; người gửi xem và hủy; gửi lại sau khi bị từ chối hay hủy |
| COM-S03 | Thành viên | Rời server |

<a id="trạng-thái-yêu-cầu-tham-gia"></a>

### Trạng thái yêu cầu tham gia

| Trạng thái | Sự kiện | Kết quả |
|---|---|---|
| Chưa có | Gửi vào server công khai chờ duyệt | Đang chờ; chưa có tư cách hay quyền phòng |
| Đang chờ | Người duyệt chấp nhận | Đã duyệt; tạo tư cách cùng transaction |
| Đang chờ | Người duyệt từ chối | Bị từ chối |
| Đang chờ | Người gửi hủy | Đã hủy |
| Bị từ chối hoặc đã hủy | Người dùng gửi lại | Tạo yêu cầu mới, không mở lại yêu cầu cũ |

Màn hình hẹp và trạng thái chung: [giao diện chung](mechanics.md#ux).

<a id="technical"></a>

## Thiết kế dữ liệu và xử lý

<a id="membership-contracts"></a>

<a id="membership-thiết-kế-dữ-liệuapi"></a>

### Xin duyệt, duyệt, rời (mục tiêu, chưa làm)

Quy ước chung: [Community](mechanics.md#contracts). Hợp đồng đầy đủ: [OpenAPI](../../contracts/community.openapi.json). Phần đã chạy (vào ngay, tham gia lại) xem [mục dưới](#publicimmediate-và-rejoin).

| Method và đường dẫn | Đầu vào | Kết quả và quyền |
|---|---|---|
| `POST /servers/{id}/join` (nhánh chờ duyệt) | Không có body | Mục tiêu: 202 yêu cầu đang chờ. Hiện tại trả 409 `JOIN_APPROVAL_REQUIRED` |
| `GET /servers/{id}/join-requests/me` | Phiên người gửi | 200 `{request: JoinRequest | null}`; không trả yêu cầu của người khác |
| `POST /servers/{id}/join-requests/{requestId}/approve` hoặc `/reject` | `{expectedVersion}` | 200 trạng thái cuối; cần quyền duyệt; chỉ từ đang chờ |
| `DELETE /servers/{id}/join-requests/{requestId}` | `expectedVersion` | 200 đã hủy; chỉ người gửi, chỉ khi đang chờ |
| `DELETE /servers/{id}/members/me?membershipId=...` | Epoch đang tham gia | 204 đã rời. Chủ server chưa chuyển: 409. Epoch cũ không làm rời epoch mới. Ngắt HTTP, realtime, media đúng hạn |

<a id="membership-giao-dịch-của-thành-phần"></a>

#### Transaction

- **Tham gia công khai:** khóa server, đọc lại chế độ và cách tham gia, rồi mới tới tư cách và yêu cầu. Đã là thành viên thì trả tư cách hiện tại. Vào ngay: tạo hoặc kích hoạt lại tư cách, kết thúc yêu cầu đang chờ cũ. Chờ duyệt: tối đa một yêu cầu đang chờ mỗi server và người dùng (dùng [partial unique index](https://www.postgresql.org/docs/18/indexes-partial.html); service vẫn khóa); gọi lặp trả lại yêu cầu đó.
- **Vào bằng cách khác:** yêu cầu đang chờ chuyển sang đã duyệt với lý do `joined_elsewhere` và `membershipId` hiện tại; giao diện ghi rõ đã vào bằng đường khác, không giả là có người duyệt.
- **Duyệt, từ chối, hủy:** kiểm tra người thực hiện, người được duyệt, version; chỉ một thao tác thắng. Duyệt tạo tư cách cùng commit; đã vào bằng đường khác thì chỉ đóng yêu cầu, không tạo thêm tư cách. Yêu cầu mới có ID mới, không sửa lịch sử cũ.

Thay đổi schema: [COM-SQL-01 đến 10](mechanics.md#schema-migration).

<a id="publicimmediate-và-rejoin"></a>

### Vào ngay và tham gia lại (đã làm)

<a id="join-api-và-lỗi"></a>

#### API và lỗi

`POST /api/v1/servers/{serverId}/join`, không có body; người thực hiện lấy từ JWT (`sub`, `sid`, `sst`). Trả 200 Membership sau commit. Không cần `clientOperationId`: khóa server và ràng buộc unique `(serverId, userId)` đã chống trùng; bấm lại thì trả tư cách hiện tại.

| HTTP và mã | Khi nào |
|---|---|
| 400 `VALIDATION_FAILED` | Body hay đầu vào sai; không nhận `userId` hay vai trò từ client |
| 401 `SESSION_INVALID` | Thiếu phiên, guard hay hạn không hợp lệ |
| 403 `ACCOUNT_ACCESS_DENIED` | Tài khoản không hoạt động hoặc email chưa xác minh |
| 404 `RESOURCE_NOT_FOUND` | Server riêng tư, không hoạt động, đã xóa, không tồn tại; không lộ thông tin |
| 409 `JOIN_APPROVAL_REQUIRED` | Server cần duyệt; không ghi gì |
| 409 `VERSION_LIMIT_REACHED` | Version chạm giới hạn số nguyên; không ghi dở |
| 503 `ACCESS_CHECK_UNAVAILABLE`, `COMMUNITY_TEMPORARILY_UNAVAILABLE` | Guard, DB hay khóa lỗi tạm; đối chiếu bằng GET |

- Thành viên đã có trên server công khai nhận lại tư cách, kể cả khi server đang chờ duyệt; không ghi sự kiện hay tăng version.
- Server riêng tư chặn đường tham gia công khai; thành viên của server riêng tư vẫn đọc được qua API riêng.
- Middleware JWT có thể trả 401 cho tài khoản không hoạt động trước khi tới guard; guard trả 403 cho tài khoản hay email không đủ điều kiện. Cả hai đều không ghi gì.

<a id="join-giao-dịch"></a>

#### Các bước trong transaction

1. Dùng transaction chung `READ COMMITTED`; guard Identity giữ khóa chia sẻ trên người dùng và hạn phiên.
2. `SELECT ... FOR UPDATE` trên server; đọc lại trạng thái, đã xóa, chế độ, cách tham gia. Thứ tự khóa Identity → server → bản ghi con áp dụng cho mọi thao tác ghi.
3. Đọc tư cách của mình dưới khóa server. Đã là thành viên thì trả tư cách hiện tại; chưa là thành viên mà server cần duyệt thì báo xung đột.
4. Kiểm tra tràn số của version và `accessVersion`. Lần đầu: UUIDv7 mới, version 1. Tham gia lại: UUIDv7 mới, version +1, đặt lại thời điểm tham gia, rời, nickname, timeout, người mời.
5. Tham gia lại thì xóa `member_roles` và `channel_user_overrides` của cặp server và người dùng trong cùng transaction. @everyone áp ngầm.
6. Tăng `accessVersion` đúng một lần (version thông tin server do trigger tăng). Ghi một sự kiện `Community.MembershipJoined.v1` với `serverId, userId, membershipId, membershipVersion, accessVersion`; `published_at` để NULL (chưa phát qua Hub).
7. Đọc tư cách, kiểm tra hạn lease lần nữa, rồi commit. Lỗi thì rollback tất cả; service không tự chạy lại.

Không cần migration mới (bảng và cột đã có từ migration 001). Khi làm UC-COM-07, 08 phải thêm schema cho yêu cầu và trạng thái `joined_elsewhere`, rồi kiểm thử lại phần tham gia.

Bằng chứng tham gia lại qua fixture không thay được bằng chứng của API rời hay việc hủy đăng ký realtime cũ.

<a id="join-webclient"></a>

#### Giao diện

- Điểm vào là thông tin server công khai (qua link chia sẻ hoặc tìm kiếm). Chưa là thành viên hoặc đã rời, server công khai vào ngay: hiện nút Tham gia. Đã là thành viên, server riêng tư, hay cần duyệt: không có nút này.
- Thao tác dùng `retry:false`, gắn với người dùng hiện tại, có `AbortController`, timeout 15 giây, chặn bấm hai lần. Đổi trang hay đổi người dùng thì bỏ phản hồi cũ.
- Thành công: tải chi tiết và danh sách bằng GET. Đọc lỗi sau commit thì không tự POST lại.
- Mất mạng, 401, 503, 500: giữ trạng thái "cần kiểm tra". Nút Kiểm tra kết quả chỉ GET. Đã là thành viên thì mở chi tiết; chưa là thành viên và server vẫn công khai vào ngay thì mới cho bấm Tham gia lại. Tải lại trang chỉ GET; 404 thì bỏ thông tin cũ; xung đột chờ duyệt thì đọc lại cách tham gia rồi dừng.
- Không giả vờ đã vào phòng khi chưa có phòng và lịch sử.

<a id="acceptance"></a>

<a id="tiêu-chí-chấp-nhận"></a>

## Tiêu chí chấp nhận

| Mã | Tình huống | Kết quả mong đợi |
|---|---|---|
| <a id="ac-com-02"></a> AC-COM-02 | Tham gia server công khai mới tạo qua tìm kiếm | Vào ngay (cấu hình mặc định) |
| <a id="ac-com-03"></a> AC-COM-03 | Gửi yêu cầu qua tìm kiếm khi server bật chờ duyệt | Yêu cầu được ghi nhận; chưa là thành viên cho tới khi được duyệt |
| <a id="ac-com-10"></a> AC-COM-10 | Server chờ duyệt có một yêu cầu; ba nhóm người ở [AC-COM-09](invitations.md#ac-com-09) lần lượt thử duyệt | Chủ server và người có quyền duyệt được; thành viên thường bị từ chối |
| <a id="ac-com-21"></a> AC-COM-21 | Thành viên thường rời server | Hết tư cách; không còn truy cập nội dung thành viên |
| <a id="ac-com-30"></a> AC-COM-30 | Người duyệt từ chối; người gửi hủy hoặc gửi lại sau khi bị từ chối hay đã hủy | Trạng thái đúng; chưa duyệt chưa là thành viên; mỗi cặp chỉ một yêu cầu đang chờ; thao tác đồng thời không cùng thắng |
| <a id="ac-com-35"></a> AC-COM-35 | Thành viên rời rồi tham gia lại; dùng lại lời mời đích danh đã hết 7 ngày, đã hủy, đã từ chối | Tham gia lại theo cách hiện tại, vai trò mặc định; lịch sử theo quyền; lời mời đã kết thúc không nhận được |

Tiêu chí liên quan nhiều phần xem [bảng truy vết](traceability.md#use-case-coverage).

<a id="tests"></a>

<a id="ca-kiểm-thử"></a>

## Ca kiểm thử

Dữ liệu và cách ghi bằng chứng: [dùng chung](../messaging/channel-messaging.md#evidence). Mỗi ca cần test cho từng endpoint và kiểm tra quyền qua API.

| Mã ca | Thao tác | Cần quan sát | Cho |
|---|---|---|---|
| <a id="tc-com-03"></a> TC-COM-03 | P tham gia server vào ngay; bấm lặp và đồng thời | Một tư cách, có quyền mặc định sau commit | [AC-COM-02](#ac-com-02) |
| <a id="tc-com-04"></a> TC-COM-04 | P gửi yêu cầu chờ duyệt, xem trạng thái; N và M thử duyệt, từ chối | Chỉ người có quyền xử lý được; đang chờ thì chưa đọc được phòng | [AC-COM-03](#ac-com-03), [10](#ac-com-10), [30](#ac-com-30) |
| <a id="tc-com-05"></a> TC-COM-05 | P hủy cùng lúc M duyệt hay từ chối; gửi lại sau khi đã kết thúc | Một thao tác thắng; trạng thái và tư cách nhất quán; gửi lại được sau khi hủy hay bị từ chối | [AC-COM-30](#ac-com-30) |
| <a id="tc-com-11"></a> TC-COM-11 | N rời khi đang đọc, gửi, nhận cập nhật | Chat bị ngắt trong 5 giây (DEC-083); DM vẫn dùng được | [AC-COM-21](#ac-com-21) |
| <a id="tc-com-16"></a> TC-COM-16 | Thành viên có vai trò riêng rời rồi vào lại với từng cách tham gia | Vai trò mặc định, không khôi phục vai trò cũ; lịch sử theo quyền mới | [AC-COM-35](#ac-com-35) |
| <a id="tc-com-22"></a> TC-COM-22 | Rời, vào lại; gửi lại PUT vai trò, ACL, rời và sự kiện thu hồi của epoch cũ | Vai trò và ngoại lệ cũ không quay lại; epoch cũ bị chặn; sự kiện cũ không xóa dữ liệu của epoch mới | [AC-COM-35](#ac-com-35), [AC-COM-42](access-control.md#ac-com-42) |

<a id="gaps"></a>

<a id="việc-còn-lại"></a>

## Còn mở

Phụ thuộc chung: [kế hoạch](../../project/planning.md#community-work-items), [migration](mechanics.md#schema-migration), [vòng đời dữ liệu](../../system/data-lifecycle.md).

- **Yêu cầu tham gia:** đã chốt (DEC-073, 096); đã thiết kế chuyển trạng thái và schema. Còn: làm và kiểm thử duyệt, hủy, đổi sang riêng tư; giới hạn tần suất chưa chốt (OQ-003, 004).
- **Rời server:** đã thiết kế epoch, dọn vai trò và ngoại lệ, chuyển chủ. Khi mất quyền thì dọn dữ liệu đệm và cấu hình của epoch cũ, giữ tin và dấu thu hồi. Bảo vệ khi khôi phục DB để [sau v1](../../project/overview.md#backup-scope) ([vòng đời](../../system/data-lifecycle.md#restore)) (OQ-003, 011).

---

Nguồn: DEC-022 (COM-001), DEC-032 (COM-011), DEC-038, DEC-087 (COM-035), DEC-045 (COM-021), DEC-073 (COM-030), DEC-083, DEC-096.
