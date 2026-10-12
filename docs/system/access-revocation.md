# SCDC — Thu hồi phiên và quyền truy cập

## Phạm vi

Hợp đồng xuyên Identity, Community, Messaging và Media. Chính sách tính quyền nằm tại [Community access control](../features/community/access-control.md); lifecycle phiên tại [Accounts](../features/accounts/README.md). File này giữ thời hạn hiệu lực và nghĩa vụ phối hợp.

Restore từ backup và proof tương ứng thuộc [giai đoạn sau v1](../releases/v1.md#backup-scope) theo DEC-156. Restart, tiếp tục tiến trình bền, rebuild Redis từ PostgreSQL hiện hành và kiểm chứng hàng rào/quyền tới commit vẫn thuộc v1; sổ riêng phục vụ restore không là phụ thuộc mới của v1.

## Quy tắc

| Đường truy cập | Điều kiện |
|---|---|
| HTTP mới | Kiểm tra session/account và quyền scope hiện hành |
| Mutation | Giữ quyền tới commit theo hợp đồng transaction; thay đổi quyền tranh mutation phải có thứ tự xác định |
| Subscribe/resume | Admission theo phiên, actor, scope và epoch hiện hành; cursor không cấp quyền |
| Chat đang kết nối | Ngừng định tuyến nội dung mới trong ≤5 giây từ commit thu hồi — DEC-083 |
| Media đang chạy | Ngừng cả phát/nhận RTP trong ≤5 giây từ commit; không xác nhận quyền thì dừng — DEC-099 |

Khóa tài khoản, revoke session, đổi mật khẩu, mất membership/View, xóa phòng hoặc thay role/ACL có thể kích hoạt thu hồi theo quy tắc của chủ đề. Thu hồi không xóa byte mà client đã tự sao chép.

## Trách nhiệm

- Identity là nguồn session/account/security stamp.
- Community là nguồn membership, ownership, role/ACL và access epoch/version.
- Messaging kiểm tra admission, guard writer/reader, registry/dispatch/reconnect và ngừng gửi tới connection không còn quyền.
- Media kiểm tra join/resume và thực thi lease/fencing ở đường truyền SFU. Đóng websocket không đủ chứng minh WebRTC đã ngừng.
- WebClient dọn state/draft/cache của scope mất quyền; server thực thi độc lập với client.

Guard trong một host dùng lock order Identity → Community → Messaging khi hợp đồng yêu cầu. Thiết kế writer/Hub tại [Messaging](../features/messaging/README.md), guard transaction tại [Community](community.md), coordinator/SFU tại [Media](../features/media/README.md). Shared lock không tự có hiệu lực xuyên service ở v1.

## Hiện trạng và kiểm chứng

Identity/Community có HTTP/transaction guard và bằng chứng theo gói Community. Consumer Messaging, Hub/dispatcher và proof deadline kết nối thật chưa được tích hợp; Media chưa có runtime. Các hồ sơ gói nền không chứng minh thu hồi realtime hoặc RTP.

Ca kiểm chứng phải ghi commit thu hồi, thời điểm cutoff, clock/trace, actor/scope/epoch và kết quả khi authority/worker/provider lỗi hoặc restart. Thử token/cursor/epoch cũ, tranh mutation/subscribe và reconnect. Tiêu chí chi tiết tại [tin phòng](../features/messaging/channel-messaging.md), [đồng bộ tin](../features/messaging/synchronization.md) và [Media](../features/media/README.md).

Thiết kế restore backup sau v1 phải giữ dấu thu hồi/floor và vô hiệu phiên/generation cũ trước mở truy cập theo [vòng đời dữ liệu](data-lifecycle.md#restore).

<a id="v1-session-validation"></a>

## D30 — Kiểm tra phiên tại service nghiệp vụ

Trạng thái: đã xác nhận phương án C ngày 2026-10-12 — [DEC-148](../records/decisions/README.md#dec-148). Các service xác minh JWT cục bộ rồi đọc trạng thái phiên/tài khoản tối thiểu từ Redis do Identity quản lý. Áp dụng cho HTTP cần phiên và admission kết nối/subscribe/resume.

| Nội dung | Yêu cầu |
|---|---|
| Chủ sở hữu | Identity quyết định/cập nhật trạng thái; PostgreSQL Identity giữ dữ liệu bền |
| Dữ liệu công bố | Trạng thái phiên/tài khoản cần cho xác thực, kèm hạn và phiên bản; schema cụ thể chưa chốt |
| Bên đọc | Community, Messaging và Media chỉ đọc view đã công bố bằng quyền giới hạn theo hợp đồng |
| Trạng thái chưa rõ | Thiếu bản ghi, lỗi kho hoặc phục hồi chưa đủ không chứng minh phiên hợp lệ; chưa cấp truy cập khi chưa xác minh được |
| Thu hồi | Đường đọc phải chặn phiên trước khi xác nhận thu hồi có hiệu lực; giữ hàng rào ghi và điều kiện commit tại D05 |

Lựa chọn giảm lời gọi kiểm tra phiên tới Identity API và tránh đồng bộ bản trạng thái riêng tại mỗi service. Đánh đổi là thêm Redis, lời gọi mạng và giao thức nhất quán/phục hồi; chưa có phép đo hiệu năng. Hỏi Identity cho mỗi lần kiểm tra và giữ bản trạng thái riêng đã được so sánh nhưng không được chọn cho đường kiểm tra thông thường. [D31](#v1-session-validation-fallback) chọn tạm từ chối khi chưa xác minh được qua Redis, không gọi Identity dự phòng.

[D02](database.md#v1-database-layout) được bổ sung phạm vi đọc view xác thực này; các DB PostgreSQL nghiệp vụ vẫn riêng, service khác không đọc/ghi PostgreSQL Identity hoặc ghi view Redis. Redis này chưa được chọn làm SignalR backplane, broker hoặc thành phần của LiveKit. Client Redis là truy cập hạ tầng, không đổi gRPC/mTLS giữa các service nghiệp vụ theo D04/D20; bảo vệ kết nối Redis, quyền và credentials cần thiết kế riêng.

Không suy luận phiên hợp lệ chỉ từ việc không có mục trong blacklist. Phải xác định điều kiện tin cậy/đầy đủ của trạng thái đọc; restart, nạp lại trạng thái Redis, rebuild từ PostgreSQL hiện hành, dữ liệu cũ hoặc eviction không được khôi phục quyền đã thu hồi. Restore từ backup áp dụng sau v1 và phải giữ cùng nguyên tắc. Nguồn cơ chế: [Redis — persistence](https://redis.io/docs/latest/operate/oss_and_stack/management/persistence/), [Redis — eviction](https://redis.io/docs/latest/develop/reference/eviction/).

PostgreSQL và Redis không tự có transaction chung. DB commit rồi chờ sự kiện nền D27 cập nhật Redis không tự đáp ứng HTTP hiện hành. Giao thức phối hợp thu hồi/cấp phiên/refresh, readiness/generation, chống trạng thái cũ và phục hồi cần thiết kế/kiểm chứng trước triển khai.

[D28/D29 — chữ ký và khóa công khai](api-conventions.md#v1-user-token-signing), quyền scope của Community và [D05 — quyền tới commit](#v1-authorization-coordination) giữ nguyên. Kết quả đọc Redis không tự cấp permit ghi hoặc chứng minh cutoff chat/media; không gọi Redis cho từng frame RTP. Worker thu hồi/bù/dọn tiếp tục dùng danh tính service theo D20.

Schema, blacklist/allowlist cụ thể, TTL, xử lý phiên hết hạn tự nhiên, lỗi/deadline, quyền RPC/context actor và reader/dispatcher/SFU còn mở; không thay thời hạn phiên DEC-065. Redis tự host cùng máy phát triển/demo trong container riêng do Compose quản lý theo [D32](environments.md#v1-auth-redis-deployment). Version, persistence, eviction, tài nguyên và HA chưa chọn. Chưa đổi code, SQL, khóa hoặc cấu hình.

<a id="v1-session-validation-fallback"></a>

## D31 — Xác minh phiên dự phòng

Trạng thái: đã xác nhận phương án A ngày 2026-10-12 — [DEC-149](../records/decisions/README.md#dec-149). Khi lỗi kết nối, thiếu trạng thái hoặc Redis chưa đủ tin cậy để xác minh phiên, tạm từ chối yêu cầu cần xác thực; không gọi Identity dự phòng.

| Tình huống | Cách xử lý |
|---|---|
| Chưa xác minh được qua Redis | Phản hồi tạm thời không thể xử lý yêu cầu; chờ kho phục vụ lại đúng hợp đồng |
| Đã xác định phiên không hợp lệ | Từ chối theo hợp đồng phiên; không tìm nguồn khác để ghi đè kết quả |
| Redis phục vụ lại | Yêu cầu tiếp theo kiểm tra lại JWT, phiên và quyền hiện hành; không dùng kết quả hợp lệ cũ để bỏ qua kiểm tra |

Lựa chọn giảm nhánh xử lý và tránh dồn lượt kiểm tra sang Identity khi Redis lỗi. Đánh đổi là lỗi riêng Redis có thể gián đoạn truy cập cần xác thực. Phương án B gọi Identity dự phòng đã được so sánh nhưng không được chọn. Nguyên tắc hạn chế lời gọi tới phụ thuộc đang lỗi được mô tả tại [Microsoft — Circuit Breaker](https://learn.microsoft.com/en-us/azure/architecture/patterns/circuit-breaker); chưa chọn thư viện/cấu hình cụ thể.

Lỗi phụ thuộc chưa chứng minh phiên không hợp lệ và không tự thu hồi phiên. Cần phân biệt lỗi tạm thời này với kết quả xác thực bị từ chối; mã lỗi và hành vi UI cụ thể chưa chốt. Không cho truy cập khi chưa xác minh được, không tự chuyển sang JWT đơn thuần hoặc bỏ qua Redis. Việc phục hồi phải đáp ứng [D30](#v1-session-validation); kết quả xác minh vẫn phải phối hợp D05 và quyền Community.

D31 áp dụng cho kiểm tra phiên khi tiếp nhận truy cập theo D30, gồm HTTP cần phiên và admission kết nối/subscribe/resume. Không thay cutoff kết nối đang mở, hợp đồng xử lý mutation đã được nhận hoặc tự chọn cách thử lại. Ánh xạ lỗi/UI, timeout, giới hạn lời gọi Redis, điều kiện phục hồi và proof còn cần thiết kế. Chưa đổi code, hợp đồng máy đọc hoặc cấu hình.

<a id="v1-authorization-coordination"></a>

## D05 — Phối hợp quyền khi ghi dữ liệu

Trạng thái: đã xác nhận phương án A ngày 2026-10-11 — [DEC-123](../records/decisions/README.md#dec-123). Chọn hàng rào ghi bền tại database từng service để phối hợp mutation và thu hồi quyền khi tách khỏi MVP. gRPC/Protobuf đã chọn tại [D04](api-conventions.md#v1-internal-protocol) là giao thức giao tiếp; cơ chế phối hợp được mô tả dưới đây.

Yêu cầu giữ nguyên: quyền hợp lệ tới commit; mutation và thu hồi có thứ tự xác định. Hạn ngừng chat/media ≤5 giây vẫn tính từ commit thu hồi có hiệu lực. Việc ghi nhận ý định thu hồi chưa được coi là thu hồi đã hoàn tất; không đổi tên một commit đã thay quyền thành “đang chờ” để dời mốc đo.

### Cơ chế đã chọn

Authority là service sở hữu trạng thái phiên/quyền: Identity hoặc Community. Owner là service có database chứa thao tác ghi cần được bảo vệ. Gate là bản ghi kiểm soát quyền ghi; epoch là phiên bản quyền dùng để từ chối quyền cũ.

1. Identity/Community lưu ý định thu hồi riêng và chặn cấp quyền mới cho scope bị ảnh hưởng. Cấp quyền và bắt đầu thu hồi phải được tuần tự hóa; policy hiện hành chưa được chốt thành policy đã thu hồi ở bước này.
2. Mỗi DB owner có gate/epoch bền. Mọi mutation liên quan kiểm tra gate và giữ khóa trong cùng transaction ghi tới commit. Lệnh cài hàng rào dùng khóa xung đột: mutation đã giữ khóa kết thúc trước hàng rào; mutation mang quyền cũ đến sau hàng rào bị từ chối.
3. Owner chỉ xác nhận sau khi transaction cài hàng rào đã commit. Authority chỉ commit thu hồi có hiệu lực sau xác nhận của tất cả owner có thể ghi trong scope đó. Xác nhận đã nhận RPC hoặc sự kiện chưa đủ.
4. Mất liên lạc hoặc chưa rõ kết quả thì giữ ý định ở trạng thái chưa hoàn tất và chặn cấp quyền mới cho scope liên quan. Timeout/TTL không chứng minh writer đã rollback. Recovery phải lặp an toàn và không làm lùi epoch/hàng rào đã bền.

Đây là thiết kế suy luận dựa trên cơ chế khóa cục bộ của [PostgreSQL](https://www.postgresql.org/docs/18/explicit-locking.html#LOCKING-ROWS), chưa có implementation hoặc proof xuyên service. Các owner phải thực thi gate ở đường ghi thực tế; một token hoặc kiểm tra trong bộ nhớ không thay được gate trong transaction.

Đánh đổi: cơ chế này dùng transaction cục bộ, không phụ thuộc prepared transactions xuyên DB để giữ quyền. Ứng dụng phải quản lý epoch, hàng rào, xác nhận và phục hồi; thu hồi có thể chờ khi owner không liên lạc được. RPC bị hủy không tự chứng minh công việc hoặc transaction đã dừng; trách nhiệm phía ứng dụng được nêu tại [gRPC deadlines](https://grpc.io/docs/guides/deadlines/).

[D06](#v1-revocation-coordinator) xác định service chịu trách nhiệm điều phối. Schema/gate/permit, danh sách owner, thứ tự nhiều authority, endpoint `.proto`, retry/deadline, phản hồi API/UI khi chưa hoàn tất và cơ chế atomicity cho nghiệp vụ ghi nhiều DB còn mở. Quyền của người yêu cầu khi tiến trình thu hồi còn chạy, hạn phiên tự nhiên, reader/dispatcher và cutoff tại SFU tiếp tục cần thiết kế/kiểm chứng theo hợp đồng riêng.

Trước triển khai v1 cần proof writer bị dừng rồi chạy lại, mất ACK sau commit, authority/owner restart, lỗi mạng từng phần và quyền cũ sau fencing; restart phải đọc đúng gate/epoch bền tại owner DB, không làm lùi hàng rào. Proof restore từ backup không làm lùi hàng rào áp dụng sau v1. Chọn D05 chưa thay các tiêu chí nghiệm thu hoặc chứng minh cutoff realtime/media.

<a id="v1-revocation-coordinator"></a>

## D06 — Chủ thể điều phối thu hồi

Trạng thái: đã xác nhận phương án A ngày 2026-10-11 — [DEC-124](../records/decisions/README.md#dec-124). Mỗi service sở hữu phiên/quyền chịu trách nhiệm theo dõi tiến trình, gửi yêu cầu cài hàng rào, thu thập xác nhận, thử lại và phục hồi theo D05.

| Service | Phạm vi điều phối | Trạng thái bền |
|---|---|---|
| Identity | Thu hồi phiên/tài khoản; ví dụ đăng xuất mọi thiết bị | Ý định, tiến độ xác nhận và phục hồi trong DB Identity |
| Community | Thu hồi quyền cộng đồng theo membership/role/ACL/kênh; ví dụ xóa role làm mất quyền phòng | Ý định, tiến độ xác nhận và phục hồi trong DB Community |

Mỗi authority tự ghi ý định thu hồi, chặn cấp quyền mới và commit thay đổi phiên/quyền trong database của mình. Từng owner vẫn tự cài hàng rào trong transaction cục bộ; điều phối không cấp quyền truy cập database service khác.

Lợi ích là tiến trình và trạng thái quyền thuộc cùng service, giảm bước bàn giao giữa các chủ sở hữu. Đánh đổi là cần triển khai và kiểm chứng phục hồi ở cả Identity và Community. Hai service có thể dùng chung quy tắc hoặc thư viện; cấu trúc code sẽ được thiết kế riêng. Tham khảo trách nhiệm lưu trạng thái và phục hồi của bộ điều phối tại [Microsoft — orchestration](https://learn.microsoft.com/en-us/azure/architecture/patterns/saga#orchestration); D05 vẫn cần giao thức hàng rào riêng.

Thu hồi Identity/Community đồng thời phải được xử lý mà không chờ vòng. Lệnh điều phối xác thực danh tính service bằng mTLS theo [D20](api-conventions.md#v1-service-authentication), độc lập với quyền của người dùng đang bị thu hồi; quyền gọi RPC và context actor cần thiết kế chi tiết. Công việc tiếp tục/đối soát tiến trình chạy cùng API của authority theo [D12](backend.md#v1-application-workers); mỗi service ban đầu có một tiến trình API hoạt động theo [D21](environments.md#v1-service-replicas). Số luồng xử lý công việc, claim/retry, mở rộng sau, schema, thư viện và hợp đồng `.proto` cần thiết kế chi tiết. [D27](api-conventions.md#v1-integration-event-transport) chọn outbox/worker/gRPC cho sự kiện sau commit, chưa thêm broker ban đầu. Giao thức và proof phục hồi còn mở; source chưa triển khai cơ chế này.

<a id="v1-revocation-transport"></a>

## D07 — Truyền lệnh và xác nhận thu hồi

Trạng thái: đã xác nhận phương án A ngày 2026-10-11 — [DEC-126](../records/decisions/README.md#dec-126). Identity/Community gọi trực tiếp owner bằng gRPC + Protobuf theo [D04](api-conventions.md#v1-internal-protocol) để cài hàng rào và nhận kết quả. Tiến trình vẫn được lưu bền tại authority theo D06; điều kiện commit thu hồi theo D05 giữ nguyên.

- Authority lưu ý định và tiến độ trong DB của mình, gửi yêu cầu tới từng owner liên quan.
- Owner trả xác nhận sau khi transaction cài hàng rào đã commit. Authority lưu xác nhận gắn với đúng đợt thu hồi; chỉ chốt thu hồi khi đủ xác nhận theo D05.
- Mất phản hồi, timeout hoặc owner tạm ngừng: authority giữ tiến trình chưa hoàn tất, tiếp tục đối soát/thử lại an toàn từ trạng thái bền. Không coi lỗi mạng là bằng chứng owner chưa commit; không làm lùi hàng rào đã cài.

Ví dụ: Community cần cài hàng rào tại Messaging và Media. Community gọi hai owner và lưu kết quả. Nếu chỉ Messaging xác nhận, tiến trình vẫn chưa hoàn tất cho tới khi có xác nhận hợp lệ của Media; authority restart phải tiếp tục được tiến trình đó.

Lựa chọn tận dụng gRPC và tiến trình bền đã có trong thiết kế, không cần hàng đợi riêng cho lệnh hàng rào. Đánh đổi là từng lần gọi cần owner đáp ứng; thu hồi có thể chờ khi mất liên lạc. Deadline/hủy RPC không tự chứng minh transaction đã rollback; ứng dụng phải quản lý công việc phía server theo [gRPC deadlines](https://grpc.io/docs/guides/deadlines/).

D07 chỉ chốt đường truyền lệnh hàng rào và kết quả. Danh tính service dùng mTLS theo [D20](api-conventions.md#v1-service-authentication). Schema `.proto`, cấu hình xác thực và quyền RPC, thời hạn/lịch thử lại và phản hồi API/UI khi chưa hoàn tất cần thiết kế riêng; cách host công việc nền theo [D12](backend.md#v1-application-workers). Gọi gRPC không yêu cầu giữ request HTTP ban đầu mở suốt tiến trình. Sự kiện sau commit theo [D27](api-conventions.md#v1-integration-event-transport) không thay xác nhận cài hàng rào của D07; proof thu hồi/restart/cutoff vẫn còn mở.

[D08 — tiếp nhận tin](../features/messaging/message-lifecycle.md#v1-message-intake) chọn ghi trực tiếp và giữ khả năng chuyển sang hàng đợi cho luồng gửi tin; đây là quyết định riêng với D07.
