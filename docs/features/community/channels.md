# SCDC — Phòng

Mỗi server có các phòng (channel): phòng chữ để nhắn tin, phòng thoại để nói chuyện. Thành viên chỉ thấy phòng mình được phép xem. Người có quyền tạo, sửa, xóa phòng.

Phần Channels của Community sở hữu thông tin phòng, loại phòng, danh sách theo quyền và vòng đời phòng. Messaging sở hữu không gian chat và nội dung tin.

<a id="implementation"></a>

**Trạng thái** (nền `900e226`, 2026-10-13):

| Chức năng | Trạng thái | Bằng chứng |
|---|---|---|
| Tạo phòng chữ (một phần UC-COM-16) | Đã có backend và giao diện; phòng, không gian chat, bản ghi chống trùng, outbox ghi cùng lúc | [Phòng chữ và ACL](../../records/verification/community/channels-access.md) |
| Danh sách, chi tiết (một phần UC-COM-17); sửa thông tin (UC-COM-18) | Đã kiểm thử; UC-COM-17 chưa có lịch sử tin | [Phòng chữ và ACL](../../records/verification/community/channels-access.md) |
| Cấu hình ACL qua HTTP (một phần UC-COM-22) | Đã kiểm thử; thuật toán và quyền ở [quyền](access-control.md) | [Phòng chữ và ACL](../../records/verification/community/channels-access.md) |
| Xóa phòng (UC-COM-19); phòng thoại, Media | Chỉ có thiết kế | [Kế hoạch](../../project/planning.md#community-work-items) |
| Lịch sử, gửi, nhận, Hub, mất quyền trong phòng | Chưa có; chuyển sang v1. Hub DM đã có nhưng không thay được | [Tin phòng](../messaging/channel-messaging.md) |

Quy tắc quyền xem chỉ định nghĩa ở [quyền](access-control.md#view-permissions). Điều kiện chung: [cơ chế Community](mechanics.md#use-case-conditions). Cách chạy: [hướng dẫn](../../guides/development.md#community).

<a id="requirements"></a>

<a id="phạm-vi-và-quy-tắc"></a>

## Quy tắc

| Mã | Quy tắc |
|---|---|
| <a id="com-007"></a> COM-007 | Chỉ chủ server hoặc người có quyền mới tạo được phòng. |
| <a id="com-008"></a> COM-008 | Thành viên chỉ thấy phòng mình được phép xem. |
| <a id="com-012"></a> COM-012 | Phòng mới mặc định mọi thành viên xem được, trừ khi giới hạn quyền. |
| <a id="com-015"></a> COM-015 | Thành viên mới được xem lịch sử cũ của phòng mình được phép xem. |
| <a id="com-034"></a> COM-034 | Tên phòng 1–100 ký tự, chủ đề tối đa 1.000. Chủ server hay người có quyền quản lý phòng tạo, sửa, xóa được. Xóa phòng thì ngừng mọi truy cập tin và media; v1 không khôi phục được. |

<a id="use-cases"></a>

<a id="use-case"></a>

## Ca sử dụng

Điều kiện, version, epoch, gửi lại và lỗi dùng chung: [quy ước tích hợp](mechanics.md#use-case-conditions). Quy tắc, AC, TC: [bảng truy vết](traceability.md#use-case-coverage).

<a id="uc-com-16"></a>

<a id="uc-com-16--tạo-phòng"></a>

### UC-COM-16 — Tạo phòng

**Phối hợp:** Identity, Messaging (không gian chat), WebClient; Media (phòng thoại, v1).

**Ai làm:** chủ server hoặc thành viên có `manage_channels`. Không cần quyền xem phòng khác.

**Khi nào:** mở COM-S04, nhập tên, chủ đề, chọn loại (chữ hay thoại).

**Các bước:**

1. Nhập dữ liệu ([COM-034](#com-034), [COM-039](servers.md#com-039)) và gửi.
2. Hệ thống kiểm tra quyền, tên không trùng trong server, loại phòng; chuẩn bị dữ liệu phòng ([D14](#v1-channel-provisioning)). Phòng mới mặc định mọi thành viên xem được.
3. **v1:** chờ trong thời gian ngắn ([D17](#v1-channel-creation-response)). Đủ dữ liệu và chốt thành công thì trả phòng, công bố sẵn sàng, chuyển người dùng tới phòng.
4. Hết thời gian mà chưa xong thì chốt hủy ([D19](#v1-channel-creation-interruption)). Chỉ báo lỗi khi việc hủy đã được lưu và đã chặn công bố muộn. Giữ dữ liệu nhập và nút Thử lại; server tự dọn dữ liệu đã tạo dở.

**Ngoại lệ:**

- Tên sai hay trùng: từ chối. Mất quyền giữa chừng: không hoàn tất.
- Không rõ kết quả (timeout, mất phản hồi): tra cứu bằng cùng khóa ([D18](#v1-channel-operation-recovery)); không tự kết luận đã hủy, không tạo khóa mới.
- Gửi lại cùng khóa và nội dung không tạo thêm phòng.
- Thử lại sau khi lần trước đã xác nhận hủy là thao tác mới ([D19](#v1-channel-creation-interruption)).
- Tạo thông tin phòng thoại không chứng minh gọi được.

**Kết quả:** một phòng đang hoạt động đúng loại. Phòng chữ dùng được cho [UC-COM-17](#uc-com-17) và [UC-COM-23](../messaging/channel-messaging.md#uc-com-23). v1 không có phòng chỉ đọc hay chat chữ trong phòng thoại.

<a id="uc-com-17"></a>

<a id="uc-com-17--xem-phòng-được-phép-và-lịch-sử-tin-văn-bản"></a>

### UC-COM-17 — Xem phòng và lịch sử tin

**Thực hiện:** Community (danh sách, thông tin phòng); Messaging (lịch sử tin, đọc qua guard của Community).

**Ai làm:** thành viên đang hoạt động (chủ server cũng phải đủ điều kiện chung).

**Khi nào:** chọn server, mở phòng, hoặc tải tin cũ ở COM-S03.

**Các bước:**

1. Community tính quyền xem và chỉ trả các phòng được xem.
2. Khi chọn phòng: kiểm tra lại quyền, trả thông tin phòng. Phòng chữ: Messaging tải lịch sử theo trang qua guard của Community.
3. Hiện tin mới nhất, dấu đã sửa, tin đã xóa. Thành viên mới thấy lịch sử cũ nếu có quyền xem. Phòng thoại chuyển sang Media.

**Ngoại lệ:**

- Không có phòng nào hay chưa có tin: hiện trạng thái trống.
- Phòng ẩn hay đã xóa: 404, kể cả khi biết ID hay có quyền quản lý.
- Tác giả cũ không còn hoạt động không làm mất trang lịch sử.
- Mất quyền xem khi đang đọc: xem [UC-COM-25](../messaging/channel-messaging.md#uc-com-25).

**Kết quả:** danh sách và nội dung đúng quyền hiện tại. Đọc lịch sử không khôi phục phòng đã xóa, không cấp quyền mới.

<a id="uc-com-18"></a>

<a id="uc-com-18--sửa-thông-tin-phòng"></a>

### UC-COM-18 — Sửa thông tin phòng

**Ai làm:** chủ server hoặc thành viên có `manage_channels` **và** quyền xem phòng đó.

**Khi nào:** sửa tên hay chủ đề trong phần quản lý phòng.

**Các bước:**

1. Sửa tên, chủ đề ([COM-034](#com-034), [COM-039](servers.md#com-039)), gửi kèm `expectedVersion`.
2. Kiểm tra quyền quản lý và quyền xem, dữ liệu, version, tên không trùng; cập nhật.
3. Trả phòng hiện tại; báo các client có quyền tải lại thông tin.

**Ngoại lệ:** phòng ẩn hay đã xóa không lộ thông tin. Thiếu `manage_channels`: từ chối. Tên trùng hay version cũ: không ghi đè, giữ form. Không đổi được loại phòng (v1). ACL dùng [UC-COM-22](access-control.md#uc-com-22).

**Kết quả:** thông tin phòng được cập nhật; lịch sử và loại phòng không đổi.

<a id="uc-com-19"></a>

<a id="uc-com-19--xóa-phòng"></a>

### UC-COM-19 — Xóa phòng

**Phối hợp:** Identity, Messaging (vòng đời, thu hồi), WebClient; Media (phòng thoại, v1).

**Ai làm:** chủ server hoặc thành viên có `manage_channels` và quyền xem phòng đó.

**Khi nào:** chọn Xóa và xác nhận.

**Các bước:**

1. Kiểm tra lại quyền xem, `manage_channels`, version.
2. Trong cùng transaction (MVP): chuyển phòng và không gian chat sang đã xóa, tăng version quyền, ghi sự kiện thu hồi. Phòng thoại phối hợp với Media.
3. Sau commit: phòng biến mất khỏi danh sách; chặn đọc, gửi, vào, vào lại; ngắt kết nối và đóng nội dung trên giao diện ([UC-COM-25](../messaging/channel-messaging.md#uc-com-25)).

**Ngoại lệ:** quyền hay version cũ, hoặc phòng đã xóa: không tạo phòng mới. Xóa tranh với gửi tin hay vào cuộc gọi: theo guard và thứ tự commit; tin đã commit trước vẫn giữ. Lỗi thì rollback tất cả. Ngắt media cần bằng chứng riêng.

**Kết quả:** phòng không truy cập được, không khôi phục được trong v1; giữ ID và nội dung (DEC-105). Không xóa server hay backup.

Transaction ở trên giả định một ứng dụng và một database. Khi tách database ở v1, giao thức xóa phòng còn phải thiết kế; D14 chỉ chốt điều kiện tạo phòng.

<a id="ux"></a>

<a id="ux-và-trạng-thái"></a>

## Giao diện

```text
COM-S03 · Màn hình rộng tham chiếu 1280 × 800
┌─────────────┬─────────────────────┬────────────────────────────────┐
│ Cộng đồng   │ Tên cộng đồng [⋯]   │ Tên phòng / chủ đề             │
│             │                     │                                │
│ • Nhóm A    │ Phòng theo chủ đề   │ Lịch sử tin                    │
│ • Nhóm B    │ # chung             │                                │
│             │ # học-tập           │ Tin đã bị xóa                  │
│ [Khám phá]  │                     │                  Tin của tôi   │
│             │ [+ Tạo phòng]*      │                  Đã sửa [⋯]   │
│             │ [Quản lý]*          ├────────────────────────────────┤
│             │                     │ [Nhập tin…              ] [Gửi]│
└─────────────┴─────────────────────┴────────────────────────────────┘
* Chỉ hiện thao tác người dùng được phép thực hiện.
```

- Danh sách chỉ có phòng được xem. Thành viên mới thấy lịch sử cũ.
- Không có chế độ chỉ đọc (đợt đầu): xem được phòng thì gửi được tin, nếu tài khoản đủ điều kiện. Giới hạn tin, sửa, xóa, lỗi, gửi lại giống [DM](../messaging/direct-messaging.md#ux).
- Các trạng thái cần thiết kế: chưa có phòng nào, phòng chưa có tin, tải lỗi, lời mời không dùng được, đang chờ duyệt, mất quyền khi đang mở, rời server. Mất quyền hay rời thì đóng nội dung phòng, tải lại danh sách theo quyền, không gửi nhận qua kết nối cũ.
- Phòng bị xóa: đóng nội dung và cuộc gọi (DEC-077).

| Màn hình | Ai dùng | Nội dung |
|---|---|---|
| COM-S04 Tạo phòng | Chủ server, người có quyền | Tên 1–100, chủ đề tối đa 1.000; mặc định mọi thành viên xem. Nút Tạo hiện "đang xử lý" ngay. v1 chờ ngắn ([D17](#v1-channel-creation-response)); chỉ thêm và chuyển tới phòng khi thành công. Đã hủy thì giữ dữ liệu và nút Thử lại; chưa rõ kết quả thì đối chiếu ([D18](#v1-channel-operation-recovery)) |

Màn hình hẹp và trạng thái chung: [giao diện chung](mechanics.md#ux).

<a id="technical"></a>

## Thiết kế dữ liệu và xử lý

<a id="channel-dữ-liệu-và-nâng-cấp"></a>

### Dữ liệu và migration 004

- `name_key`: cắt khoảng trắng, NFC, `ToLowerInvariant` (.NET), so sánh collation C; unique theo server trong các phòng đang hoạt động. Giữ cột `normalized_name` cũ nhưng bỏ unique cũ.
- Tên 1–100 UTF-16; chủ đề tối đa 1.000; chuẩn hóa xuống dòng khi ghi.
- Loại phòng (chữ, thoại) không đổi. Writer hiện chỉ tạo phòng chữ.
- Trạng thái active hay deleted, `deleted_at`, `version` và `access_version` (int32). Mỗi lần cập nhật chạy một trigger version và thời gian, kể cả ACL. `accessVersion` dùng CAS cho cấu hình quyền; `version` cho thông tin phòng.

**Chạy migration 004:**

- Cần 001, 002, 003 đúng checksum, và file mapping đầy đủ cho mọi phòng cũ (loại phòng, quyền xem mặc định). Tên và khóa xử lý theo lô 500.
- Phòng cũ chỉ đọc, không gian chat đã lưu trữ, hay đã xóa mà thiếu thời điểm: phải sửa dữ liệu có review; không tự chọn loại phòng, quyền, không đổi tên hay epoch, không xóa nội dung.
- Thứ tự: dừng writer cũ → chạy 004 → triển khai bản mới. Database mới đã gồm 001 đến 004; seed ghi rõ khóa, quyền xem mặc định, loại mặc định là chữ.

<a id="channel-api-và-quyền"></a>

### API và quyền

- MVP có GET và POST danh sách phòng, GET và PATCH một phòng (theo OpenAPI).
- POST: loại mặc định là chữ, chủ đề null. Phòng thoại bị từ chối (chưa có Media).
- PATCH: chỉ tên và chủ đề, kèm `expectedVersion`; chủ đề null để xóa.
- ACL có API riêng ([cấu hình ACL](access-control.md#acl-api)). Request giới hạn kích thước; không tự gửi lại thao tác ghi.
- v1: chờ ngắn, hủy, bù ([D17, D19](#v1-channel-creation-response)); tra cứu ([D18](#v1-channel-operation-recovery)). Hợp đồng chi tiết chưa có.

**Guard quản lý:** lease Identity → server (chia sẻ khi đọc, cập nhật khi ghi) → phòng. Mọi route một phòng kiểm tra quyền xem **trước** quyền quản lý; phòng ẩn, đã xóa hay thuộc server khác trả 404, kể cả người có `manage_channels` hay `manage_channel_access`. Cần phiên hợp lệ, tài khoản hoạt động và đã xác minh, server và tư cách hợp lệ. Quyền xem tính theo [chính sách chuẩn](access-control.md#view-permissions).

**Danh sách:** lọc theo cùng chính sách trong SQL trước LIMIT; cursor gắn người dùng, server, giới hạn, mục đích, hạn 24 giờ; không hứa bản chụp cố định.

**Tạo phòng (MVP):** ghi không gian chat Messaging qua `IChatSpaceLifecycle` trên cùng kết nối và transaction; Community chỉ ghi schema của mình. Đọc và ghi tin phòng phải dùng [guard phòng](mechanics.md#transactions); chưa có. Writer và Hub của DM có guard riêng, không cấp quyền phòng.

**Sự kiện (MVP):** tạo phòng và sửa thông tin tăng `accessVersion` của server và ghi outbox (chỉ ID, lý do, version) cùng transaction; không phát tên, ACL hay danh sách thành viên. Sửa thông tin tăng version phòng; sửa mà không đổi gì thì không tăng. Version và sự kiện khi đổi ACL: [quyền](access-control.md#acl-api).

---

## Thiết kế v1: tạo phòng khi tách service

Khi mỗi service có database riêng, không thể tạo phòng và không gian chat trong một transaction. Sáu quyết định dưới đây xử lý việc đó.

<a id="v1-channel-provisioning"></a>

### D14 — Chỉ báo xong khi dữ liệu nền đã sẵn

**Quyết định:** chỉ báo tạo phòng thành công và công bố phòng khi dữ liệu nền đúng loại phòng đã được chuẩn bị ([D02](../../system/database.md#v1-database-layout)).

| Loại phòng | Cần chuẩn bị |
|---|---|
| Chữ | Thông tin phòng ở Community và không gian chat ở Messaging |
| Thoại | Thông tin phòng ở Community và bản ghi phòng trống ở Media ([phòng thoại](../media/voice-rooms.md#contracts)) |

Phòng thoại chưa tạo phòng trên LiveKit, chưa giữ chỗ, chưa cấp token ở bước này; không tạo không gian chat cho phòng thoại.

**Lý do:** "đã tạo xong" nghĩa là dữ liệu nền đã đủ. **Đổi lại:** tạo phòng phụ thuộc Messaging hoặc Media phải đáp ứng. Chuẩn bị xong không đảm bảo service luôn sẵn sàng về sau.

- MVP hiện tạo phòng, không gian chat, bản ghi thao tác, outbox trong một transaction ([ChannelService](../../../services/Modules/Community/Features/Channels/Application/ChannelService.cs) truyền transaction cho [IChatSpaceLifecycle](../../../services/SCDC.Contracts/Messaging/IChatSpaceLifecycle.cs)). v1 dùng transaction riêng từng service, Community điều phối.
- Không có rollback chung. Một service có thể đã commit khi service kia chưa xong; đối chiếu và xử lý theo quyết định đã lưu. Không công bố phòng từ kết quả một phần. Tham khảo [Saga](https://learn.microsoft.com/en-us/azure/architecture/patterns/saga).
- Quyền phải giữ tới bước hoàn tất ([D05](../../system/access-revocation.md#v1-authorization-coordination)). Kết quả chuẩn bị đến muộn không tự công bố nếu quyền hay tài nguyên đã mất hiệu lực.

<a id="v1-channel-creation-coordination"></a>

### D15 — Community điều phối

**Quyết định:** Community lưu tiến độ tạo phòng trong DB của mình, yêu cầu Messaging hoặc Media chuẩn bị, ghi nhận kết quả và điều phối phục hồi.

| Việc | Ai làm |
|---|---|
| Tiến độ, bước cần làm, kết quả đã nhận, phục hồi | Community (lưu trong DB Community) |
| Chuẩn bị dữ liệu chat hay phòng thoại | Messaging hay Media, tự ghi DB của mình |
| Hoàn tất và công bố phòng | Community, khi đủ dữ liệu và quyền (D14) |

**Lý do:** một nơi theo dõi toàn bộ thao tác. **Đổi lại:** Community phải có logic điều phối và phục hồi; Community ngừng thì chưa chốt được dù service khác đã xong. Worker chạy cùng API Community ([D12](../../system/backend.md#v1-application-workers)).

**Ví dụ:** Messaging đã tạo không gian chat, Community chưa kịp ghi nhận thì khởi động lại. Community đọc tiến độ đã lưu để đối chiếu và xử lý. Nếu đã hủy, worker tiếp tục dọn và chặn công bố muộn. Timeout không chứng minh bên kia chưa commit.

Community không đọc hay ghi DB của Messaging, Media. Sự kiện sau commit theo [D27](../../system/api-conventions.md#v1-integration-event-transport).

<a id="v1-channel-preparation-transport"></a>

### D16 — Gọi gRPC trực tiếp để chuẩn bị

**Quyết định:** Community gọi gRPC trực tiếp tới Messaging hoặc Media để chuẩn bị dữ liệu và nhận kết quả trong lần gọi. Kết quả chưa rõ thì đối chiếu và thử lại từ tiến độ đã lưu.

| Bước | Ai làm |
|---|---|
| Gọi chuẩn bị | Community, qua gRPC (D04) |
| Trả kết quả | Messaging hay Media, **sau khi** đã commit dữ liệu |
| Ghi nhận và hoàn tất | Community; chỉ công bố khi đủ điều kiện D14 và quyền hợp lệ |

**Lý do:** dùng giao thức nội bộ đã chọn, không thêm kênh riêng. **Đổi lại:** mỗi lần gọi cần bên kia trả lời. Messaging ngừng thì tạo phòng chữ chưa xong; Community thử trong thời gian chờ rồi hủy và dọn (D19).

- Nhận được RPC hay bắt đầu xử lý chưa phải "đã chuẩn bị".
- Timeout có thể xảy ra sau khi bên kia đã commit ([gRPC status codes](https://grpc.io/docs/guides/status-codes/)). Phục hồi phải nhận ra cùng thao tác, không tạo trùng; timeout không tự rollback.
- Danh tính service qua mTLS ([D20](../../system/api-conventions.md#v1-service-authentication)). Sự kiện D27 không thay điều kiện "đã chuẩn bị".

<a id="v1-channel-creation-response"></a>

### D17 — Phản hồi tạo phòng

**Trạng thái:** DEC-135 ban đầu (cho phép trả 202 và tạo lâu dài) đã được thay một phần bởi D19 (DEC-137). Hiện hành: chờ ngắn; trả thành công khi đã xong, hoặc lỗi khi việc hủy đã được xác nhận. Phòng đang chuẩn bị không hiện trong danh sách.

| Tình huống | Phản hồi | Giao diện |
|---|---|---|
| Thao tác mới đã xong | `201` và phòng | Báo thành công, cập nhật danh sách, chuyển tới phòng |
| Gửi lại thao tác đã xong | `200` và phòng hiện tại (sau kiểm tra quyền) | Dùng kết quả cũ |
| Hết giờ, hủy đã lưu và đã chặn công bố muộn | Lỗi tạo (mã cụ thể chưa chốt) | Giữ dữ liệu nhập, nút Thử lại |
| Mất phản hồi hoặc chưa xác nhận được xong hay hủy | Chưa rõ; đối chiếu bằng GET (D18) | Báo chưa rõ kết quả; Thử lại thì đối chiếu thao tác cũ trước |

- Nút Tạo hiện "đang xử lý" ngay.
- Chỉ báo lỗi khi server đã xác nhận hủy; timeout ở trình duyệt hay RPC không đủ để kết luận.
- Không tự POST lại. Chưa rõ thì dùng cùng `clientOperationId` và nội dung, rồi GET (D18). Đóng màn hình không thay quyết định ở server.
- MVP hiện trả `201` hoặc `200` kèm Channel DTO trong transaction chung.

<a id="v1-channel-operation-recovery"></a>

### D18 — Tra cứu khi mất phản hồi

**Quyết định:** khi mất phản hồi hay tải lại trang mà client còn giữ khóa thao tác, WebClient GET tiến trình hay kết quả bằng `clientOperationId`. Không cần đã nhận được ID từ server.

| Thành phần | Quy tắc |
|---|---|
| Người thực hiện | Lấy từ phiên hiện tại; kiểm tra quyền trước khi trả |
| Loại thao tác | `create_channel` (khác với loại phòng chữ hay thoại) |
| Phạm vi và khóa | Server và `clientOperationId` đã lưu trước khi POST |
| Kết quả | Tiến trình, trạng thái hoặc tham chiếu kết quả, theo quyền hiện tại |

**Lý do:** theo dõi lại bằng một thao tác chỉ đọc. **Đổi lại:** thêm hợp đồng tra cứu và kiểm tra quyền. Tham khảo [Asynchronous request-reply](https://learn.microsoft.com/en-us/azure/architecture/patterns/asynchronous-request-reply).

- GET chỉ đọc; không tạo, không tiếp tục, không kích hoạt xử lý. Worker của Community chạy độc lập.
- Gửi lại POST thủ công vẫn dùng nguyên khóa và nội dung, có kiểm tra fingerprint. Không tạo khóa mới chỉ vì mất phản hồi.
- Không tìm thấy bản ghi không chứng minh request chưa tới: có thể đang trên đường, hoặc đã quá hạn lưu.
- MVP hiện giữ khóa và nội dung trong `sessionStorage` theo người dùng và server để gửi lại thủ công; tra cứu v1 chưa làm.

<a id="v1-channel-creation-interruption"></a>

### D19 — Hủy và dọn dữ liệu

**Quyết định:** chờ ngắn; lỗi rõ ràng kèm nút Thử lại. Dữ liệu đã commit được dọn nền sau khi server đã chắc chắn chặn việc công bố muộn. Thay phần 202 của D17; giữ D14, D15, D16, D18.

| Bước | Quy tắc |
|---|---|
| Chuẩn bị trong thời gian chờ | Chỉ hoàn tất khi đủ dữ liệu và quyền; dữ liệu đang chuẩn bị không hiện, không dùng như phòng sẵn sàng |
| Chốt thành công hay hủy | Loại trừ nhau trên cùng thao tác; đã chốt thành công thì trả thành công |
| Xác nhận hủy | Lưu quyết định hủy và thông tin phục hồi; chỉ báo lỗi khi đã chặn công bố muộn |
| Dọn nền | Community điều phối theo tiến độ đã lưu; mỗi service dọn dữ liệu của mình; đối chiếu và thử lại khi gián đoạn |

- Sau khi đã hủy, kết quả hay lệnh đến muộn không làm thao tác cũ thành công. Service phải xử lý cả trường hợp chuẩn bị đến muộn sau khi đã dọn; chỉ xóa dữ liệu rồi quên thao tác là chưa đủ.
- Dữ liệu đang chuẩn bị phải bị chặn trên mọi đường đọc, ghi, phát sự kiện trước khi phòng sẵn sàng.
- "Đã xác nhận hủy" và "đã dọn xong mọi nơi" là hai mốc khác nhau. Dọn cũng có thể lỗi; cần chống trùng và lưu tiến độ để thử lại ([Compensating Transaction](https://learn.microsoft.com/en-us/azure/architecture/patterns/compensating-transaction)).
- Thời gian chờ không đảm bảo luôn có kết quả rõ; chưa rõ thì dùng D18.
- Không hoàn tất bằng phiên hay quyền đã mất (D05).

**Nút Thử lại:**

| Kết quả lần trước | Thử lại làm gì |
|---|---|
| Chưa rõ | GET đối chiếu; gửi lại thủ công giữ nguyên khóa và nội dung |
| Đã thành công | Đọc phòng hiện tại |
| Đã xác nhận hủy | Tạo thao tác mới với khóa mới (thao tác đã kết thúc không tạo lại bằng khóa cũ, xem [chống trùng](mechanics.md#operations)) |

Phải chặn thao tác cũ sống lại, và chỉ dọn đúng dữ liệu của nó, không ảnh hưởng lần tạo mới. Trình duyệt không tự POST lại.

<a id="channel-webclient-và-bằng-chứng"></a>

### Giao diện và bằng chứng (MVP)

- Thành viên vào danh sách phòng theo quyền.
- Tạo: lưu thao tác và nội dung theo người dùng và server trước khi POST; gửi lại thủ công nguyên nội dung và khóa sau khi mất phản hồi hay tải lại.
- Sửa thông tin: giữ version đã đọc; xung đột hay timeout thì GET đối chiếu trước khi lưu lại.
- Đổi người dùng, server hay phòng đang chọn thì hủy các lần đọc cũ và bỏ dữ liệu quản lý; 401, 403, 404 thì đóng thông tin cũ.
- Không hiện tin hay ô soạn tin khi chưa có API tin.
- Chờ ngắn, hủy, dọn (D17, D19) và tra cứu (D18) của v1 chưa làm.

Giao diện sửa quyền: [trình sửa ACL](access-control.md#acl-editor-ui). Server luôn kiểm tra lại quyền dưới khóa.

Bằng chứng API, DB, transaction chung và Chromium: [hồ sơ](../../records/verification/community/channels-access.md). HTTP guard hay outbox không chứng minh thu hồi realtime hay Media.

### Xóa phòng và phòng thoại (giả định một ứng dụng)

<a id="delete-giao-dịch-của-thành-phần"></a>

#### Transaction

- **Xóa phòng:** Community chuyển sang đã xóa, tăng version và `accessVersion`; Messaging đánh dấu không gian chat đã xóa qua hợp đồng vòng đời; outbox thu hồi; tất cả cùng transaction. Không được có tình huống phòng đã xóa vẫn nhận tin. Không xóa tin hay backup ở đây.
- **Phòng thoại:** cần Media. Đề xuất `IMediaRoomLifecycle` tạo bản ghi phòng trống cùng transaction và đánh dấu đang đóng khi xóa (giả định một ứng dụng); chưa có interface hay migration. Tạo phòng thoại v1 theo [D14](#v1-channel-provisioning). LiveKit theo [Media](../media/participation-lifecycle.md#provider); ngắt (DEC-099) cần bằng chứng riêng. Migration giữ loại "thoại" của phòng cũ, nhưng writer hiện chỉ tạo phòng chữ.

<a id="acceptance"></a>

<a id="tiêu-chí-chấp-nhận"></a>

## Tiêu chí chấp nhận

| Mã | Tình huống | Kết quả mong đợi |
|---|---|---|
| <a id="ac-com-07"></a> AC-COM-07 | Thành viên không có quyền thử tạo phòng | Bị từ chối; chủ server hay người có quyền thì tạo được |
| <a id="ac-com-14"></a> AC-COM-14 | Có tin trong phòng khi thành viên vắng mặt; sau đó mở lại, vẫn có quyền xem | Thấy tin trong lịch sử |
| <a id="ac-com-15"></a> AC-COM-15 | Thành viên mới mở phòng có tin cũ, được phép xem | Thấy lịch sử cũ |
| <a id="ac-com-34"></a> AC-COM-34 | Người đúng và sai quyền sửa, xóa phòng đang có tin và cuộc gọi | Phòng đã xóa không đọc, gửi, nhận, gọi tiếp được; chặn tranh chấp với writer; v1 không khôi phục |

Tiêu chí liên quan nhiều phần: [bảng truy vết](traceability.md#use-case-coverage).

<a id="tests"></a>

<a id="ca-kiểm-thử"></a>

## Ca kiểm thử

Dữ liệu và cách ghi bằng chứng: [dùng chung](../messaging/channel-messaging.md#evidence). Mỗi ca cần test cho từng endpoint và kiểm tra quyền qua API.

| Mã ca | Thao tác | Cần quan sát | Cho |
|---|---|---|---|
| <a id="tc-com-09"></a> TC-COM-09 | Thành viên mới, hoặc vừa mất mạng, mở lại phòng có tin cũ | Lịch sử đầy đủ nếu còn quyền xem | [AC-COM-14](#ac-com-14), [15](#ac-com-15) |
| <a id="tc-com-15"></a> TC-COM-15 | Xóa phòng cùng lúc với gửi tin hay vào cuộc gọi; thử ID phòng cũ sau khi xóa | Không khôi phục phòng; không đọc, nhận, gọi sau thu hồi; tin đã commit vẫn giữ | [AC-COM-34](#ac-com-34) |

<a id="gaps"></a>

<a id="việc-còn-lại"></a>

## Còn mở

Phụ thuộc chung: [kế hoạch](../../project/planning.md#community-work-items), [migration](mechanics.md#schema-migration), [vòng đời dữ liệu](../../system/data-lifecycle.md). Đã đạt và chưa đạt xem [hồ sơ kiểm thử](../../records/verification/community/README.md).

- Xóa phòng nguyên tử và tranh chấp gửi với xóa; lịch sử và ghi tin phòng dùng guard; Hub và thu hồi.
- Phòng thoại phụ thuộc Media và bằng chứng ngắt riêng.
- **v1 tạo phòng (D14 đến D19):** schema và ID tham chiếu; giao thức hủy và dọn; dấu hủy và thời hạn lưu; trả lại tên và hạn mức; thời gian chờ; mã lỗi; URL và schema tra cứu, quyền đọc, hành vi khi mất khóa; file `.proto`, quyền RPC, thời hạn và thử lại; tranh chấp với mất quyền hay xóa phòng; giao thức xóa phòng giữa service.
- Kiểm thử v1 phải gồm: thành công tranh với hủy; mất phản hồi sau commit; lệnh đến muộn sau khi đã dọn; khởi động lại giữa lúc dọn; Thử lại sau khi hủy.

---

Nguồn: DEC-026 (COM-007), DEC-027 (COM-008), DEC-033 (COM-012), DEC-038 (COM-015), DEC-077 (COM-034), DEC-099, DEC-105, [DEC-132](../../records/decisions/README.md#dec-132) (D14), [DEC-133](../../records/decisions/README.md#dec-133) (D15), [DEC-134](../../records/decisions/README.md#dec-134) (D16), [DEC-135](../../records/decisions/README.md#dec-135) (D17), [DEC-136](../../records/decisions/README.md#dec-136) (D18), [DEC-137](../../records/decisions/README.md#dec-137) (D19).
