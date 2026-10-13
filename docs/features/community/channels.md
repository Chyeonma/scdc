# Phòng và vòng đời phòng

Community / Channels sở hữu metadata, kind, danh sách theo quyền và vòng đời phòng. Messaging sở hữu chat space và nội dung tin. Quy tắc, luồng sử dụng, UX, thiết kế dữ liệu/API và tiêu chí kiểm chứng của chủ đề được quản lý tại trang này.

<a id="implementation"></a>

## Trạng thái theo khả năng

Đối chiếu hiện trạng ngày 2026-10-08. Nền Community đã hợp nhất vào `main`; bằng chứng dẫn dưới đây gắn revision cụ thể, không phải kết quả chạy lại cho mọi thay đổi sau đó.

| Khả năng | Trạng thái và giới hạn | Bằng chứng |
|---|---|---|
| Tạo text — phần UC-COM-16 | Backend/WebClient đã kiểm chứng; space/channel/operation/outbox nguyên tử | [Phòng text/ACL](../../records/verification/community/channels-access.md) |
| List/detail — phần UC-COM-17; sửa metadata — UC-COM-18 | Đã kiểm chứng; UC-COM-17 chưa có lịch sử tin | [Phòng text/ACL](../../records/verification/community/channels-access.md) |
| Cấu hình ACL HTTP — phần UC-COM-22 | Đã kiểm chứng; thuật toán, snapshot và quyền quản lý tại [Access control](access-control.md) | [Phòng text/ACL](../../records/verification/community/channels-access.md) |
| Xóa phòng — UC-COM-19; voice/Media | Thiết kế mục tiêu; chưa triển khai lifecycle/runtime | [Kế hoạch](../../project/planning.md#community-work-items) |
| Lịch sử, gửi/nhận, Hub và mất quyền | Chưa tích hợp; Community MVP chưa đạt | [Tin phòng](../messaging/channel-messaging.md) |

Quy tắc view/ACL chỉ định nghĩa tại [Access control](access-control.md#view-permissions); giao tiếp qua [Messaging](../messaging/channel-messaging.md).

## Mục lục

- [Quy tắc](#requirements)
- [Use case](#use-cases)
- [UX và trạng thái](#ux)
- [Thiết kế dữ liệu và xử lý](#technical)
- [D14 — Điều kiện hoàn tất tạo phòng v1](#v1-channel-provisioning)
- [D15 — Điều phối tạo phòng v1](#v1-channel-creation-coordination)
- [D16 — Truyền lệnh chuẩn bị phòng v1](#v1-channel-preparation-transport)
- [D17 — Phản hồi tạo phòng v1](#v1-channel-creation-response)
- [D18 — Tìm lại tiến trình khi mất phản hồi](#v1-channel-operation-recovery)
- [D19 — Hủy tạo phòng và dọn dữ liệu](#v1-channel-creation-interruption)
- [Tiêu chí chấp nhận](#acceptance)
- [Ca kiểm thử](#tests)
- [Việc còn lại](#gaps)

[Điều kiện chung và cơ chế Community](../../system/community.md#use-case-conditions) · [Hướng dẫn chạy và kiểm thử](../../guides/community-development.md) · [Mục lục Community](README.md).

<a id="requirements"></a>

<a id="phạm-vi-và-quy-tắc"></a>

## Phạm vi và quy tắc

| Mã | Quy tắc | Căn cứ |
|---|---|---|
| <a id="com-007"></a> COM-007 | Chỉ chủ sở hữu hoặc người được cấp quyền có thể tạo phòng theo chủ đề. | DEC-026 |
| <a id="com-008"></a> COM-008 | Thành viên chỉ thấy phòng mình được cấp quyền xem. | DEC-027 |
| <a id="com-012"></a> COM-012 | Phòng mới mặc định cho mọi thành viên xem được, trừ khi giới hạn quyền. | DEC-033 |
| <a id="com-015"></a> COM-015 | Thành viên mới vào cộng đồng được xem lịch sử cũ của phòng mình được phép xem. | DEC-038 |
| <a id="com-034"></a> COM-034 | Tên phòng 1–100, chủ đề tối đa 1.000; owner/người có quyền quản lý phòng tạo/sửa/xóa; xóa ngừng truy cập tin/media, chưa khôi phục trong v1. | DEC-077 |

<a id="use-cases"></a>

<a id="use-case"></a>

## Use case

Điều kiện, version/epoch, retry và lỗi dùng chung theo [quy ước tích hợp](../../system/community.md#use-case-conditions). Quy tắc/AC/TC áp dụng cho từng UC ở [bảng truy vết](traceability.md#use-case-coverage).

<a id="uc-com-16"></a>

<a id="uc-com-16--tạo-phòng"></a>

### UC-COM-16 — Tạo phòng

**Module phụ trách:** Community / Channels.

**Phối hợp:** Identity, Messaging (lifecycle chat space), WebClient; Media cho phòng voice ở v1.

**Tác nhân:** Owner hoặc thành viên có manage_channels.

**Điều kiện trước:** Server/membership/phiên hợp lệ; tạo mới chỉ cần manage_channels, không đòi xem một phòng khác.

**Kích hoạt:** Actor mở COM-S04, nhập tên/chủ đề và chọn kind text/voice.

**Luồng chính:**

1. Actor nhập dữ liệu theo [COM-034](#com-034), [COM-039](servers.md#com-039) và gửi thao tác tạo.
2. Hệ thống kiểm tra quyền, tên unique trong server và kind; chuẩn bị dữ liệu phòng theo [D14](#v1-channel-provisioning). Phòng mới mặc định mọi thành viên view.
3. Với v1, xử lý trong khoảng chờ ngắn theo [D17](#v1-channel-creation-response). Đủ dữ liệu nền và chốt thành công thì trả phòng, công bố sẵn sàng và chuyển tới phòng theo quyền hiện hành.
4. Chưa hoàn tất khi hết hạn thì chốt hủy theo [D19](#v1-channel-creation-interruption). Chỉ báo lỗi tạo xác định sau khi hủy được lưu bền và chặn công bố muộn; giữ dữ liệu nhập và nút Thử lại. Server bù/dọn dữ liệu nền.

**Ngoại lệ:** Tên sai/trùng bị từ chối; actor mất quyền không được hoàn tất tạo phòng. Kết quả chưa rõ do timeout/mất phản hồi thì tra cứu cùng khóa theo [D18](#v1-channel-operation-recovery); không tự kết luận đã hủy hoặc tạo khóa mới. Replay cùng khóa/payload không tạo thêm space/phòng. Thử lại chủ động sau khi lần cũ đã xác nhận hủy là thao tác mới theo [D19](#v1-channel-creation-interruption). Dữ liệu chuẩn bị một phần được đối soát/bù theo quyết định đã lưu. Tạo metadata voice chưa chứng minh gọi/media hoạt động.

**Kết quả sau cùng khi tạo thành công:** Có một phòng active đúng kind; text có thể dùng [UC-COM-17](#uc-com-17), [UC-COM-23](../messaging/channel-messaging.md#uc-com-23). Phòng read-only và text bên trong voice nằm ngoài hợp đồng v1.

<a id="uc-com-17"></a>

<a id="uc-com-17--xem-phòng-được-phép-và-lịch-sử-tin-văn-bản"></a>

### UC-COM-17 — Xem phòng được phép và lịch sử tin văn bản

**Module phụ trách:** Community / Channels (danh sách, metadata); Messaging (lịch sử tin).

**Phối hợp:** Identity, WebClient; Messaging đọc lịch sử qua guard quyền của Community.

**Tác nhân:** Thành viên active; owner vẫn phải thỏa điều kiện nền.

**Điều kiện trước:** Actor/server/membership hợp lệ; phòng còn active và actor có view tại mỗi lần đọc.

**Kích hoạt:** Thành viên chọn server, mở phòng hoặc tải lịch sử cũ tại COM-S03.

**Luồng chính:**

1. Community tính quyền theo thuật toán view và chỉ trả các phòng được xem trên từng trang.
2. Khi người dùng chọn phòng, kiểm tra lại quyền rồi trả metadata. Với text, Messaging tải lịch sử có phân trang qua guard Community.
3. Giao diện hiển thị tin hiện hành, dấu đã sửa/tombstone; thành viên mới được xem lịch sử cũ khi còn view. Voice chuyển sang hành trình Media.

**Ngoại lệ:** Không có phòng được xem hoặc chưa có tin hiển thị trạng thái rỗng. Hidden/deleted channel trả 404 kể cả biết ID hoặc có management permission; owner không vượt điều kiện nền. Tác giả cũ inactive không làm mất trang lịch sử của người đọc đủ quyền, projection theo thiết kế vòng đời dữ liệu. Mất view khi đang đọc chuyển [UC-COM-25](../messaging/channel-messaging.md#uc-com-25).

**Kết quả sau cùng:** Có danh sách/nội dung đúng quyền hiện hành; đọc lịch sử không phục hồi phòng đã deleted và không cấp quyền mới.

<a id="uc-com-18"></a>

<a id="uc-com-18--sửa-thông-tin-phòng"></a>

### UC-COM-18 — Sửa thông tin phòng

**Module phụ trách:** Community / Channels.

**Phối hợp:** Identity (tài khoản/phiên), WebClient (giao diện).

**Tác nhân:** Owner hoặc thành viên có manage_channels và view phòng đích.

**Điều kiện trước:** Actor/server/membership/phòng hợp lệ; đã tải metadata và version.

**Kích hoạt:** Actor sửa tên/chủ đề từ phần quản lý phòng.

**Luồng chính:**

1. Actor chỉnh tên/chủ đề theo [COM-034](#com-034), [COM-039](servers.md#com-039) và gửi expectedVersion.
2. Hệ thống kiểm tra quyền quản lý cùng view, dữ liệu/version và tên unique rồi cập nhật metadata.
3. Sau commit, trả phòng hiện hành và thông báo tải lại thông tin cho các client đủ quyền.

**Ngoại lệ:** Hidden/deleted channel không lộ metadata; thiếu manage_channels trong phòng được biết từ chối. Tên trùng/version cũ không ghi đè, lỗi giữ form để người dùng sửa. Kind không thay đổi qua PATCH trong v1; ACL dùng [UC-COM-22](access-control.md#uc-com-22).

**Kết quả sau cùng:** Metadata phòng được cập nhật; lịch sử/kind không bị đổi bởi thao tác sửa tên/chủ đề.

<a id="uc-com-19"></a>

<a id="uc-com-19--xóa-phòng"></a>

### UC-COM-19 — Xóa phòng

**Module phụ trách:** Community / Channels.

**Phối hợp:** Identity, Messaging (lifecycle/thu hồi), WebClient; Media cho phòng voice ở v1.

**Tác nhân:** Owner hoặc thành viên có manage_channels và view phòng đích.

**Điều kiện trước:** Actor/server/membership/phòng hợp lệ; đã tải version và được biết phòng.

**Kích hoạt:** Actor chọn xóa và xác nhận phòng cần xóa.

**Luồng chính:**

1. Hệ thống kiểm tra lại view/manage_channels/version.
2. Chuyển channel và Messaging space sang deleted, tăng version quyền và ghi sự kiện thu hồi cùng transaction qua hợp đồng lifecycle; voice phối hợp lifecycle Media theo thiết kế.
3. Sau commit, phòng biến mất khỏi danh sách, chặn đọc/gửi/join/resume; thu hồi kết nối và giao diện đóng nội dung theo [UC-COM-25](../messaging/channel-messaging.md#uc-com-25).

**Ngoại lệ:** Quyền/version cũ hoặc deleted không biến thành phòng mới. Delete tranh send/media admission tuân guard và thứ tự commit; nội dung đã commit trước vẫn giữ theo retention. Lỗi lifecycle rollback toàn bộ; proof cutoff media cần thực hiện riêng.

**Kết quả sau cùng:** Phòng không truy cập/khôi phục trong v1, giữ ID và nội dung theo DEC-105. Thao tác này không xóa toàn server hoặc purge backup.

Mô tả transaction chung/rollback của luồng xóa trên là giả định một host/database. Giao thức xóa khi tách database v1 còn cần thiết kế; D14 chỉ chốt điều kiện hoàn tất tạo phòng.

<a id="ux"></a>

<a id="ux-và-trạng-thái"></a>

## UX và trạng thái

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

Danh sách chỉ chứa phòng được phép xem. Thành viên mới được xem lịch sử
cũ của phòng đó. Không có cấu hình chỉ đọc trong đợt đầu; người xem được
phòng thì gửi được sau khi thỏa điều kiện tài khoản. Giới hạn tin,
sửa/xóa, lỗi và chủ động thử lại giống [wireframe DM](../messaging/direct-messaging.md#ux).

Trạng thái riêng: chưa có phòng được xem, phòng chưa có tin, tải lỗi,
lời mời không dùng được, yêu cầu chờ duyệt, mất quyền khi đang mở và
rời cộng đồng. Sau mất quyền/rời, đóng vùng nội dung phòng và tải lại
danh sách theo quyền; không tiếp tục gửi hoặc nhận tin qua kết nối cũ.

Phòng bị xóa đóng vùng nội dung và cuộc gọi theo DEC-077.

| Màn hình | Người thực hiện | Nội dung và trạng thái cần thiết |
|---|---|---|
| COM-S04 Tạo phòng | Chủ sở hữu/người có quyền tạo phòng | Tên 1–100/chủ đề tối đa 1.000; mặc định mọi thành viên xem. Nút Tạo phản hồi đang xử lý ngay; v1 chờ ngắn theo [D17](#v1-channel-creation-response), chỉ thêm/chuyển tới phòng sau thành công. Hủy xác nhận thì giữ dữ liệu và nút Thử lại; kết quả chưa rõ dùng [D18](#v1-channel-operation-recovery) để đối soát |

Phạm vi màn hình hẹp/trình duyệt và trạng thái chung theo [tích hợp UX](../../system/community.md#ux).


<a id="technical"></a>

## Thiết kế dữ liệu và xử lý

<a id="channel-dữ-liệu-và-nâng-cấp"></a>

### Dữ liệu và nâng cấp

004 thêm name_key .NET trim/NFC/ToLowerInvariant so sánh C, unique partial theo server với channel active/nondeleted. Tên 1–100 UTF-16, topic tối đa 1.000, CRLF chuẩn hóa khi writer; giữ normalized_name legacy nhưng bỏ unique cũ. Kind text/voice bất biến; writer mới chỉ tạo text. Status active/deleted, deleted_at, version/access_version int32; một trigger version/timestamp cho mỗi update, kể cả ACL. AccessVersion dùng CAS cấu hình, metadata dùng version tổng thể của resource.

Runner yêu cầu 001/002/003 đúng checksum, mapping JSON đầy đủ cho channel legacy với kind/defaultView; tên/key theo batch 500. Legacy read-only, Messaging space archived hoặc deleted thiếu timestamp yêu cầu repair có review; không tự chọn kind/quyền, đổi tên/epoch hoặc purge nội dung. Space active/deleted giữ trạng thái tương ứng cho channel. Drain writer cũ → runner 004 → deploy mới. Bootstrap mới gồm 001–004; seed ghi key/defaultView explicit và kind mặc định text.

<a id="channel-api-và-quyền"></a>

### API và quyền

Nền MVP có GET/POST channels và GET/PATCH channel theo OpenAPI. POST default kind text/topic null, voice chưa có lifecycle Media nên bị từ chối. v1 chờ ngắn/hủy/bù theo [D17/D19](#v1-channel-creation-response), lookup theo [D18](#v1-channel-operation-recovery); hợp đồng chi tiết chưa có. PATCH chỉ name/topic + expectedVersion; null topic để xóa. [Cấu hình ACL](access-control.md#acl-api) có API, snapshot và CAS riêng. Request được giới hạn kích thước, không tự replay mutation.

Management guard giữ Identity lease → server share/read hoặc update/mutation → channel share/update. Mọi single-channel route kiểm tra view trước quyền quản lý; phòng bị ẩn, đã xóa hoặc thuộc server khác trả 404, kể cả actor có manage_channels/manage_channel_access. Foundation phải valid session/active verified account/server/member. Tính view theo [policy chuẩn](access-control.md#view-permissions). Danh sách dùng cùng policy trong SQL trước LIMIT; cursor actor/server/limit/purpose/24 giờ, không hứa snapshot bất biến.

Trong MVP, create ghi Messaging space qua IChatSpaceLifecycle trên cùng connection/transaction; Community chỉ ghi community schema. Reader/writer tin phải dùng [channel admission guard](../../system/community.md#transactions); chưa có lịch sử/send/subscription writer tiêu thụ hợp đồng này.

Trong MVP, create/metadata mutation tăng server accessVersion và ghi event outbox IDs/cause/version cùng transaction, không broadcast tên/ACL/member list. Metadata bump channel version; metadata no-op giữ version/event. Version và sự kiện khi đổi ACL hoặc dọn override được định nghĩa tại [Access control](access-control.md#acl-api).

<a id="v1-channel-provisioning"></a>

### D14 — Điều kiện hoàn tất tạo phòng v1

Trạng thái: đã xác nhận phương án A ngày 2026-10-11 — [DEC-132](../../records/decisions/README.md#dec-132). v1 chỉ báo tạo phòng hoàn tất và công bố phòng sẵn sàng sau khi dữ liệu nền đúng loại phòng đã được chuẩn bị. Phạm vi là điều kiện hoàn tất khi các service có database riêng theo [D02](../../system/database.md#v1-database-layout).

| Loại phòng | Dữ liệu cần chuẩn bị |
|---|---|
| Text | Metadata tại Community và chat space tại Messaging |
| Voice | Metadata tại Community và room DB rỗng tại Media theo [phòng thoại](../media/voice-rooms.md#contracts) |

Phòng voice chưa provision SFU, giữ quota hoặc cấp token ở bước tạo dữ liệu nền; không tạo text space trong voice. Lựa chọn giữ điều kiện “đã tạo xong” gắn với dữ liệu nền đầy đủ. Đánh đổi là hoàn tất tạo phụ thuộc Messaging hoặc Media đáp ứng; chuẩn bị xong không bảo đảm service luôn sẵn sàng ở lần sử dụng sau.

Nền MVP hiện tạo text channel, Messaging space, operation và outbox trong một transaction; [ChannelService](../../../services/Modules/Community/Features/Channels/Application/ChannelService.cs) truyền transaction sang [IChatSpaceLifecycle](../../../services/SCDC.Contracts/Messaging/IChatSpaceLifecycle.cs). Quy tắc này giữ cho MVP; v1 dùng các transaction cục bộ, Community điều phối theo [D15](#v1-channel-creation-coordination). Hợp đồng bước xử lý và phục hồi chi tiết cần thiết kế tiếp.

Không có rollback chung tự động giữa các database. Bản ghi chuẩn bị có thể đã commit tại một owner trong khi owner khác chưa hoàn tất; đối soát và xử lý theo quyết định đã lưu. Sau khi hủy theo [D19](#v1-channel-creation-interruption), server bù/dọn dữ liệu và chặn công bố muộn. Không công bố phòng sẵn sàng từ kết quả một phần. Tham khảo transaction cục bộ và bù tại [Microsoft — Saga](https://learn.microsoft.com/en-us/azure/architecture/patterns/saga).

Quyền phải được bảo vệ tới bước hoàn tất theo [D05](../../system/access-revocation.md#v1-authorization-coordination); thao tác thử lại vẫn giữ khóa/payload và chống trùng. Kết quả chuẩn bị đến muộn không được tự công bố phòng nếu quyền hoặc tài nguyên đã bị vô hiệu.

Mô hình phản hồi tại [D17](#v1-channel-creation-response) đã được điều chỉnh bởi [D19](#v1-channel-creation-interruption): chờ ngắn, thành công hoặc lỗi sau hủy được xác nhận, bù/dọn nền. Schema/ID tham chiếu, giao thức hủy/bù, timeout/retry và race với mất quyền/xóa phòng cần thiết kế chi tiết. Chủ thể điều phối đã chọn tại [D15](#v1-channel-creation-coordination); đường truyền gRPC trực tiếp tại [D16](#v1-channel-preparation-transport). OpenAPI chỉ cập nhật description; chưa thay đổi schema/response, code hoặc SQL.

<a id="v1-channel-creation-coordination"></a>

### D15 — Điều phối tạo phòng v1

Trạng thái: đã xác nhận phương án A ngày 2026-10-11 — [DEC-133](../../records/decisions/README.md#dec-133). Community lưu tiến độ tạo phòng trong DB mình, yêu cầu Messaging/Media chuẩn bị, ghi nhận kết quả và điều phối phục hồi. Chỉ chốt phòng sẵn sàng khi đủ điều kiện [D14](#v1-channel-provisioning).

| Trách nhiệm | Service sở hữu |
|---|---|
| Tiến độ tạo, bước cần thực hiện, kết quả đã ghi nhận và phục hồi | Community; dữ liệu bền trong DB Community |
| Chuẩn bị dữ liệu chat/voice và kết quả tương ứng | Messaging hoặc Media; mỗi owner tự ghi DB của mình |
| Hoàn tất tạo và công bố phòng sẵn sàng | Community, sau khi đủ dữ liệu nền và quyền hợp lệ theo D14 |

Lựa chọn giữ một nơi theo dõi toàn bộ thao tác; đánh đổi là cần logic điều phối/phục hồi tại Community. Khi Community ngừng, tiến trình chưa thể được chốt hoàn tất dù owner đã chuẩn bị dữ liệu. Công việc phục hồi chạy cùng API Community theo [D12](../../system/backend.md#v1-application-workers); không thêm service điều phối riêng. Tham khảo trách nhiệm lưu trạng thái và phục hồi của orchestration tại [Microsoft — Saga](https://learn.microsoft.com/en-us/azure/architecture/patterns/saga).

Ví dụ Messaging đã tạo space nhưng Community chưa ghi nhận kết quả rồi restart: Community đọc tiến độ bền để đối soát và xử lý theo quyết định/thời hạn của thao tác. Nếu đã hủy theo [D19](#v1-channel-creation-interruption), công việc nền tiếp tục bù/dọn và chặn công bố muộn. Timeout không chứng minh owner chưa commit. Schema/trình tự transaction và cách nhận diện kết quả cần thiết kế, chưa có proof phục hồi.

Mỗi service giữ transaction cục bộ và quyền sở hữu DB; Community không đọc/ghi DB Messaging hoặc Media trực tiếp. Quyền phải được bảo vệ tới từng commit và bước hoàn tất theo D05/D14. Kết quả đến muộn không tự công bố tài nguyên đã bị vô hiệu.

Đường truyền lệnh/kết quả đã chọn tại [D16](#v1-channel-preparation-transport), phản hồi tại [D17](#v1-channel-creation-response), hủy/bù nền tại [D19](#v1-channel-creation-interruption). Giao thức hủy/bù, hợp đồng HTTP, schema/ID, deadline/retry và xóa phòng còn cần thiết kế chi tiết. Sự kiện sau commit theo [D27](../../system/api-conventions.md#v1-integration-event-transport), chưa thêm broker ban đầu. Chưa đổi code, SQL hoặc schema/response của hợp đồng máy đọc.

<a id="v1-channel-preparation-transport"></a>

### D16 — Truyền lệnh chuẩn bị phòng v1

Trạng thái: đã xác nhận phương án A ngày 2026-10-11 — [DEC-134](../../records/decisions/README.md#dec-134). Community gọi gRPC trực tiếp tới Messaging/Media để chuẩn bị dữ liệu phòng và nhận kết quả trong lần gọi. Community lưu kết quả, đối soát/thử lại từ tiến độ bền theo [D15](#v1-channel-creation-coordination) khi kết quả chưa rõ.

| Bước | Trách nhiệm |
|---|---|
| Gọi chuẩn bị dữ liệu | Community gọi owner theo gRPC/Protobuf đã chọn tại D04 |
| Trả kết quả đã chuẩn bị | Messaging hoặc Media trả kết quả sau khi dữ liệu đúng thao tác đã commit trong DB của mình |
| Ghi nhận và hoàn tất | Community ghi nhận kết quả; chỉ công bố phòng sẵn sàng khi đủ điều kiện D14 và quyền hợp lệ |

Lựa chọn dùng giao thức nội bộ đã chọn, không thêm kênh giao nhận lệnh/kết quả riêng cho luồng này. Đánh đổi là mỗi lần gọi cần owner đáp ứng. Nếu Messaging ngừng, tiến trình tạo phòng text chưa hoàn tất; Community giữ tiến độ, thử trong ngân sách chờ và hủy/bù theo [D19](#v1-channel-creation-interruption).

Xác nhận đã nhận RPC hoặc bắt đầu xử lý chưa đủ để báo “đã chuẩn bị”. Timeout/mất phản hồi có thể xảy ra sau khi owner đã commit theo [gRPC — status codes](https://grpc.io/docs/guides/status-codes/). Phục hồi phải nhận diện cùng thao tác và tránh tạo trùng; timeout không tự rollback dữ liệu hoặc biến kết quả chưa rõ thành lỗi cuối cùng.

gRPC trực tiếp nói về lần gọi nội bộ; thời hạn tạo và phản hồi theo [D17/D19](#v1-channel-creation-response). Danh tính service dùng mTLS theo [D20](../../system/api-conventions.md#v1-service-authentication). Hợp đồng `.proto`, cấu hình xác thực và quyền RPC, deadline/lịch retry, giao thức hủy/bù và xóa phòng cần thiết kế chi tiết. Giữ transaction cục bộ và quyền tới commit; [D27](../../system/api-conventions.md#v1-integration-event-transport) về sự kiện sau commit không thay điều kiện “đã chuẩn bị” của D16. Chưa đổi code, SQL hoặc schema/response.

<a id="v1-channel-creation-response"></a>

### D17 — Phản hồi tạo phòng v1

Trạng thái: lựa chọn ban đầu tại [DEC-135](../../records/decisions/README.md#dec-135) đã được thay một phần bởi [DEC-137](../../records/decisions/README.md#dec-137) ngày 2026-10-11. Hiện hành: chờ ngắn; trả thành công khi đã hoàn tất hoặc lỗi xác định sau khi hủy được xác nhận theo [D19](#v1-channel-creation-interruption). Dữ liệu đang chuẩn bị giữ nội bộ; danh sách chỉ thêm phòng sẵn sàng theo D14.

| Tình huống | Phản hồi | UI |
|---|---|---|
| Thao tác mới đã hoàn tất | `201` và phòng theo quyền hiện hành | Báo thành công, cập nhật danh sách và chuyển tới phòng |
| Replay cùng thao tác đã hoàn tất | `200` và phòng hiện hành sau kiểm tra quyền | Dùng kết quả của cùng thao tác |
| Hết thời hạn, hủy đã lưu bền và chặn công bố muộn | Lỗi tạo xác định; HTTP/errorCode chi tiết chưa chốt | Giữ dữ liệu nhập và nút Thử lại |
| Mất phản hồi hoặc chưa xác nhận được hoàn tất/hủy | Kết quả chưa rõ; đối soát GET theo D18 | Thông báo chưa xác nhận được kết quả; Thử lại đối soát thao tác cũ trước |

Nút Tạo phản hồi đang xử lý ngay trong khoảng chờ. Mô hình `202`/pending công khai để tiếp tục tạo lâu dài của DEC-135 được thay bằng D19. Chỉ báo lỗi tạo xác định khi server đã xác nhận nhánh hủy; timeout ở browser hoặc RPC không đủ để kết luận. Kết quả đọc phòng vẫn theo dữ liệu/quyền hiện hành.

Giữ chống trùng và không tự POST lại. Khi kết quả cũ chưa rõ, dùng cùng `clientOperationId`/payload và GET [D18](#v1-channel-operation-recovery); nút Thử lại xử lý theo [D19](#v1-channel-creation-interruption). Việc đóng request/màn hình không thay thế quyết định hoàn tất/hủy bền tại server.

Nền MVP hiện trả `201/200` và Channel DTO trong shared transaction. Thời gian chờ cụ thể, mã lỗi, URL/schema trạng thái, quyền đọc chi tiết, lịch GET, retention và hợp đồng bù/dọn v1 cần thiết kế tiếp. OpenAPI chỉ cập nhật description; chưa đổi response/schema, code hoặc SQL.

<a id="v1-channel-operation-recovery"></a>

### D18 — Tìm lại tiến trình khi mất phản hồi

Trạng thái: đã xác nhận phương án A ngày 2026-10-11 — [DEC-136](../../records/decisions/README.md#dec-136). Khi mất phản hồi hoặc reload mà client còn khóa thao tác, WebClient dùng GET tra cứu tiến trình/kết quả đã lưu bằng `clientOperationId`. Không cần nhận được tham chiếu server trong phản hồi đầu để tìm lại cùng thao tác.

| Thành phần tra cứu | Quy tắc |
|---|---|
| Actor | Actor từ phiên hiện hành, kiểm tra quyền trước khi trả dữ liệu |
| Operation kind | `create_channel`; đây là loại thao tác, khác kind text/voice của phòng |
| Scope và khóa | Server cùng `clientOperationId` đã lưu trước POST |
| Kết quả | Tiến trình/trạng thái hoặc tham chiếu kết quả đã lưu, theo quyền hiện hành |

Lựa chọn khôi phục theo dõi bằng thao tác đọc; đánh đổi là thêm hợp đồng tra cứu và kiểm tra quyền. WebClient MVP đã giữ khóa/payload trong sessionStorage theo actor/server để manual retry; lookup v1 chưa triển khai. Mô hình mất phản hồi và trả cùng tiến trình theo khóa chống trùng được tham khảo tại [Microsoft — Asynchronous request-reply](https://learn.microsoft.com/en-us/azure/architecture/patterns/asynchronous-request-reply); URL tra cứu của SCDC cần thiết kế riêng.

GET chỉ đọc, không tạo, resume hoặc kích hoạt bước xử lý nghiệp vụ; công việc nền Community tiếp tục độc lập. Lookup không yêu cầu phát lại payload tạo. Giữ quy tắc không tự POST lại; manual POST retry vẫn dùng nguyên khóa/payload và kiểm tra fingerprint. Không khởi động lại tiến trình đã kết thúc hoặc tạo khóa mới chỉ vì mất phản hồi.

Chưa tìm thấy bản ghi không tự chứng minh request chưa từng được tiếp nhận: request có thể còn đang tới server hoặc metadata không còn trong thời hạn lưu. Hủy/bù và Thử lại sau khi đã xác nhận hủy theo [D19](#v1-channel-creation-interruption). URL/schema/status enum, quyền đọc chi tiết, phạm vi lưu khóa của trình duyệt, hành vi khi mất khóa/payload, retention, lịch GET và giao thức hủy/bù chi tiết còn mở. OpenAPI chỉ cập nhật description; chưa thêm endpoint/schema hoặc đổi code/SQL.

<a id="v1-channel-creation-interruption"></a>

### D19 — Hủy tạo phòng và dọn dữ liệu

Trạng thái: đã xác nhận phương án điều chỉnh ngày 2026-10-11 — [DEC-137](../../records/decisions/README.md#dec-137). Người dùng chọn chờ ngắn, lỗi rõ ràng và nút Thử lại; đồng ý dữ liệu đã commit được bù/dọn nền sau khi server đã chặn chắc chắn việc công bố phòng muộn. Phần `202`/pending công khai tại D17 được thay; giữ điều kiện hoàn tất D14, Community điều phối bền D15, gRPC D16 và GET đối soát D18.

| Bước | Quy tắc |
|---|---|
| Chuẩn bị trong khoảng chờ | Chỉ hoàn tất/công bố sau khi đủ dữ liệu nền và quyền hợp lệ; dữ liệu chuẩn bị chưa được đưa vào danh sách hoặc sử dụng như phòng sẵn sàng |
| Chốt thành công hoặc hủy | Hai quyết định loại trừ nhau trên cùng thao tác; nếu thành công đã được chốt thì trả kết quả thành công |
| Xác nhận hủy | Lưu bền quyết định hủy và thông tin phục hồi; chỉ trả lỗi tạo xác định sau khi đã chặn công bố muộn của thao tác đó |
| Bù/dọn nền | Community điều phối theo tiến độ bền; mỗi owner xử lý dữ liệu của mình, đối soát/thử lại khi bị gián đoạn |

Sau khi hủy được xác nhận, lệnh/kết quả đến muộn không được làm thao tác cũ thành công hoặc công bố phòng. Hợp đồng owner phải xử lý việc chuẩn bị đến muộn cả khi bước dọn đã chạy; chỉ xóa dữ liệu rồi quên thao tác chưa đủ. Dữ liệu chuẩn bị phải được chặn trên mọi đường đọc/ghi/phát sự kiện trước khi phòng sẵn sàng. Giao thức hủy, dấu hủy và cách đồng bộ với transaction owner cần thiết kế/kiểm chứng.

Hủy được xác nhận và dọn sạch mọi owner là hai mốc riêng. v1 giữ transaction cục bộ; dữ liệu đã commit được bù bằng bước xử lý có thể phục hồi, không có rollback chung tự động. Bù/dọn cũng có thể thất bại và cần chống trùng, lưu tiến độ để thử lại theo [Microsoft — Compensating Transaction](https://learn.microsoft.com/en-us/azure/architecture/patterns/compensating-transaction). Worker phục hồi cùng API Community theo D12; khi thao tác đã hủy, worker tiếp tục đối soát/dọn dữ liệu của thao tác đó.

Nút Thử lại giữ dữ liệu nhập và phân biệt kết quả cũ: chưa rõ thì GET đối soát, manual POST replay giữ nguyên khóa/payload; đã thành công thì đọc phòng hiện hành; đã xác nhận hủy kết thúc thì hành động Thử lại tạo thao tác mới với khóa mới. Quy tắc này giữ điều kiện thao tác terminal không được tạo lại bằng khóa cũ tại [operation Community](../../system/community.md#operations). Phải chặn thao tác cũ sống lại và giới hạn bù/dọn đúng dữ liệu của nó, không ảnh hưởng lần tạo mới. Không tự POST lại từ browser.

Khoảng chờ ngắn không bảo đảm luôn nhận được kết quả xác định trong thời hạn đó. Nếu chưa xác nhận được hoàn tất hoặc hủy, client vẫn có kết quả chưa rõ và dùng D18; timeout có thể xảy ra sau commit theo [gRPC — Status codes](https://grpc.io/docs/guides/status-codes/). Không tiếp tục hoàn tất bằng phiên/quyền đã mất hiệu lực theo D05.

Thời gian chờ, HTTP/errorCode, schema trạng thái, giao thức hủy/bù, dấu hủy/retention, giải phóng tên/quota, lịch thử/giới hạn đồng thời và xóa phòng đã công bố cần thiết kế tiếp. Kiểm chứng phải bao gồm race thành công/hủy, mất phản hồi sau commit, lệnh đến muộn sau dọn, restart giữa bù và Thử lại sau hủy. Chưa đổi code, SQL hoặc route/schema; chưa có proof v1.

<a id="channel-webclient-và-bằng-chứng"></a>

### WebClient và bằng chứng

Nền WebClient MVP: detail member vào danh sách phòng theo quyền. Tạo giữ operation/body theo actor/server trước POST, retry chủ động nguyên body/key sau mất response/reload. Chờ ngắn/hủy/bù [D17/D19](#v1-channel-creation-response) và lookup [D18](#v1-channel-operation-recovery) của v1 chưa triển khai. Sửa metadata giữ version đã đọc; conflict/timeout cần GET đối soát trước lưu lại. Actor/server/selection đổi hủy đọc cũ và bỏ dữ liệu quản lý; 401/403/404 đóng metadata cũ. Không hiển thị tin/composer khi gói chưa có API tin.

Giao diện sửa quyền, catalog role/member và giữ draft/epoch được mô tả tại [ACL editor](access-control.md#acl-editor-ui). Máy chủ kiểm tra lại quyền dưới khóa cho từng thao tác.

Nghiệm thu API/DB/shared transaction và Chromium độc lập với thuật toán fixture tại [hồ sơ nghiệm thu](../../records/verification/community/channels-access.md). Không suy HTTP guard hoặc outbox thành proof thu hồi realtime/Media; phụ thuộc và việc tiếp theo ở [kế hoạch Community](../../project/planning.md#community-work-items).

### Lifecycle xóa và voice — giả định một host

<a id="delete-giao-dịch-của-thành-phần"></a>

#### Giao dịch của thành phần

- Xóa phòng: Community chuyển deleted/version/accessVersion, Messaging đánh dấu space deleted qua hợp đồng lifecycle, outbox thu hồi cùng transaction. Không có hai commit độc lập khiến phòng đã deleted vẫn nhận tin; không purge tin/backup ở đây.

Voice cần Media admission/lifecycle. Đề xuất `IMediaRoomLifecycle` tạo room DB rỗng cùng transaction và đánh dấu closing khi xóa giả định một host/database; interface/migration chưa có. Điều kiện tạo voice v1 theo [D14](#v1-channel-provisioning); giao thức tạo/xóa xuyên database cần thiết kế tiếp. SFU provision/dừng theo [Media](../media/participation-lifecycle.md#provider); cutoff DEC-099 cần bằng chứng riêng. Kind voice có thể được bảo toàn khi migrate legacy, nhưng writer hiện hành chỉ tạo text.

<a id="acceptance"></a>

<a id="tiêu-chí-chấp-nhận"></a>

## Tiêu chí chấp nhận

| Mã | Tình huống kiểm tra | Kết quả mong đợi |
|---|---|---|
| <a id="ac-com-07"></a> AC-COM-07 | Thành viên không có quyền tạo phòng thử tạo phòng. | Hệ thống từ chối thao tác; chủ sở hữu hoặc người được cấp quyền thực hiện được. |
| <a id="ac-com-14"></a> AC-COM-14 | Tin được lưu trong phòng khi thành viên đang vắng mặt; thành viên mở lại phòng sau đó và vẫn có quyền xem. | Thành viên thấy tin trong lịch sử phòng. |
| <a id="ac-com-15"></a> AC-COM-15 | Thành viên mới tham gia mở phòng có tin từ trước và mình được phép xem. | Thành viên thấy lịch sử cũ của phòng. |
| <a id="ac-com-34"></a> AC-COM-34 | Người đúng/sai quyền sửa/xóa phòng đang có tin/cuộc gọi | Deleted không đọc/gửi/nhận/tiếp tục gọi được; chặn race writer; không có khôi phục v1 |


Các tiêu chí liên quan nhiều phần có một nguồn chuẩn ở thành phần chủ trì; [ma trận UC/AC/TC](traceability.md#use-case-coverage) dẫn tới tất cả tiêu chí cần kiểm chứng. Kết quả thực thi được quản lý trong hồ sơ nghiệm thu, dẫn chiếu từ [trạng thái theo khả năng](#implementation).

<a id="tests"></a>

<a id="ca-kiểm-thử"></a>

## Ca kiểm thử

Dùng [dữ liệu và cách ghi bằng chứng chung](../messaging/channel-messaging.md#evidence). Các ca bên dưới là đặc tả kiểm chứng; cần bổ sung assertion cho từng endpoint/nhánh và kiểm tra quyền bằng API.

| Mã ca | Thao tác và dữ liệu | Kết quả cần quan sát | Dẫn chiếu |
|---|---|---|---|
| <a id="tc-com-09"></a> TC-COM-09 | Thành viên mới/mất mạng mở lại phòng có tin cũ | Lịch sử đầy đủ nếu còn quyền xem | [AC-COM-14](#ac-com-14), [AC-COM-15](#ac-com-15) |
| <a id="tc-com-15"></a> TC-COM-15 | Xóa phòng cùng lúc gửi/join media; thử ID phòng cũ sau xóa | Không phục hồi phòng, không đọc/nhận/gọi sau thu hồi; tin đã commit giữ theo retention | [AC-COM-34](#ac-com-34) |

<a id="gaps"></a>

<a id="việc-còn-lại"></a>

## Việc còn lại

Trạng thái phụ thuộc chung theo [kế hoạch triển khai](../../project/planning.md#community-work-items), [migration](../../system/community.md#schema-migration) và [vòng đời dữ liệu](../../system/data-lifecycle.md). Các đầu vào review/mock/proof còn mở, không đánh dấu nghiệm thu từ tài liệu/fixture.

Lifecycle create text cùng Messaging, danh sách/metadata/view guard/tên Unicode đã có bằng chứng trong [gói phòng/ACL](../../records/verification/community/channels-access.md). Còn lifecycle delete nguyên tử và race gửi/xóa, lịch sử/writer Messaging dùng guard, Hub/thu hồi; voice phụ thuộc Media lifecycle và proof cutoff riêng. Phạm vi đã đạt và phần chưa chứng minh được đối chiếu tại [verification](../../records/verification/community/README.md), tiến độ chỉ ghi tại [trạng thái theo khả năng](#implementation).
