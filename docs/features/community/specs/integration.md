# SCDC — Community — Quy ước chung và tích hợp liên module

Cập nhật: 2026-10-07. Đặc tả nghiệp vụ và tiêu chí kiểm chứng; tiến độ hiện tại tại [status.md](../status.md).

Nguồn chuẩn cho điều kiện dùng chung, use case, UX và tiêu chí của hành trình tích hợp tin phòng/realtime. ID/version, transaction, retry, migration và lỗi nằm tại [thiết kế tích hợp](../design/integration.md#contracts). Tin nhắn/Hub thuộc Messaging; cơ chế nhắn tin dùng chung dẫn chiếu đặc tả DM theo [bảng nguồn chuẩn](#responsibilities).

[Tổng quan Community](../README.md) · [Truy vết UC/COM/AC/TC](README.md#use-cases) · [Kế hoạch triển khai](../delivery/README.md#use-case-delivery).


Thiết kế kỹ thuật của phần này: [dữ liệu/API và giao dịch](../design/integration.md#contracts).

## Mục lục

- [Phạm vi và quy tắc](#requirements)
- [Module phụ trách và nguồn chuẩn](#responsibilities)
- [Use case tích hợp tin phòng và realtime](#use-cases)
- [UX và trạng thái](#ux)
- [Tiêu chí chấp nhận](#acceptance)
- [Ca kiểm thử](#tests)
- [Việc còn lại](#gaps)

<a id="requirements"></a>

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
| <a id="com-024"></a> COM-024 | Tin văn bản tối đa 2.000 đơn vị UTF-16 sau CRLF/CR → LF; cho xuống dòng/emoji; từ chối UTF-16 lỗi và tin rỗng/chỉ trắng hoặc vô hình. Dùng chung [quy tắc nội dung DM](../../direct-messaging.md#detailed-design). | DEC-053, DEC-068, DEC-090 |

<a id="responsibilities"></a>

## Module phụ trách và nguồn chuẩn

Tài liệu được đặt theo hành trình người dùng. Mỗi use case ghi module thực hiện và các phần phối hợp; khi triển khai, mỗi module giữ dữ liệu và nghiệp vụ thuộc ranh giới của mình.

| Nội dung | Module phụ trách | Nguồn chuẩn |
|---|---|---|
| Cộng đồng, membership, lời mời, metadata/phân quyền phòng | Community | [Servers](servers.md), [Memberships](memberships.md), [Invitations](invitations.md), [Channels](channels.md), [Permissions](permissions.md) |
| Nội dung tin, lưu/gửi/lịch sử, sửa/xóa, chống trùng, sequence/cursor và tombstone dùng chung | Messaging | [Hợp đồng nhắn tin](../../direct-messaging.md#contracts), [thiết kế chi tiết](../../direct-messaging.md#detailed-design), [ca TC-TEXT](../../direct-messaging.md#tests) |
| Áp dụng cơ chế nhắn tin vào phòng, kiểm tra view/membership, route và định tuyến cập nhật | Messaging phối hợp Community/Identity/WebClient | [UC-COM-23/24/25](#use-cases), [tin phòng và realtime](../design/integration.md#channel-messaging), [OpenAPI](../../../contracts/community.openapi.json), [catalogue realtime](../../../contracts/community-realtime.schema.json) |
| Điều kiện tài khoản và phiên | Identity | [Accounts](../../accounts.md#use-cases); Community/Messaging dùng hợp đồng Identity |
| Giao diện, trạng thái gửi, cập nhật và đối soát sau reconnect | WebClient | [UX Community](#ux), [UX DM dùng chung cho tin](../../direct-messaging.md#ux) |

COM-013/014/017/018/022/023/024 và các AC/TC bên dưới giữ quy tắc áp dụng cho tin phòng. Phần thuật toán nhắn tin chung chỉ dẫn chiếu nguồn Messaging ở bảng trên; thay đổi cơ chế chung cập nhật tại nguồn đó và các liên kết/phần tích hợp bị ảnh hưởng. Thứ tự quyền và thu hồi của Community tiếp tục ở [Permissions](permissions.md#view-permissions).

UC-COM-17 tại [Channels](channels.md#uc-com-17) mô tả một luồng mở phòng: Community trả danh sách/metadata theo view, Messaging trả lịch sử qua guard Community. UC-COM-23/24 có module thực hiện chính là Messaging. UC-COM-25 phối hợp các module: Messaging giữ Hub/dispatcher, Community cung cấp quyền/lifecycle và thay đổi cần thu hồi, Identity cung cấp trạng thái phiên, WebClient đồng bộ giao diện. DM giữ quyền truy cập riêng theo [đặc tả DM](../../direct-messaging.md#permissions).

<a id="use-case-conditions"></a>

### Điều kiện và ngoại lệ dùng chung

- Mọi actor dùng ứng dụng phải có tài khoản active, email đã xác minh và phiên hợp lệ theo DEC-051. Thao tác quản lý còn đòi membership active và quyền đúng thao tác ở thời điểm thực hiện; owner cũng chịu điều kiện nền. Hệ thống lấy actor từ phiên, không nhận actor tùy ý từ client.
- Luồng chính giả định dữ liệu hợp lệ và phụ thuộc sẵn sàng. Validation, trạng thái tài nguyên và quyền đều kiểm tra phía server theo COM/ACL. Private server không được biết hoặc phòng không được xem trả 404 theo hợp đồng mục tiêu; thiếu quyền quản lý trong scope được biết trả 403. Không trả metadata/nội dung bị ẩn trong lỗi hoặc thông báo.
- Các thao tác có version dùng version đã tải; ACL dùng accessVersion, leave/gán role/ngoại lệ dùng đúng membershipId. Xung đột trả 409 và yêu cầu đọc lại hiện trạng, không tự đổi version/epoch rồi ghi đè.
- Không tự phát lại mutation sau refresh, timeout hoặc mất kết nối. Khi kết quả không rõ, tải lại trạng thái trước khi người dùng quyết định tiếp. Riêng thao tác tạo giữ clientOperationId và payload ban đầu cho lần thử lại chủ động; gửi tin giữ clientMessageId theo thiết kế DM. Cùng khóa khác payload hoặc tài nguyên đã deleted/terminal không tạo lại tài nguyên bằng khóa cũ.
- Khi transaction rollback, không để lại dữ liệu nghiệp vụ dở dang hoặc phát cập nhật như đã thành công. Khi không xác nhận được quyền, dừng thao tác/phát nội dung theo hợp đồng; lỗi phụ thuộc không trở thành quyền truy cập.
- Thu hồi chat đo từ commit, deadline ≤5 giây theo DEC-083; HTTP tiếp theo kiểm tra quyền hiện hành. Media có proof riêng theo DEC-099. Vòng đời nội dung, cleanup và bảo vệ sau restore theo [data-lifecycle.md](../../../data-lifecycle.md).

<a id="use-cases"></a>

## Use case tích hợp tin phòng và realtime

Điều kiện, version/epoch, retry và lỗi dùng chung theo [quy ước tích hợp](#use-case-conditions). Quy tắc/AC/TC áp dụng cho từng UC ở [bảng truy vết](README.md#use-case-coverage).

<a id="uc-com-23"></a>

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

## UX và trạng thái

Bộ đếm tên/mô tả/chủ đề dùng UTF-16 theo DEC-092/093 để thống nhất API; giới hạn và validation theo [COM-029](servers.md#com-029), [COM-034](channels.md#com-034), [COM-037](permissions.md#com-037), [COM-038](servers.md#com-038), [COM-039](servers.md#com-039).

Đề xuất màn hình hẹp dùng lần lượt danh sách cộng đồng → danh sách
phòng → hội thoại; quản lý mở thành trang riêng. DEC-082 đã chốt desktop
Chrome/Edge/Firefox/Safari và Chrome Android/Safari iOS cho Community;
phiên bản/OS/thiết bị/build và bằng chứng khả dụng còn cần khóa tại OQ-007.
Media v1 cam kết trên desktop, điện thoại ở đợt sau.

Vg sở hữu UI Community và quyền/lifecycle phòng; Sáng cung cấp nền Messaging và đối chiếu hợp đồng tin phòng. Thái cung cấp dataset/bộ chạy kiểm tra theo [DEC-117](../../../project.md#team). Các ca dưới đây mô tả yêu cầu; bằng chứng đã ghi nhận được dẫn chiếu từ [tiến độ](../status.md).

<a id="acceptance"></a>

## Tiêu chí chấp nhận

| Mã | Tình huống kiểm tra | Kết quả mong đợi |
|---|---|---|
| <a id="ac-com-12"></a> AC-COM-12 | Thành viên được phép nhắn gửi tin văn bản trong phòng, rồi sửa và xóa tin của mình. | Tin được lưu và thấy lại; sửa có dấu “Đã sửa”; xóa hiện “Tin nhắn đã bị xóa” cho các thành viên có quyền xem phòng. |
| <a id="ac-com-13"></a> AC-COM-13 | Thành viên cố sửa/xóa tin của người khác hoặc đọc/gửi tin ở phòng không có quyền. | Hệ thống từ chối, kể cả khi gọi trực tiếp API. |
| <a id="ac-com-18"></a> AC-COM-18 | Kết quả gửi tin phòng lần đầu không rõ, người gửi bấm thử lại cho chính tin đó. | Phòng chỉ có một tin tương ứng với thao tác gửi. |
| <a id="ac-com-22"></a> AC-COM-22 | Thành viên chưa xác minh email nhưng có quyền xem phòng thử gửi tin. | Hệ thống từ chối gửi và chỉ dẫn bước xác minh email. |
| <a id="ac-com-23"></a> AC-COM-23 | Thành viên sửa tin nhiều lần; người khác tải lại. | Chỉ nội dung hiện hành cùng dấu “Đã sửa”, không có lịch sử nội dung cũ. |
| <a id="ac-com-24"></a> AC-COM-24 | Thành viên gửi/sửa tin tại biên 2.000/2.001 UTF-16 sau chuẩn hóa xuống dòng; gửi tin chỉ trắng/vô hình hoặc Unicode lỗi. | Nhận đến 2.000 UTF-16, từ chối vượt giới hạn/tin trống/Unicode lỗi; giữ tiếng Việt, emoji và ZWJ trong tin có nội dung theo DEC-090. |


Các tiêu chí liên quan nhiều phần có một nguồn chuẩn ở thành phần chủ trì; [ma trận UC/AC/TC](README.md#use-case-coverage) dẫn tới tất cả tiêu chí cần kiểm chứng. Kết quả thực thi được quản lý trong hồ sơ nghiệm thu, dẫn chiếu từ [tiến độ](../status.md).

<a id="tests"></a>

## Ca kiểm thử

Dùng [dữ liệu và cách ghi bằng chứng chung](#evidence). Các ca bên dưới là đặc tả kiểm chứng; cần bổ sung assertion cho từng endpoint/nhánh và kiểm tra quyền bằng API.

| Mã ca | Thao tác và dữ liệu | Kết quả cần quan sát | Dẫn chiếu |
|---|---|---|---|
| <a id="tc-com-10"></a> TC-COM-10 | Gửi/sửa/xóa tin phòng; N sửa tin O bằng API; gửi lại khi mất response | Kiểm tra tác giả, tombstone, một tin mỗi thao tác; dữ liệu UTF-16 dùng chung DM | [AC-COM-12](#ac-com-12), [AC-COM-13](#ac-com-13), [AC-COM-18](#ac-com-18), [AC-COM-23](#ac-com-23), [AC-COM-24](#ac-com-24) |
| <a id="tc-com-12"></a> TC-COM-12 | U chưa xác minh giả membership và gọi API đọc/gửi | Chặn truy cập ứng dụng phía server, không suy U được đọc phòng | [AC-COM-22](#ac-com-22), DEC-051 |
| <a id="tc-com-23"></a> TC-COM-23 | POST create response mất; manual retry cùng/khác payload; resource sau đó deleted | Một resource cùng operation, payload khác conflict; không phục hồi resource deleted bằng retry | Hợp đồng operation |
| <a id="tc-com-25"></a> TC-COM-25 | Channel guard tranh role/ACL/leave/delete và send/subscribe/dispatch | Guard giữ tới commit; thu hồi chat ≤5 giây, không mất/trùng/trái quyền | [AC-COM-11](permissions.md#ac-com-11), [AC-COM-18](#ac-com-18), [AC-COM-34](channels.md#ac-com-34), [AC-COM-42](permissions.md#ac-com-42), DEC-083 |
| <a id="tc-com-26"></a> TC-COM-26 | Migration tên legacy/collision, system role/permission catalog/trigger version | Không đổi/xóa dữ liệu thật tự động; một nguồn tăng version và schema phù hợp writer | COM-SQL-01–10 |

<a id="evidence"></a>

## Dữ liệu và bằng chứng kiểm thử

Dữ liệu: O là chủ sở hữu; M được cấp một quyền quản lý cụ thể; N là thành viên thường. Chuẩn bị phòng mở/phòng giới hạn, vai trò R-allow/R-deny, người ngoài cộng đồng, lời mời hợp lệ/hết hạn/thu hồi và yêu cầu chờ duyệt. Tài khoản thử phải có trạng thái xác minh/phiên được kiểm soát.

Các tiêu chí mới dẫn tới DEC-072–077/087/092–098; đối chiếu bằng chứng theo [gói triển khai](../delivery/create-view/plan.md#first-package). Ca chat thu hồi đo ≤5 giây theo DEC-083; media thu hồi ≤5 giây/fail-close theo DEC-099, chưa có kết quả SFU.

Các ca này bao phủ quyền đã chốt; cần bổ sung ca cho từng AC-COM khi hoàn thiện backend/API. Chạy trực tiếp API để kiểm tra quyền, không chỉ nhìn nút UI. Ca nội dung tin dùng [TC-TEXT](../../direct-messaging.md#tests). Ca gửi lại/đồng thời cũng cần chạy cho tin phòng sau khi chọn hợp đồng.

Ca [AC-COM-22](#ac-com-22) kiểm tra điều kiện chưa xác minh phía máy chủ; DEC-051 vẫn chặn truy cập ứng dụng từ trước, không suy rằng người chưa xác minh được đọc phòng. Chat thu hồi đã chốt DEC-083; ca media theo DEC-099 và MEDIA-GAP ở đặc tả media vẫn “Chưa chạy”, không đánh dấu đạt. Kết quả theo [mẫu nghiệm thu](../../../release-operations.md#testing).

<a id="gaps"></a>

## Việc còn lại

Trạng thái phụ thuộc chung theo [kế hoạch triển khai](../delivery/README.md#use-case-delivery), [migration](../design/integration.md#schema-migration) và [vòng đời dữ liệu](../../../data-lifecycle.md). Các đầu vào review/mock/proof còn mở, không đánh dấu nghiệm thu từ tài liệu/fixture.

| Nội dung | Câu hỏi còn mở | Liên quan |
|---|---|---|
| Nhắn tin phòng | Dùng chung text/HMAC/sequence/cursor DM; REST/realtime đã có schema, chưa có writer/mock/proof; mất quyền không xóa nội dung, room deleted giữ chưa đặt hạn theo DEC-105; restore placeholder dùng chung schema DEC-108, còn DATA-GAP proof. | OQ-005, OQ-011 |
