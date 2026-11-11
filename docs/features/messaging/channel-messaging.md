# SCDC — Nhắn tin trong phòng

Thành viên có quyền xem một phòng chữ có thể đọc lịch sử, gửi tin, sửa và xóa tin của mình, và nhận tin mới theo thời gian thực. Khi mất quyền, họ ngừng nhận tin của phòng đó.

**Trạng thái:** chưa có. Nền `900e226` đã có ChatHub và outbox cho DM, và Community đã có tạo phòng, ACL, guard. Nhưng chưa có: ghi tin phòng, `SubscribeChannel`, `UnsubscribeChannel`, sự kiện `ChannelMessageChanged`. MVP chuyển phần lịch sử, gửi, nhận và mất quyền sang v1 ([MVP](../../releases/mvp.md#community-scope)); sửa và xóa (UC-COM-24) cũng thuộc v1.

**Người phụ trách:** Sáng (tin phòng và giao diện liên quan); Vg (Community, quyền, vòng đời phòng). Xem [phân công](../../project/planning.md#team).

**Ai sở hữu gì:** Messaging sở hữu tin, Hub và worker gửi sự kiện. Community sở hữu phòng, thành viên và quyền. Identity cung cấp phiên.

Tài liệu liên quan: điều kiện chung, version, epoch, lỗi ở [cơ chế Community](../community/mechanics.md); quyền ở [vai trò và quyền](../community/access-control.md); lưu, gửi, gửi lại, sửa, xóa ở [vòng đời tin](message-lifecycle.md); nội dung ở [text-policy](text-policy.md); lịch sử và Hub ở [đồng bộ](synchronization.md).

Backup và placeholder sau khôi phục để [sau v1](../../project/overview.md#backup-scope). Thời hạn lưu, sửa, xóa, chống trùng và thu hồi quyền vẫn thuộc v1.

<a id="requirements"></a>

<a id="phạm-vi-và-quy-tắc"></a>

## Quy tắc

| Mã | Quy tắc |
|---|---|
| <a id="com-009"></a> COM-009 | Gửi file trong phòng đã có trong kế hoạch, chi tiết đang thiết kế ([gửi file](../files/README.md)). Hiện chỉ có tin văn bản. |
| <a id="com-013"></a> COM-013 | Người gửi sửa và xóa tin phòng giống tin riêng: sửa lúc nào cũng được, có dấu "Đã sửa"; xóa thì mọi người thấy dòng thay thế. Trạng thái gửi và gửi lại cũng giống DM. |
| <a id="com-014"></a> COM-014 | Thành viên có quyền mở phòng vẫn xem được các tin đã lưu khi họ vắng mặt. |
| <a id="com-017"></a> COM-017 | Đợt đầu: ai có quyền xem phòng thì được gửi tin văn bản trong phòng đó. |
| <a id="com-018"></a> COM-018 | Gửi lại cùng một thao tác không tạo tin trùng. |
| <a id="com-022"></a> COM-022 | Phải xác minh email mới được gửi tin. |
| <a id="com-023"></a> COM-023 | Tin đã sửa chỉ giữ nội dung mới nhất; không có lịch sử bản cũ. |
| <a id="com-024"></a> COM-024 | Tối đa 2.000 đơn vị UTF-16 sau khi chuẩn hóa xuống dòng; cho xuống dòng và emoji; từ chối Unicode lỗi và tin trống. Dùng chung [quy tắc nội dung](text-policy.md#contract-6). |

<a id="use-cases"></a>

<a id="use-case-tích-hợp-tin-phòng-và-realtime"></a>

## Ca sử dụng

Điều kiện, version, epoch, gửi lại và lỗi dùng chung theo [quy ước tích hợp](../community/mechanics.md#use-case-conditions). Quy tắc, AC và TC của từng ca xem [bảng truy vết](../community/traceability.md#use-case-coverage).

<a id="uc-com-23"></a>

<a id="uc-com-23--gửi-và-chủ-động-thử-lại-tin-văn-bản"></a>

### UC-COM-23 — Gửi tin và gửi lại

**Thực hiện:** Messaging. **Phối hợp:** Community (thành viên, quyền phòng), Identity (tài khoản, phiên), WebClient.

**Ai làm:** thành viên đang hoạt động, có quyền xem phòng chữ, tài khoản đã xác minh, phiên hợp lệ.

**Điều kiện trước:** phòng chữ đang hoạt động. Điều kiện Identity và Community được giữ tới khi Messaging commit.

**Khi nào:** người dùng bấm Gửi, hoặc bấm Thử lại cho tin chưa rõ kết quả.

**Các bước:**

1. Client giữ nội dung và `clientMessageId`. Messaging chuẩn hóa và kiểm tra nội dung ([COM-024](#com-024)).
2. Messaging kiểm tra quyền, kiểm tra chống trùng, rồi lưu tin, sequence và outbox trong một transaction.
3. Sau commit, trả tin và hiện "đã gửi". Gửi lại cùng khóa và nội dung thì trả lại đúng tin đó, không tạo tin hay outbox mới.

**Ngoại lệ:**

- Chưa xác minh, mất phiên, mất quyền xem, phòng đã xóa, hoặc là phòng thoại: bị chặn.
- Cùng `clientMessageId` nhưng khác nội dung: lỗi xung đột. Thiếu khóa fingerprint: dừng thao tác.
- Mất phản hồi: giữ trạng thái chưa rõ hoặc lỗi để người dùng tự gửi lại.
- Thu hồi quyền xảy ra cùng lúc không chen được vào giữa bước kiểm tra quyền và commit.

**Kết quả:** mỗi thao tác hợp lệ tạo đúng một tin, đã lưu trước khi hiện "đã gửi". File và quyền chỉ đọc chưa thuộc tin phòng v1.

<a id="uc-com-24"></a>

<a id="uc-com-24--sửa-hoặc-xóa-tin-của-mình"></a>

### UC-COM-24 — Sửa hoặc xóa tin của mình

**Thực hiện:** Messaging. **Phối hợp:** Community, Identity, WebClient.

**Ai làm:** tác giả tin, còn quyền xem phòng và đủ điều kiện dùng ứng dụng.

**Điều kiện trước:** phòng đang hoạt động, tin thuộc phòng, client đã có version của tin. Quyền tác giả và quyền xem được kiểm tra lại khi thao tác.

**Khi nào:** tác giả chọn Sửa hoặc Xóa trong menu của tin (màn hình COM-S03).

**Các bước:**

1. **Sửa:** gửi nội dung mới và `expectedVersion`; Messaging kiểm tra nội dung và quyền tác giả.
2. **Xóa:** gửi `expectedVersion`; Messaging bỏ nội dung, giữ lại ID, sequence và khóa chống trùng (tombstone).
3. Tin, version và outbox commit cùng lúc. Người có quyền thấy nội dung mới với dấu "Đã sửa", hoặc dòng "Tin nhắn đã bị xóa".

**Ngoại lệ:**

- Chủ server hay người quản lý không sửa, xóa được tin người khác.
- Không sửa được tin đã xóa. Sai version hoặc mất quyền xem: phải tải lại hoặc bị chặn.
- Nội dung sửa không hợp lệ: giữ form. Phản hồi không rõ: không tự gửi lại.
- Không trả nội dung cũ qua kết quả của thao tác gửi.

**Kết quả:** sửa chỉ giữ nội dung mới nhất. Xóa không làm mất khóa chống trùng; gửi lại không hồi sinh tin đã xóa.

<a id="uc-com-25"></a>

<a id="uc-com-25--nhận-cập-nhật-kết-nối-lại-và-xử-lý-mất-quyền"></a>

### UC-COM-25 — Nhận cập nhật, kết nối lại, mất quyền

**Thực hiện:** luồng tích hợp; Messaging làm Hub và worker. **Phối hợp:** Community (quyền, vòng đời phòng, yêu cầu thu hồi), Identity (phiên), WebClient (đồng bộ).

**Ai làm:** người dùng đang dùng Community; client và hệ thống realtime.

**Điều kiện trước:** đăng ký nhận tin phòng cần người dùng, tư cách thành viên và quyền xem hiện tại. Thông báo về yêu cầu tham gia hay lời mời được gửi đúng người dùng và người quản lý còn quyền, không bắt người ngoài đăng ký theo dõi server.

**Khi nào:** mở phòng; có sự kiện đã commit; kết nối lại; quyền, phiên, tư cách thành viên hay phòng thay đổi.

**Các bước:**

1. Client đăng ký nhận tin phòng qua Hub chat. Server kiểm tra điều kiện, gắn đăng ký với phiên, server, phòng, `membershipId` và `accessVersion`.
2. Worker gửi trạng thái hiện tại của tin cho các kết nối còn quyền. `CommunityChanged` chỉ báo client tải lại thông tin và quyền. Thông báo về yêu cầu, lời mời, tư cách thành viên chỉ gửi cho đúng người được biết.
3. Khi kết nối lại: kiểm tra quyền, đăng ký lại, bù tin qua REST, tải lại các trang cũ để thấy tin đã sửa hoặc xóa. Gộp theo ID và version; chỉ tiến cursor sau khi gộp đủ trang.
4. Khi bị thu hồi quyền: server tự gỡ đăng ký và chặn nội dung mới; client đóng nội dung và ô soạn tin, xóa dữ liệu đệm và bản nháp của phòng đó. Thời hạn: 5 giây từ commit.

**Ngoại lệ:**

- Mất quyền hoặc phiên thì không kết nối lại vào phòng cũ được. Kiểm tra quyền lỗi thì dừng gửi.
- Sự kiện trùng hoặc đến sai thứ tự không tạo tin lặp, không hiện version cũ hơn.
- Thu hồi `membershipId` cũ không xóa dữ liệu của lần tham gia lại (epoch mới).
- Client không hợp tác vẫn bị server ngừng gửi. Không phát danh sách thành viên, ACL, yêu cầu hay lời mời cho cả server.

**Kết quả:** client có quyền đồng bộ với dữ liệu đã lưu; phòng bị mất quyền không nhận thêm nội dung. DM và các phòng khác theo quyền riêng. Cắt media kiểm thử riêng theo Media.

<a id="ux"></a>

## Giao diện

- Mở phòng: hiện thông tin phòng (theo quyền xem) và lịch sử tin ([UC-COM-17](../community/channels.md#uc-com-17)).
- Khung chat có trạng thái đang gửi, đã lưu, lỗi và nút Thử lại ([vòng đời tin](message-lifecycle.md#client-state)). Menu Sửa và Xóa chỉ hiện cho tác giả.
- Mất quyền, rời server, phòng bị xóa: xóa dữ liệu đệm và tin tạm của phòng, đóng ô soạn tin. DM không bị ảnh hưởng.
- Bản nháp tin phòng (đề xuất): giữ trong bộ nhớ tab như DM, không lưu xuống trình duyệt.
- Media nhận thu hồi từ cùng commit, ngắt trong 5 giây hoặc dừng nếu không xác nhận được ([thiết kế media](../media/participation-lifecycle.md)). Bằng chứng của chat không áp cho media.

<a id="contracts"></a>

## API

Hợp đồng mục tiêu: [OpenAPI Community](../../contracts/community.openapi.json) và [schema realtime](../../contracts/community-realtime.schema.json). Các route tin phòng và lệnh, sự kiện phòng trên Hub chưa có.

<a id="channel-admission"></a>

### Kiểm tra quyền khi ghi

`IChannelAccessGuard` kiểm tra Identity, server và phòng trong transaction của bên gọi và giữ tới commit. Lease gồm epoch thành viên, các phiên bản quyền và hạn phiên. Messaging kiểm tra lease ngay trước commit, và tự kiểm tra không gian và tác giả trong dữ liệu của mình. Messaging không đọc hay JOIN bảng Community, và không thay việc kiểm tra bằng một giá trị đúng/sai. Thứ tự khóa xem [cơ chế Community](../community/mechanics.md#transactions).

<a id="channel-messaging"></a>

<a id="tin-phòng-và-realtime"></a>

### REST

`/servers/{id}/channels/{channelId}/messages` với GET, POST, PATCH, DELETE, giống DM.

- Tin dùng `channelId` thay `conversationId`. Sequence theo không gian Messaging. Fingerprint, chống trùng, tombstone, version, cursor dùng chung thiết kế DM.
- Phòng thoại chỉ có media, chưa có chat chữ bên trong. Không đổi loại phòng bằng PATCH trong v1.
- Quyền quản lý hay chủ server không thay cho kiểm tra tác giả khi sửa, xóa.

### Hub

Vẫn là `/hubs/chat`. Schema riêng [community-realtime.schema.json](../../contracts/community-realtime.schema.json) thêm `SubscribeChannel`, `UnsubscribeChannel` và `ChannelMessageChanged`.

- Đăng ký kiểm tra phiên, tư cách thành viên và quyền hiện tại. Registry gắn người dùng, phiên, server, phòng, `membershipId`, `accessVersion`.
- Worker chỉ gửi cho kết nối còn đủ điều kiện; đối chiếu lại mỗi tối đa 1 giây; ngắt trong 5 giây từ commit thu hồi (DEC-083). Lỗi kiểm tra quyền thì dừng gửi.
- `CommunityChanged` chỉ chứa `eventId`, `serverId`, `accessVersion`; không chứa tên phòng bị ẩn, danh sách thành viên, vai trò hay nội dung tin.
- `MembershipChanged`, `JoinRequestChanged`, `MemberInvitationChanged` gửi đúng người và người quản lý còn quyền.
- Server tự gỡ đăng ký khi mất quyền, không chờ client.
- Client gộp version theo từng tài nguyên. Sự kiện thu hồi của `membershipId` cũ không xóa dữ liệu của lần tham gia lại.

**v1:** sự kiện Community → Messaging đi qua outbox, worker và gRPC ([D27](../../system/api-conventions.md#v1-integration-event-transport)). Hợp đồng nội bộ thiết kế riêng, khác với schema gửi trình duyệt, và không thay việc kiểm tra quyền trước khi gửi.

<a id="acceptance"></a>

<a id="tiêu-chí-chấp-nhận"></a>

## Tiêu chí chấp nhận

| Mã | Tình huống | Kết quả mong đợi |
|---|---|---|
| <a id="ac-com-12"></a> AC-COM-12 | Thành viên gửi tin trong phòng, rồi sửa và xóa tin của mình | Tin được lưu và xem lại được; sửa có "Đã sửa"; xóa hiện "Tin nhắn đã bị xóa" cho người có quyền xem |
| <a id="ac-com-13"></a> AC-COM-13 | Sửa hoặc xóa tin người khác; đọc hoặc gửi ở phòng không có quyền | Bị từ chối, kể cả khi gọi API trực tiếp |
| <a id="ac-com-18"></a> AC-COM-18 | Gửi tin lần đầu không rõ kết quả; người gửi bấm Thử lại | Phòng chỉ có một tin |
| <a id="ac-com-22"></a> AC-COM-22 | Thành viên chưa xác minh email thử gửi tin | Bị từ chối, kèm hướng dẫn xác minh |
| <a id="ac-com-23"></a> AC-COM-23 | Sửa tin nhiều lần; người khác tải lại | Chỉ thấy nội dung mới nhất và "Đã sửa"; không có lịch sử |
| <a id="ac-com-24"></a> AC-COM-24 | Gửi hoặc sửa tin dài 2.000 và 2.001 đơn vị UTF-16; tin chỉ có khoảng trắng hoặc ký tự vô hình; Unicode lỗi | Nhận tới 2.000; từ chối quá dài, tin trống, Unicode lỗi; giữ tiếng Việt, emoji, ZWJ |

Tiêu chí liên quan nhiều phần được định nghĩa ở trang chính; [bảng truy vết](../community/traceability.md#use-case-coverage) liệt kê đầy đủ. Kết quả chạy xem [Community](../community/README.md).

<a id="tests"></a>

<a id="ca-kiểm-thử"></a>

## Ca kiểm thử

Mỗi ca cần test cho từng endpoint và nhánh, và kiểm tra quyền qua API.

| Mã ca | Thao tác | Cần quan sát | Cho |
|---|---|---|---|
| <a id="tc-com-10"></a> TC-COM-10 | Gửi, sửa, xóa tin phòng; N sửa tin của O qua API; gửi lại khi mất phản hồi | Kiểm tra tác giả, tombstone, một tin mỗi thao tác; dữ liệu UTF-16 như DM | [AC-COM-12](#ac-com-12), [13](#ac-com-13), [18](#ac-com-18), [23](#ac-com-23), [24](#ac-com-24) |
| <a id="tc-com-12"></a> TC-COM-12 | U chưa xác minh giả làm thành viên, gọi API đọc và gửi | Server chặn; U không đọc được phòng | [AC-COM-22](#ac-com-22) |
| <a id="tc-com-25"></a> TC-COM-25 | Đổi vai trò, ACL, rời server, xóa phòng cùng lúc với gửi, đăng ký, gửi sự kiện | Guard giữ tới commit; ngắt chat trong 5 giây; không mất, không trùng, không trái quyền | [AC-COM-11](../community/access-control.md#ac-com-11), [AC-COM-18](#ac-com-18), [AC-COM-34](../community/channels.md#ac-com-34), [AC-COM-42](../community/access-control.md#ac-com-42) |

<a id="evidence"></a>

## Dữ liệu thử và bằng chứng

**Tài khoản:** O là chủ server; M có một quyền quản lý; N là thành viên thường; một người ngoài; U chưa xác minh. Chuẩn bị phòng mở, phòng giới hạn, vai trò allow và deny. Kiểm soát được phiên, epoch thành viên và thời điểm commit khi đổi quyền.

- Gọi trực tiếp API và Hub, kể cả với client không hợp tác. Không dùng trạng thái nút giao diện làm bằng chứng quyền.
- TC-COM-10, 12, 25 chưa có kết quả chạy.
- AC-COM-22: người chưa xác minh không đọc được phòng, vì DEC-051 chặn vào ứng dụng từ trước.
- Kiểm tra nội dung dùng [TC-TEXT](text-policy.md#tests). Mất phản hồi, đồng thời và kết nối lại phải chạy lại cho tin phòng.
- Thu hồi chat đo trong 5 giây từ commit (DEC-083). Media đo riêng (DEC-099).
- TC-COM-23 (chống trùng) và TC-COM-26 (migration) thuộc [Community](../community/mechanics.md).

Ghi kết quả theo [quy trình kiểm thử](../../releases/acceptance.md#testing) và [mẫu hồ sơ](../../records/templates/release-record.md): commit, build, mã UC, AC, TC, thời điểm, kết quả và giới hạn.

<a id="gaps"></a>

<a id="việc-còn-lại"></a>

## Còn mở

Phụ thuộc chung xem [kế hoạch triển khai](../../records/verification/community/README.md#history-use-case-delivery), [migration](../community/mechanics.md#schema-migration) và [vòng đời dữ liệu](../../system/data-lifecycle.md).

- Dùng chung với DM: kiểm tra nội dung, HMAC, sequence, cursor. Đã có schema REST và realtime; chưa có writer, mock hay bằng chứng.
- Mất quyền không xóa nội dung. Phòng đã xóa giữ dữ liệu, chưa đặt thời hạn (DEC-105).
- Placeholder sau khôi phục (DEC-108) và bằng chứng backup để sau v1 (DEC-156).
- Theo dõi tại OQ-005, OQ-011.

---

Nguồn: SCP-005, DEC-023 bổ sung bởi DEC-173 (COM-009), DEC-034 (COM-013), DEC-035 (COM-014), DEC-037 (COM-018), DEC-040 (COM-017), DEC-041 (COM-022), DEC-051, DEC-052 (COM-023), DEC-053, DEC-068, DEC-090 (COM-024), DEC-083 (chat ≤ 5 giây), DEC-099 (media), [DEC-171](../../records/decisions/README.md#dec-171).
