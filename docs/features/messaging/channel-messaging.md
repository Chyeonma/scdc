# Nhắn tin trong phòng

Phần tin phòng của SCP-005; UC-COM-23/24 và phần chat của UC-COM-25. Messaging sở hữu tin/Hub/dispatcher; Community sở hữu channel, membership và quyền; Identity cung cấp phiên. Vg giữ UI Community và quyền/lifecycle phòng, Sáng cung cấp nền Messaging; Thái cung cấp dataset/bộ chạy.

## Trạng thái và phạm vi bàn giao

[MVP](../../releases/mvp.md#community-scope) đã chọn lịch sử/gửi/nhận tin và xử lý mất quyền. Sửa/xóa tin UC-COM-24 thuộc phần mở rộng/v1. Chưa có writer tin phòng hoặc Hub được kiểm chứng; nền tạo phòng/ACL và guard đã có bằng chứng riêng tại [Community](../community/channels.md), không chứng minh chat/realtime đã đạt.

Nguồn điều kiện nền/version/epoch/lỗi và ranh giới module tại [Community](../../system/community.md). Quyền xem và management tại [phân quyền](../community/access-control.md); model/gửi/retry/sửa/xóa tại [vòng đời tin](message-lifecycle.md), validation tại [nội dung văn bản](text-policy.md), history/Hub tại [đồng bộ](synchronization.md).

<a id="requirements"></a>

<a id="phạm-vi-và-quy-tắc"></a>

## Phạm vi và quy tắc

| Mã | Quy tắc | Căn cứ |
|---|---|---|
| <a id="com-009"></a> COM-009 | Gửi file tài liệu trong phòng được xếp vào đợt sau. | DEC-023 |
| <a id="com-013"></a> COM-013 | Tin văn bản trong phòng được người gửi sửa và xóa như tin riêng: sửa bất cứ lúc nào với dấu “Đã sửa”, xóa cho mọi người với dòng thay thế; trạng thái gửi và thử lại áp dụng tương tự. | DEC-034 |
| <a id="com-014"></a> COM-014 | Tin đã được lưu trong phòng vẫn xem lại được khi thành viên có quyền mở phòng sau lúc vắng mặt. | DEC-035, SCP-005 |
| <a id="com-017"></a> COM-017 | Trong đợt đầu, mọi thành viên có quyền xem phòng đều được gửi tin văn bản trong phòng đó. | DEC-040 |
| <a id="com-018"></a> COM-018 | Thử lại cùng thao tác gửi trong phòng không tạo tin trùng. | DEC-037 |
| <a id="com-022"></a> COM-022 | Người gửi phải xác minh email trước khi gửi tin trong phòng. | DEC-041, SCDC-FR-ACC-001 |
| <a id="com-023"></a> COM-023 | Tin đã sửa chỉ giữ nội dung mới nhất; không cung cấp lịch sử bản cũ. | DEC-052, nguyên tắc tương tự DM tại DEC-034 |
| <a id="com-024"></a> COM-024 | Tin văn bản tối đa 2.000 đơn vị UTF-16 sau CRLF/CR → LF; cho xuống dòng/emoji; từ chối UTF-16 lỗi và tin rỗng/chỉ trắng hoặc vô hình. Dùng chung [quy tắc nội dung Messaging](text-policy.md#contract-6). | DEC-053, DEC-068, DEC-090 |

<a id="use-cases"></a>

<a id="use-case-tích-hợp-tin-phòng-và-realtime"></a>

## Use case tích hợp tin phòng và realtime

Điều kiện, version/epoch, retry và lỗi dùng chung theo [quy ước tích hợp](../../system/community.md#use-case-conditions). Quy tắc/AC/TC áp dụng cho từng UC ở [bảng truy vết](../community/traceability.md#use-case-coverage).

<a id="uc-com-23"></a>

<a id="uc-com-23--gửi-và-chủ-động-thử-lại-tin-văn-bản"></a>

### UC-COM-23 — Gửi và chủ động thử lại tin văn bản

**Module phụ trách:** Messaging.

**Phối hợp:** Community (membership/quyền phòng), Identity (tài khoản/phiên), WebClient (gửi/thử lại).

**Tác nhân:** Thành viên active có view phòng text, tài khoản đã xác minh và phiên hợp lệ.

**Điều kiện trước:** Phòng text active; điều kiện Identity/Community được giữ tới commit Messaging theo thiết kế guard.

**Kích hoạt:** Người dùng bấm gửi từ composer hoặc chủ động thử lại thao tác gửi chưa rõ kết quả.

**Luồng chính:**

1. Client giữ payload và clientMessageId của thao tác; Messaging chuẩn hóa/kiểm tra nội dung theo [COM-024](#com-024) và thiết kế DM.
2. Messaging kiểm tra quyền, xử lý operation chống trùng rồi lưu tin, sequence theo space và outbox trong cùng transaction.
3. Sau commit, trả tin hiện hành và hiển thị đã gửi. Khi người dùng thử lại cùng khóa/payload, trả cùng tin hiện hành, không tạo tin/outbox mới.

**Ngoại lệ:** Chưa xác minh/mất phiên/mất view/deleted/voice channel bị chặn. Cùng clientMessageId khác payload trả xung đột; thiếu fingerprint key dừng thao tác. Response mất giữ trạng thái chưa rõ/lỗi để người dùng đối soát hoặc thử lại chủ động. Revoke tranh send không chen giữa kiểm tra quyền và commit.

**Kết quả sau cùng:** Một tin cho mỗi thao tác hợp lệ, lưu bền trước trạng thái đã gửi. Nội dung và retry dùng cùng cơ chế DM; file và quyền chỉ đọc độc lập chưa thuộc tin phòng v1.

<a id="uc-com-24"></a>

<a id="uc-com-24--sửa-hoặc-xóa-tin-của-mình"></a>

### UC-COM-24 — Sửa hoặc xóa tin của mình

**Module phụ trách:** Messaging.

**Phối hợp:** Community (membership/quyền phòng), Identity (tài khoản/phiên), WebClient (sửa/xóa).

**Tác nhân:** Tác giả tin, còn view phòng text và đủ điều kiện ứng dụng.

**Điều kiện trước:** Phòng active, tin thuộc phòng; tác giả đã tải message version. Quyền tác giả và view kiểm tra lại khi thực hiện.

**Kích hoạt:** Tác giả chọn sửa hoặc xóa từ menu tin tại COM-S03.

**Luồng chính:**

1. Khi sửa, tác giả gửi nội dung mới và expectedVersion; Messaging kiểm tra validation dùng chung DM và quyền tác giả.
2. Khi xóa, tác giả gửi expectedVersion; Messaging bỏ content và giữ tombstone/ID/sequence cùng operation chống trùng.
3. Thay đổi tin/version/outbox commit nguyên tử; client đủ quyền nhận nội dung hiện hành cùng dấu đã sửa hoặc dòng thay thế tin đã xóa.

**Ngoại lệ:** Owner/manager không sửa/xóa tin người khác. Tin đã xóa không được sửa, stale version hoặc mất view yêu cầu đọc lại/chặn thao tác. Sửa không hợp lệ giữ form; response không rõ không tự replay mutation. Nội dung cũ không được trả lại từ receipt của thao tác gửi.

**Kết quả sau cùng:** Sửa chỉ giữ nội dung mới nhất; xóa không làm mất khóa chống trùng hoặc phục hồi tin khi retry send. Lịch sử bản sửa cũ không được cung cấp.

<a id="uc-com-25"></a>

<a id="uc-com-25--nhận-cập-nhật-kết-nối-lại-và-xử-lý-mất-quyền"></a>

### UC-COM-25 — Nhận cập nhật, kết nối lại và xử lý mất quyền

**Module phụ trách:** Luồng tích hợp; Messaging phụ trách Hub/dispatcher.

**Phối hợp:** Community (quyền/lifecycle và yêu cầu thu hồi), Identity (phiên), WebClient (đồng bộ).

**Tác nhân:** Người dùng đang sử dụng Community; client và hệ thống realtime hỗ trợ đồng bộ/thu hồi.

**Điều kiện trước:** Subscribe phòng cần actor/membership/view hiện hành. Thông báo request/invitation định tuyến theo đúng user/session và manager còn quyền, không đòi người ngoài subscribe server.

**Kích hoạt:** Mở phòng, có sự kiện đã commit, kết nối lại hoặc thay đổi quyền/phiên/membership/phòng.

**Luồng chính:**

1. Client subscribe phòng text qua Hub chat; server kiểm tra điều kiện và gắn subscription với session, server/channel, membershipId và accessVersion.
2. Dispatcher phát bản tin hiện hành cho connection còn quyền; thông báo CommunityChanged chỉ yêu cầu tải lại metadata/quyền. Request/invitation/membership gửi đúng đối tượng được biết.
3. Khi reconnect, kiểm tra quyền và subscribe lại rồi REST bù tin, tải lại trang cũ để nhận edit/delete; merge theo ID/version và chỉ tiến resume cursor sau khi merge đủ trang.
4. Khi quyền bị thu hồi, server tự gỡ subscription/chặn nội dung mới; client đóng nội dung/composer và dọn cache/draft của scope. Thu hồi chat được kiểm chứng trong ≤5 giây từ commit.

**Ngoại lệ:** Mất quyền/phiên không được reconnect vào scope cũ; kiểm tra quyền lỗi thì dừng phát. Sự kiện trùng/đảo thứ tự không nhân tin hoặc hạ version. Revoke membershipId cũ không xóa cache epoch rejoin mới. Client không hợp tác vẫn bị server ngừng phát; không broadcast roster/ACL/request/invitation cho toàn server.

**Kết quả sau cùng:** Client đủ quyền hội tụ về trạng thái đã lưu, scope mất quyền không tiếp tục nhận nội dung; DM và phòng khác vẫn theo quyền riêng. Cutoff media được kiểm chứng riêng theo đặc tả Media.

<a id="ux"></a>

## Giao diện và mất quyền

Mở phòng kết hợp metadata theo view với lịch sử Messaging, theo [UC-COM-17](../community/channels.md#uc-com-17). Vùng chat dùng trạng thái đang gửi/đã lưu/lỗi và thử lại chủ động của [vòng đời tin](message-lifecycle.md#client-state); menu chỉ cho tác giả sửa/xóa. Sửa/xóa vẫn theo scope được chọn trong hồ sơ release.

Mất quyền/rời/xóa phòng xóa cache/tin tạm của scope tương ứng và đóng composer; không ảnh hưởng DM. Draft tin phòng đề xuất dùng RAM tab cùng cơ chế DM, không lưu nội dung xuống browser storage; chưa có quyết định riêng nếu muốn lưu bền. Media nhận revocation từ cùng commit, thu hồi ≤5 giây/fail-close theo DEC-099; admission/lease/quota gate nằm trong [thiết kế media](../media/participation-lifecycle.md), không tự coi chat proof là media proof.

<a id="contracts"></a>

## API và điều kiện admission

[OpenAPI Community](../../contracts/community.openapi.json) và [schema realtime](../../contracts/community-realtime.schema.json) giữ contract tin phòng mục tiêu; các route tin/Hub chưa triển khai. Ví dụ/schema/fixture không ghi nhận kết quả runtime.

<a id="channel-admission"></a>

`IChannelAccessGuard` giữ kiểm tra Identity/server/channel trong transaction của caller tới commit; lease gồm membership epoch, access versions và hạn phiên. Messaging kiểm tra lease ngay trước commit, đồng thời kiểm tra space/tác giả thuộc dữ liệu mình sở hữu; không đọc/JOIN bảng Community hoặc thay admission bằng một boolean. Thứ tự khóa và bằng chứng guard tại [cơ chế Community](../../system/community.md#transactions); writer/history/Hub cần proof riêng khi tiêu thụ guard.

<a id="channel-messaging"></a>

<a id="tin-phòng-và-realtime"></a>

### REST và Hub

REST tin text dùng `/servers/{id}/channels/{channelId}/messages` với GET/POST/PATCH/DELETE tương tự DM. Message wire dùng `channelId` thay `conversationId`; `sequence` theo Messaging space, fingerprint/send operation/tombstone/version và cursor/resume dùng chung thiết kế DM. Voice channel chỉ có media, chưa mở luồng text bên trong voice; kind không đổi bằng PATCH trong v1. Quyền quản lý/owner không thay kiểm tra tác giả khi sửa/xóa.

Hub vẫn `/hubs/chat`; schema riêng [community-realtime.schema.json](../../contracts/community-realtime.schema.json) bổ sung SubscribeChannel/UnsubscribeChannel và ChannelMessageChanged. Subscribe kiểm tra phiên/membership/quyền hiện hành; registry gắn user/session/server/channel/membershipId/accessVersion. Dispatcher chỉ phát nội dung cho connection còn đủ điều kiện, reconciliation ≤1 giây và deadline chat ≤5 giây từ commit thu hồi theo DEC-083. Lỗi kiểm tra quyền dừng phát.

Thông báo `CommunityChanged` chỉ chứa eventId/serverId/accessVersion và yêu cầu UI tải lại metadata/quyền; không mang tên phòng bị ẩn, danh sách thành viên/role hay nội dung tin. `MembershipChanged`, `JoinRequestChanged`, `MemberInvitationChanged` định tuyến đến đúng người và manager còn quyền theo user/session, không cần người ngoài subscribe server. Server tự gỡ subscription khi mất quyền, không trông chờ client xử lý thông báo. Client merge version theo từng resource; sự kiện thu hồi có membershipId cũ không được xóa cache của epoch rejoin mới. Reconnect subscribe lại rồi REST bù trang/tải lại tin cũ để nhận edit/delete.

<a id="acceptance"></a>

<a id="tiêu-chí-chấp-nhận"></a>

## Tiêu chí chấp nhận

| Mã | Tình huống kiểm tra | Kết quả mong đợi |
|---|---|---|
| <a id="ac-com-12"></a> AC-COM-12 | Thành viên được phép nhắn gửi tin văn bản trong phòng, rồi sửa và xóa tin của mình. | Tin được lưu và thấy lại; sửa có dấu “Đã sửa”; xóa hiện “Tin nhắn đã bị xóa” cho các thành viên có quyền xem phòng. |
| <a id="ac-com-13"></a> AC-COM-13 | Thành viên cố sửa/xóa tin của người khác hoặc đọc/gửi tin ở phòng không có quyền. | Hệ thống từ chối, kể cả khi gọi trực tiếp API. |
| <a id="ac-com-18"></a> AC-COM-18 | Kết quả gửi tin phòng lần đầu không rõ, người gửi bấm thử lại cho chính tin đó. | Phòng chỉ có một tin tương ứng với thao tác gửi. |
| <a id="ac-com-22"></a> AC-COM-22 | Thành viên chưa xác minh email nhưng có quyền xem phòng thử gửi tin. | Hệ thống từ chối gửi và chỉ dẫn bước xác minh email. |
| <a id="ac-com-23"></a> AC-COM-23 | Thành viên sửa tin nhiều lần; người khác tải lại. | Chỉ nội dung hiện hành cùng dấu “Đã sửa”, không có lịch sử nội dung cũ. |
| <a id="ac-com-24"></a> AC-COM-24 | Thành viên gửi/sửa tin tại biên 2.000/2.001 UTF-16 sau chuẩn hóa xuống dòng; gửi tin chỉ trắng/vô hình hoặc Unicode lỗi. | Nhận đến 2.000 UTF-16, từ chối vượt giới hạn/tin trống/Unicode lỗi; giữ tiếng Việt, emoji và ZWJ trong tin có nội dung theo DEC-090. |


Các tiêu chí liên quan nhiều phần có một nguồn chuẩn ở thành phần chủ trì; [ma trận UC/AC/TC](../community/traceability.md#use-case-coverage) dẫn tới tất cả tiêu chí cần kiểm chứng. Kết quả thực thi được quản lý trong hồ sơ nghiệm thu, dẫn chiếu từ [tiến độ](../community/README.md).

<a id="tests"></a>

<a id="ca-kiểm-thử"></a>

## Ca kiểm thử

Dùng [dữ liệu và cách ghi bằng chứng chung](#evidence). Các ca bên dưới là đặc tả kiểm chứng; cần bổ sung assertion cho từng endpoint/nhánh và kiểm tra quyền bằng API.

| Mã ca | Thao tác và dữ liệu | Kết quả cần quan sát | Dẫn chiếu |
|---|---|---|---|
| <a id="tc-com-10"></a> TC-COM-10 | Gửi/sửa/xóa tin phòng; N sửa tin O bằng API; gửi lại khi mất response | Kiểm tra tác giả, tombstone, một tin mỗi thao tác; dữ liệu UTF-16 dùng chung DM | [AC-COM-12](#ac-com-12), [AC-COM-13](#ac-com-13), [AC-COM-18](#ac-com-18), [AC-COM-23](#ac-com-23), [AC-COM-24](#ac-com-24) |
| <a id="tc-com-12"></a> TC-COM-12 | U chưa xác minh giả membership và gọi API đọc/gửi | Chặn truy cập ứng dụng phía server, không suy U được đọc phòng | [AC-COM-22](#ac-com-22), DEC-051 |
| <a id="tc-com-25"></a> TC-COM-25 | Channel guard tranh role/ACL/leave/delete và send/subscribe/dispatch | Guard giữ tới commit; thu hồi chat ≤5 giây, không mất/trùng/trái quyền | [AC-COM-11](../community/access-control.md#ac-com-11), [AC-COM-18](#ac-com-18), [AC-COM-34](../community/channels.md#ac-com-34), [AC-COM-42](../community/access-control.md#ac-com-42), DEC-083 |

<a id="evidence"></a>

## Dữ liệu và bằng chứng

O là owner, M có một management permission, N là thành viên thường; chuẩn bị outsider, tài khoản U chưa xác minh, phòng mở/giới hạn và role allow/deny. Kiểm soát phiên, membership epoch và thời điểm commit đổi quyền. Chạy trực tiếp API/Hub, gồm client không hợp tác; không dùng trạng thái nút UI làm proof quyền.

TC-COM-10/12/25 ở trên là ca thiết kế, chưa có actual result chat được ghi nhận. AC-COM-22 không cho phép người chưa xác minh đọc phòng: DEC-051 chặn truy cập ứng dụng trước đó. Validation dùng [TC-TEXT](text-policy.md#tests); lost response/đồng thời/reconnect phải chạy lại trong ngữ cảnh tin phòng. Thu hồi chat đo ≤5 giây từ commit theo DEC-083; proof media riêng theo DEC-099, chưa có kết quả SFU.

TC-COM-23 về operation và TC-COM-26 về migration thuộc [Community](../../system/community.md). Ghi kết quả theo [quy trình kiểm thử](../../releases/acceptance.md#testing) và [mẫu hồ sơ](../../records/templates/release-record.md), gắn commit/build/phạm vi UC/AC/TC, timestamp, kết quả và giới hạn.

<a id="gaps"></a>

<a id="việc-còn-lại"></a>

## Việc còn lại

Trạng thái phụ thuộc chung theo [kế hoạch triển khai](../../records/verification/community/README.md#history-use-case-delivery), [migration](../../system/community.md#schema-migration) và [vòng đời dữ liệu](../../system/data-lifecycle.md). Các đầu vào review/mock/proof còn mở, không đánh dấu nghiệm thu từ tài liệu/fixture.

| Nội dung | Câu hỏi còn mở | Liên quan |
|---|---|---|
| Nhắn tin phòng | Dùng chung text/HMAC/sequence/cursor DM; REST/realtime đã có schema, chưa có writer/mock/proof; mất quyền không xóa nội dung, room deleted giữ chưa đặt hạn theo DEC-105; restore placeholder dùng chung schema DEC-108, còn DATA-GAP proof. | OQ-005, OQ-011 |
