# SCDC — Thu hồi phiên và quyền truy cập

Khi một người bị đăng xuất, bị khóa tài khoản, rời server hay mất quyền xem phòng, hệ thống phải cắt mọi đường truy cập của họ: request HTTP mới, thao tác ghi, kết nối chat và media đang mở. Trang này mô tả yêu cầu thời gian, trách nhiệm của từng service, và thiết kế v1 khi các service tách rời nhau.

**Trạng thái:** S2-07 bổ sung transaction riêng và coordinator policy Community ([hồ sơ](../records/verification/architecture/s2-07-2026-11-23.md)); S2-06 đã triển khai Redis, permit Identity và coordinator thu hồi trong process; kết quả kiểm chứng tại [hồ sơ S2-06](../records/verification/authentication/s2-06-2026-11-23.md). MVP đã có thu hồi cho DM (một ứng dụng duy nhất). Thiết kế triển khai S2-02 đã có [bên dưới](#s2-02-protocol); các adapter, migration và kiểm chứng runtime thuộc S2-03 đến S2-11.

Cách tính quyền xem [quyền truy cập Community](../features/community/access-control.md); vòng đời phiên xem [tài khoản](../features/accounts/README.md).

## Yêu cầu

| Đường truy cập | Yêu cầu |
|---|---|
| Request HTTP mới | Kiểm tra phiên, tài khoản và quyền hiện tại |
| Thao tác ghi | Quyền phải còn hiệu lực tới lúc commit. Nếu đổi quyền và thao tác ghi xảy ra cùng lúc, phải có thứ tự rõ ràng |
| Đăng ký nhận, bù tin | Kiểm tra phiên, người dùng, phạm vi và epoch hiện tại. Cursor không cấp quyền |
| Chat đang kết nối | Ngừng gửi nội dung mới trong **5 giây** kể từ commit thu hồi (DEC-083) |
| Media đang chạy | Ngừng cả gửi và nhận luồng media trong **5 giây** kể từ commit; không xác nhận được quyền thì dừng (DEC-099) |

Các sự kiện có thể gây thu hồi: khóa tài khoản, thu hồi phiên, đổi mật khẩu, mất tư cách thành viên hay quyền xem, xóa phòng, đổi vai trò hay ACL. Thu hồi không xóa được dữ liệu mà client đã tự sao chép.

## Trách nhiệm

| Thành phần | Trách nhiệm |
|---|---|
| Identity | Nguồn dữ liệu về phiên, tài khoản, security stamp |
| Community | Nguồn dữ liệu về thành viên, chủ sở hữu, vai trò, ACL, epoch và phiên bản quyền |
| Messaging | Kiểm tra khi đăng ký nhận; guard khi đọc và ghi; registry, gửi sự kiện, kết nối lại; ngừng gửi tới kết nối mất quyền |
| Media | Kiểm tra khi vào và vào lại; dùng lease và fencing trên LiveKit. Đóng websocket không chứng minh WebRTC đã ngừng |
| WebClient | Xóa trạng thái, bản nháp, dữ liệu đệm của phạm vi bị mất quyền. Server vẫn tự thực thi, không phụ thuộc client |

Trong MVP (một ứng dụng), guard khóa theo thứ tự Identity → Community → Messaging khi cần. Ở v1, khóa này không còn tác dụng giữa các service, nên cần thiết kế D05 bên dưới.

Chi tiết: ghi tin và Hub xem [Messaging](../features/messaging/README.md); guard trong transaction xem [Community](../features/community/mechanics.md); điều phối và LiveKit xem [Media](../features/media/README.md).

## Hiện trạng

- Nền `900e226` đã có guard HTTP và transaction cho Identity và Community. DM dùng `IAccountPairAccessGuard`, ChatHub, worker outbox và `SubscriptionReconciler`.
- Bằng chứng thu hồi phiên DM: [DM-MVP](../records/verification/messaging/DM-MVP.md). Luồng DM realtime và lịch sử: [hồ sơ hợp nhất](../records/verification/messaging/merge-2026-10-13.md).
- Chưa có: ghi và nhận tin phòng, cắt kết nối khi đổi thành viên, vai trò hay ACL; Media; thu hồi giữa các service v1. Bằng chứng DM trên một ứng dụng không thay cho bằng chứng v1.

### Cách kiểm thử

Mỗi ca ghi: thời điểm commit thu hồi, thời điểm ngắt, cách đồng bộ đồng hồ và trace, người dùng, phạm vi, epoch, và kết quả khi thành phần kiểm tra quyền, worker hay LiveKit bị lỗi hoặc khởi động lại. Thử dùng lại token, cursor, epoch cũ; thử đổi quyền cùng lúc với ghi hoặc đăng ký; thử kết nối lại. Tiêu chí chi tiết xem [tin phòng](../features/messaging/channel-messaging.md), [đồng bộ tin](../features/messaging/synchronization.md) và [Media](../features/media/README.md).

### Backup và khởi động lại

- **Thuộc v1:** khởi động lại, tiếp tục tiến trình đang dở, dựng lại Redis từ PostgreSQL hiện tại; không được làm sống lại quyền đã thu hồi.
- **Sau v1:** khôi phục từ backup phải giữ dấu thu hồi và vô hiệu phiên cũ trước khi mở truy cập ([vòng đời dữ liệu](data-lifecycle.md#restore), DEC-156).

---

## Thiết kế v1: thu hồi khi ghi dữ liệu

Khi tách service, mỗi service có database riêng, nên không thể dùng một khóa chung để giữ quyền tới commit. Ba quyết định D05, D06, D07 giải quyết việc này.

**Thuật ngữ:**

- **Authority:** service sở hữu phiên hoặc quyền (Identity hoặc Community).
- **Owner:** service có database chứa thao tác ghi cần bảo vệ (ví dụ Messaging cho tin nhắn).
- **Hàng rào (gate):** bản ghi trong DB của owner, dùng để chặn thao tác mang quyền cũ.
- **Epoch:** số phiên bản quyền; thao tác mang epoch cũ bị từ chối.

<a id="v1-authorization-coordination"></a>

### D05 — Hàng rào ghi tại database từng service

**Quyết định:** mỗi owner có hàng rào bền trong DB của mình. Thu hồi chỉ được coi là có hiệu lực khi mọi owner liên quan đã cài hàng rào.

**Các bước:**

1. Authority lưu ý định thu hồi và chặn cấp quyền mới cho phạm vi đó. Việc cấp quyền và bắt đầu thu hồi được xếp hàng tuần tự. Ở bước này quyền chưa bị coi là đã thu hồi.
2. Mọi thao tác ghi ở owner kiểm tra hàng rào và giữ khóa trong cùng transaction tới khi commit. Lệnh cài hàng rào dùng khóa xung đột với thao tác ghi: thao tác đang giữ khóa xong trước; thao tác mang quyền cũ đến sau bị từ chối.
3. Owner chỉ xác nhận sau khi transaction cài hàng rào đã commit (nhận được RPC hay sự kiện chưa đủ). Authority chỉ commit thu hồi khi đã có xác nhận của **tất cả** owner có thể ghi trong phạm vi đó.
4. Mất liên lạc hoặc chưa rõ kết quả: giữ ý định ở trạng thái chưa xong và tiếp tục chặn cấp quyền mới. Hết thời gian chờ không chứng minh thao tác ghi đã rollback. Phục hồi phải chạy lặp lại an toàn và không bao giờ làm lùi epoch hay hàng rào.

**Giữ nguyên:** quyền có hiệu lực tới commit; thời hạn 5 giây cho chat và media vẫn tính từ commit thu hồi. Không được gọi một commit đã đổi quyền là "đang chờ" để lùi mốc đo.

**Đánh đổi:** chỉ dùng transaction cục bộ, không cần transaction phân tán. Đổi lại ứng dụng phải tự quản lý epoch, hàng rào, xác nhận và phục hồi; thu hồi có thể phải chờ khi một owner mất liên lạc. RPC bị hủy không chứng minh công việc phía server đã dừng ([gRPC deadlines](https://grpc.io/docs/guides/deadlines/)).

Thiết kế dựa trên cơ chế khóa dòng của [PostgreSQL](https://www.postgresql.org/docs/18/explicit-locking.html#LOCKING-ROWS); chưa có code hay bằng chứng giữa các service. Hàng rào phải nằm trên đường ghi thật; token hay kiểm tra trong bộ nhớ không thay được.

<a id="v1-revocation-coordinator"></a>

### D06 — Ai điều phối

**Quyết định:** service sở hữu phiên hoặc quyền tự điều phối thu hồi của mình: theo dõi tiến trình, gửi lệnh cài hàng rào, thu thập xác nhận, thử lại và phục hồi.

| Service | Điều phối | Lưu tiến trình |
|---|---|---|
| Identity | Thu hồi phiên và tài khoản, ví dụ đăng xuất mọi thiết bị | DB Identity |
| Community | Thu hồi quyền theo thành viên, vai trò, ACL, phòng, ví dụ xóa vai trò làm mất quyền phòng | DB Community |

Mỗi owner vẫn tự cài hàng rào trong transaction của mình; điều phối không cho quyền truy cập DB của service khác.

**Đánh đổi:** tiến trình nằm cùng chỗ với dữ liệu quyền, ít bước bàn giao. Đổi lại phải làm và kiểm thử phục hồi ở cả Identity và Community (có thể dùng chung thư viện). Tham khảo [mẫu orchestration](https://learn.microsoft.com/en-us/azure/architecture/patterns/saga#orchestration).

Ghi chú:

- Thu hồi đồng thời ở Identity và Community không được chờ vòng lẫn nhau.
- Lệnh điều phối xác thực bằng danh tính service qua mTLS ([D20](api-conventions.md#v1-service-authentication)), không dùng quyền của người đang bị thu hồi.
- Worker tiếp tục tiến trình chạy cùng API của authority ([D12](backend.md#v1-application-workers)), mỗi service một bản ([D21](environments.md#v1-service-replicas)).
- Sự kiện sau commit dùng outbox, worker và gRPC, chưa có broker ([D27](api-conventions.md#v1-integration-event-transport)).

<a id="v1-revocation-transport"></a>

### D07 — Truyền lệnh và xác nhận

**Quyết định:** authority gọi trực tiếp từng owner bằng gRPC ([D04](api-conventions.md#v1-internal-protocol)) để cài hàng rào và nhận kết quả. Tiến trình lưu bền tại authority.

- Authority lưu ý định và tiến độ, gửi lệnh tới từng owner.
- Owner trả xác nhận sau khi đã commit hàng rào. Authority lưu xác nhận gắn với đúng lần thu hồi.
- Mất phản hồi, hết thời gian hoặc owner tạm ngừng: giữ tiến trình chưa xong, tiếp tục đối chiếu và thử lại từ trạng thái đã lưu. Lỗi mạng không chứng minh owner chưa commit.

**Ví dụ:** Community cần cài hàng rào ở Messaging và Media. Nếu chỉ Messaging xác nhận, tiến trình vẫn chưa xong cho tới khi Media xác nhận. Nếu Community khởi động lại, nó phải tiếp tục được tiến trình đó.

**Đánh đổi:** không cần hàng đợi riêng cho lệnh hàng rào. Đổi lại mỗi lần gọi cần owner trả lời; thu hồi có thể phải chờ khi mất liên lạc. Request HTTP ban đầu không cần giữ mở suốt tiến trình.

Sự kiện sau commit (D27) không thay cho xác nhận cài hàng rào. Việc nhận tin dùng quyết định riêng: [D08](../features/messaging/message-lifecycle.md#v1-message-intake).

## Thiết kế v1: kiểm tra phiên

<a id="v1-session-validation"></a>

### D30 — Xác minh JWT tại chỗ, kiểm tra phiên qua Redis

**Quyết định:** mỗi service tự xác minh chữ ký JWT, rồi đọc trạng thái phiên và tài khoản tối thiểu từ một Redis do Identity quản lý. Áp dụng cho request HTTP cần phiên và khi mở kết nối, đăng ký nhận, bù tin.

| Mục | Yêu cầu |
|---|---|
| Chủ sở hữu | Identity quyết định và cập nhật; PostgreSQL của Identity giữ dữ liệu bền |
| Dữ liệu trong Redis | Trạng thái phiên và tài khoản cần cho xác thực, kèm hạn và phiên bản theo [S2-02](#s2-02-protocol) |
| Bên đọc | Community, Messaging, Media chỉ đọc, với quyền giới hạn |
| Không chắc chắn | Thiếu bản ghi, Redis lỗi hoặc đang phục hồi không chứng minh phiên hợp lệ; chưa xác minh được thì không cho truy cập |
| Thu hồi | Redis phải chặn phiên trước khi thu hồi được xác nhận là có hiệu lực; vẫn giữ hàng rào ghi của D05 |

**Lý do:** giảm số lần gọi Identity API và tránh mỗi service giữ một bản trạng thái riêng. **Đổi lại:** thêm Redis, lời gọi mạng, và cần giao thức nhất quán và phục hồi. Chưa đo hiệu năng.

**Nguyên tắc:**

- Không coi phiên hợp lệ chỉ vì không có trong danh sách đen.
- Khởi động lại, nạp lại Redis, dựng lại từ PostgreSQL, dữ liệu cũ hay bị đẩy ra khỏi bộ nhớ (eviction) không được làm sống lại quyền đã thu hồi. Tham khảo: [persistence](https://redis.io/docs/latest/operate/oss_and_stack/management/persistence/), [eviction](https://redis.io/docs/latest/develop/reference/eviction/).
- PostgreSQL và Redis không có transaction chung. Commit DB rồi chờ sự kiện nền cập nhật Redis không đủ cho request HTTP ngay sau đó; cần giao thức phối hợp riêng cho cấp phiên, làm mới token và thu hồi.
- Kết quả đọc Redis không cấp quyền ghi và không chứng minh đã cắt chat hay media. Không gọi Redis cho từng gói media.

**Ranh giới:** đây là phần bổ sung cho [D02](database.md#v1-database-layout): service khác chỉ đọc Redis, không đọc hay ghi PostgreSQL của Identity. Redis này không dùng làm backplane SignalR, broker hay Redis của LiveKit. Kết nối Redis là truy cập hạ tầng, không thay gRPC và mTLS giữa các service. Chữ ký và khóa công khai theo [D28, D29](api-conventions.md#v1-user-token-signing); thời hạn phiên vẫn theo DEC-065. Redis tự host trong container riêng ([D32](environments.md#v1-auth-redis-deployment)).

<a id="v1-session-validation-fallback"></a>

### D31 — Khi Redis không trả lời được

**Quyết định:** nếu không xác minh được phiên qua Redis (lỗi kết nối, thiếu dữ liệu, chưa đủ tin cậy) thì tạm từ chối request. Không gọi Identity để dự phòng.

| Tình huống | Xử lý |
|---|---|
| Chưa xác minh được qua Redis | Trả lỗi tạm thời; chờ Redis phục vụ lại |
| Đã xác định phiên không hợp lệ | Từ chối theo hợp đồng phiên; không tìm nguồn khác để ghi đè |
| Redis hoạt động lại | Request tiếp theo kiểm tra lại JWT, phiên và quyền từ đầu; không dùng kết quả cũ |

**Lý do:** ít nhánh xử lý hơn, và không dồn tải sang Identity khi Redis lỗi. **Đổi lại:** Redis lỗi sẽ làm gián đoạn mọi truy cập cần đăng nhập. Tham khảo [mẫu Circuit Breaker](https://learn.microsoft.com/en-us/azure/architecture/patterns/circuit-breaker).

- Lỗi tạm thời này khác với "phiên bị từ chối", và không tự thu hồi phiên.
- Không chuyển sang chỉ kiểm tra JWT hay bỏ qua Redis.
- D31 chỉ áp dụng khi tiếp nhận truy cập; không thay cho việc cắt kết nối đang mở hay xử lý thao tác ghi đã nhận.

<a id="s2-02-protocol"></a>

## S2-02 — Hợp đồng thay transaction chung

**Trạng thái:** thiết kế triển khai ngày 2026-11-20. STEP-2 dùng adapter trong process, connection riêng theo module; STEP-3 thay adapter bằng gRPC/mTLS. Hợp đồng máy đọc nằm ở [Identity](../contracts/grpc/identity/access.proto), [Community](../contracts/grpc/community/access.proto), [Messaging](../contracts/grpc/messaging/coordination.proto). Các file này chưa có host RPC hay mã sinh trong solution.

### Phạm vi và điều kiện của từng thao tác

Identity cấp điều kiện tài khoản và phiên; Community cấp điều kiện thành viên và quyền; owner tự kiểm tra dữ liệu nghiệp vụ. User ID, session ID và stamp lấy từ principal đã xác minh hoặc tiến trình bền đã được chấp nhận, không lấy từ body của người dùng. Stamp và token chỉ đi qua bộ nhớ, không sao chép vào bảng Messaging.

| Mục đích | Điều kiện Identity | Điều kiện Community | Owner tự kiểm tra |
|---|---|---|---|
| Đọc/mở DM đã có, hộp thư, đăng ký/bù tin DM | Tài khoản và phiên người đọc hợp lệ | Không có | Đúng người tham gia và không gian còn cho truy cập |
| Tạo DM mới, gửi DM mới | Như trên; tài khoản peer còn đủ điều kiện | Không có | Cặp DM, tư cách, không gian, chống trùng |
| Gửi lại DM đã lưu | Chỉ người thực hiện; không đòi peer hoạt động | Không có | Cùng mã và fingerprint; trả tin hiện tại, không tạo outbox |
| Sửa/xóa DM | Chỉ người thực hiện | Không có | Đúng tác giả, version, trạng thái tin |
| Đọc/gửi/đăng ký/bù tin phòng | Tài khoản, phiên, email đã xác minh | Đúng membership hiện tại và quyền xem phòng | Đúng không gian chữ, phòng đã sẵn sàng; gửi mới có chống trùng |
| Sửa/xóa tin phòng | Như trên | Còn quyền xem phòng | Đúng tác giả và version; chủ server không sửa tin người khác |
| Tạo/join server | Tài khoản và phiên hợp lệ | Luật public/invite/duyệt do Community kiểm tra cục bộ; không đòi membership trước khi join | Chống trùng, tên, hạn mức và trạng thái tài nguyên |
| Quản lý Community, tạo phòng | Tài khoản và phiên hợp lệ | Đúng quyền quản lý; thao tác lên phòng có sẵn còn cần quyền xem | Version, đối tượng thuộc server; tạo phòng mới không cần quyền xem phòng chưa có |
| Media (khi được bật) | Phiên người tham gia; gọi riêng kiểm tra cả hai phiên đã nhận cuộc gọi | Phòng thoại cần membership, quyền xem và phòng đang hoạt động | Lượt tham gia, nguồn phát, hạn mức, thế hệ và bằng chứng LiveKit |

Messaging phân loại gửi DM theo hai lượt: lấy giấy phép `DM_REPLAY` để tra khóa thao tác; có bản ghi thì trả trạng thái hiện tại. Không có thì kết thúc transaction đó, lấy `DM_SEND_NEW` có peer, mở transaction mới và tra lại khóa trước khi ghi. Request khác tạo tin trong khoảng giữa hai lượt vẫn chỉ cho một kết quả. Không giữ transaction trong lúc lấy giấy phép.

Mở DM cũng tách tra cứu cuộc đã có bằng `DM_READ` và tạo mới bằng `DM_OPEN_NEW`. Nếu cặp xuất hiện trong lúc đổi lượt hoặc sau unique conflict thì kết thúc transaction tạo, lấy lại quyền đọc đúng conversation và trả cuộc đã có. Peer bị khóa chỉ chặn tạo mới, không biến cuộc trò chuyện cũ thành không truy cập được. ID tra trước transaction chỉ là gợi ý, phải kiểm tra lại tư cách và không gian dưới khóa owner.

### Khóa phạm vi và nơi thực thi

`ScopeRef` chỉ có các trường của dòng tương ứng; trường ngoài phạm vi phải rỗng. UUID dùng chuỗi chuẩn viết thường; epoch/generation/version nằm trong 1 đến Int64.MaxValue, giá trị enum UNSPECIFIED bị từ chối. Tràn epoch thì dừng thay vì quay về 0. Không dùng `accessVersion` của dữ liệu hiển thị thay cho epoch bảo vệ.

| Phạm vi | Khóa | Authority | Ảnh hưởng |
|---|---|---|---|
| `ACCOUNT` | user ID | Identity | Mọi phiên của người đó; xuất hiện thêm như peer trong giấy phép gửi DM mới |
| `SESSION` | user ID, session ID | Identity | Đúng phiên, không làm mất phiên khác hoặc chặn người khác gửi tới tài khoản còn hoạt động |
| `MEMBERSHIP` | server ID, user ID, membership ID | Community | Đúng lần tham gia; rời rồi vào lại dùng membership ID mới |
| `CHANNEL_ACCESS` | server ID, channel ID, user ID, membership ID | Community | Xem/gửi/nhận trong một phòng |
| `MANAGEMENT` | server ID, user ID, membership ID, permission code | Community | Đúng quyền quản lý; mất quyền này không tự làm mất quyền xem |
| `CHANNEL_LIFECYCLE` | server ID, channel ID | Community | Mọi người dùng của phòng, đặc biệt khi xóa |

`ACCOUNT`/`SESSION` được kiểm tra tại Community và Messaging; Media tham gia khi có đường ghi thật. `MEMBERSHIP`/`CHANNEL_ACCESS`/`CHANNEL_LIFECYCLE` được thực thi tại Community, Messaging và Media khi bật. `MANAGEMENT` do Community thực thi; việc provision đã được Community chấp nhận là lệnh hệ thống, không cấp quyền quản lý cho Messaging.

Registry cấu hình lưu `routingGeneration` và danh sách owner đang bảo vệ mỗi loại phạm vi. STEP-2 có Community/Messaging, không tạo ACK giả cho Media. Owner mới phải nạp snapshot epoch và ý định đang dở, được authority đưa vào registry rồi mới nhận traffic. Đổi registry phải dừng cấp giấy phép, hoàn tất các tiến trình cũ và đồng bộ lại; không bỏ một owner chỉ vì nó đang lỗi. Mỗi ý định lưu cố định generation và toàn bộ owner cần ACK, gồm owner chưa có dữ liệu của người dùng đó.

### Giấy phép và hàng rào cục bộ

`AcquirePermit` chỉ được gọi từ adapter tin cậy của owner. Owner nhận giấy phép trực tiếp từ authority qua kênh xác thực, giữ như giá trị bất biến trong bộ nhớ; không nhận giấy phép do HTTP client gửi, không tin giấy phép Identity được service khác tự dựng hoặc chuyển tiếp. Giấy phép gắn owner, actor, mục đích, tài nguyên, peer khi cần, operation ID, generation và vector scope/epoch. Mỗi authority chỉ cấp scope của mình; owner phải có đủ cả hai giấy phép khi nghiệp vụ cần.

`HTTP_CALLER` yêu cầu stamp từ JWT đã xác minh. `RESUMING_ACCEPTED_OPERATION` chỉ cho Community tiếp tục operation bền với purpose COMMUNITY_MANAGE hoặc CHANNEL_CREATE: lấy user/session/membership/operation ID gốc từ DB, không thay bằng phiên mới của actor, không gửi stamp hoặc token đã lưu. Identity đọc lại chính session gốc và trạng thái hiện tại; Community kiểm tra lại quyền hiện tại. Mode này không cho worker tự tạo DM/tin mới và không bỏ qua phiên hết hạn hay đã thu hồi. Adapter không lấy mode/context từ HTTP body.

Request quản lý dùng đúng một trong năm code hiện hành: `manage_channels`, `manage_invites`, `review_join_requests`, `manage_join_mode`, `manage_channel_access`. Thao tác chỉ chủ server được làm dùng code phối hợp `server_owner`; code này không phải quyền có thể gán vào role. Target/member/resource phải thuộc đúng server; scope dư, thiếu, trùng hoặc khác issuer/owner/mục đích của request đều bị từ chối.

`ACCOUNT_READ` không cấp phép ghi. Mọi permit Identity của actor chứa ACCOUNT và SESSION; `DM_OPEN_NEW/DM_SEND_NEW` thêm ACCOUNT của peer, còn `DM_READ/DM_REPLAY/DM_EDIT_OWN/DM_DELETE_OWN` không thêm peer. Permit Community cho phòng chứa MEMBERSHIP, CHANNEL_ACCESS và CHANNEL_LIFECYCLE; quản lý thêm MANAGEMENT của đúng permission code, authority tính cả quyền chủ server khi cấp scope đó. Tạo phòng chỉ cần MEMBERSHIP và MANAGEMENT cho `manage_channels`, không có lifecycle/view của phòng chưa tồn tại. Nhận media riêng thêm ACCOUNT và SESSION của peer theo `peer_session_id`; Media chịu trách nhiệm chọn đúng phiên đã nhận cuộc gọi.

Mutation trong ngữ cảnh server mang header mục tiêu `X-SCDC-Membership-Id` của actor, khác `membershipId` của thành viên đích trong body. Adapter truyền nó vào `expected_membership_id`; authority so khớp trước cấp phép. Không áp header cho tạo/join server hay DM. Đối với READ/bù/đăng ký, vẫn so khớp membership/epoch của cursor hoặc subscription; không biến dữ liệu cũ thành quyền mới sau khi tham gia lại. S2-07/08 cập nhật client và contract hiện hành cùng lúc bật adapter mới.

| Bảng thiết kế trong schema mỗi owner | Khóa và dữ liệu cần giữ |
|---|---|
| `access_fences` | Khóa chuẩn hóa của scope; `min_epoch` bigint; unique theo authority và toàn bộ khóa scope |
| `fence_receipts` | Authority, operation ID, batch ID; fingerprint của lệnh; generation và epoch đã cài |
| `access_scopes` (authority) | Cùng khóa scope; `current_epoch`, `pending_operation_id`, trạng thái active/retired và version; cấp permit và đặt ý định khóa cùng dòng |
| `access_changes` (authority) | Operation ID; actor/session ID; loại thao tác, fingerprint, expectedVersion, thay đổi dự kiến; scope/epoch mới; owner cần ACK; phase, version |
| `access_change_acks` (authority) | Operation, batch, owner, generation, epoch đã xác nhận; unique để chống trùng |

S2-03 tạo `access_fences/fence_receipts` tại owner; S2-06 tạo `access_scopes/access_changes/access_change_acks` ở Identity; S2-07 tạo ba bảng authority đó ở Community. Scope mới bắt đầu ở epoch 1; membership/session đã retired không được tái sử dụng. Scope key là tuple chuẩn hóa không NULL: UUID không dùng biểu diễn bằng UUID 0 ở DB, permission không dùng là chuỗi rỗng; API vẫn yêu cầu trường ngoài scope rỗng. Không dùng unique nullable làm hổng chống trùng. Receipt so sánh toàn bộ trường chuẩn hóa/scope đã sắp thứ tự, không hash byte Protobuf như một dạng tuần tự hóa chuẩn. Không FK sang schema khác; outbox theo owner được ghi cùng thay đổi nghiệp vụ.

Đường ghi của owner thực hiện:

1. Lấy đủ giấy phép trước khi mở transaction. Authority đọc dưới khóa phạm vi, không cấp khi scope có ý định chưa xong. Lookup hồ sơ không cấp giấy phép.
2. Sắp toàn bộ scope theo authority → loại scope → byte UUID → permission code. Trong transaction cục bộ, theo đúng thứ tự đó tạo dòng fence còn thiếu bằng `INSERT ... ON CONFLICT` rồi khóa `FOR SHARE`; việc tạo cũng phải tranh chấp trên cùng unique key. Chỉ được khởi tạo như vậy khi owner đã hoàn tất bootstrap registry.
3. Kiểm tra mọi epoch giấy phép `>= min_epoch`, actor/mục đích/tài nguyên/generation khớp. Thiếu scope cần có thì từ chối; epoch cao không tự cấp thêm quyền.
4. Khóa và kiểm tra dữ liệu nghiệp vụ, ghi dữ liệu, dấu chống trùng và outbox. Ngay trước commit kiểm tra lại hạn của mọi giấy phép và hạn phiên; giữ khóa fence tới commit.
5. `Apply` tạo/khóa các dòng cùng thứ tự trên bằng `FOR UPDATE`, nâng floor bằng `max`, ghi receipt và commit rồi mới ACK. Writer đã giữ khóa xong trước ACK; writer tới sau mang epoch cũ bị chặn. Lệnh trùng trả receipt; cùng batch ID khác nội dung bị từ chối.

Giấy phép sống tối đa 5 giây và không vượt hạn phiên; transaction ghi tối đa 1 giây, chờ khóa tối đa 500 ms. Admission yêu cầu còn ít nhất 1,5 giây, dự phòng lệch đồng hồ tối đa 100 ms mỗi bên; kiểm tra cuối tại DB dành thêm 250 ms trước hạn. Dùng `clock_timestamp()`, không dùng thời điểm bắt đầu transaction để kiểm tra hết hạn. Adapter phải áp deadline DB và rollback khi hết hạn; không gọi RPC trong transaction. Đây là ngân sách ban đầu, cần chứng minh độ lệch đồng hồ và khoảng commit trong S2-11; không mở writer mới nếu không giữ được giới hạn này. Timeout phía client không chứng minh DB đã rollback ([PostgreSQL timeouts](https://www.postgresql.org/docs/18/runtime-config-client.html#RUNTIME-CONFIG-CLIENT-STATEMENT)).

### Thu hồi và thay đổi quyền

`access_changes` có ba phase: `FENCING → COMMITTED` hoặc `FENCING → ABORTED`. ACK từng owner và cập nhật phase dùng CAS theo version. Không có bước RPC khi đang giữ transaction authority.

1. Authority kiểm tra quyền người yêu cầu và expectedVersion, khóa scope, lưu ý định cùng epoch kế tiếp. Trạng thái quyền nghiệp vụ vẫn là bản đã commit; việc cấp giấy phép mới cho scope bị ảnh hưởng dừng ngay. Cấp permit và tạo ý định dùng cùng khóa.
2. Worker gửi batch fence tới tất cả owner của generation đã lưu. Owner không gọi ngược authority trong handler cài fence; nhận diện lệnh bằng danh tính service và kiểm tra phạm vi được phép.
3. Identity còn phải ghi deny có version vào Redis và nhận xác nhận trước khi chốt thu hồi. Chỉ sau đủ receipt bền, authority mới mở transaction cuối, kiểm tra điều kiện người yêu cầu khi cần, đổi trạng thái/quyền, công bố epoch mới, đánh dấu `COMMITTED` và ghi outbox cùng commit.
4. Nếu người yêu cầu mất phiên/quyền trước commit của thay đổi vai trò/ACL/chuyển chủ, hoặc expectedVersion không còn đúng, bỏ thay đổi dự kiến: vẫn hoàn tất cài các floor đã phát, rồi `ABORTED` và cấp giấy phép theo quyền cũ nhưng epoch mới. Không hạ floor hoặc tái sử dụng permit cũ.
5. Thu hồi phiên/tài khoản đã được chấp nhận, gồm đăng xuất chính phiên yêu cầu, là lệnh an toàn một chiều: worker phải hoàn tất bằng danh tính service, không đòi phiên vừa bị chặn cấp phép lại. Hủy tạo phòng/dọn dữ liệu cũng là công việc hệ thống. Không dùng ngoại lệ này để tạo phòng hay ghi nội dung bằng quyền đã mất.
6. Restart tiếp tục từ ý định/receipt đã lưu. Không rõ kết quả thì giữ `FENCING`, đối chiếu cùng operation/batch ID và thử lại. Không timeout rồi bỏ ý định hoặc coi đã thu hồi.

Đăng xuất mọi thiết bị/đổi mật khẩu chụp tập SESSION cũ dưới khóa tài khoản và chặn tạo phiên mới trong lúc reset; fence các session đó, không đổi ACCOUNT eligibility chỉ vì chủ tài khoản đăng xuất. Giấy phép gửi tới peer còn hoạt động vẫn được cấp. Khóa tài khoản thì tăng ACCOUNT epoch vì điều kiện hoạt động thực sự đổi, đồng thời thu hồi mọi phiên. Khóa đăng nhập 15 phút không tự thu hồi phiên đang hợp lệ. Đổi mật khẩu chỉ lưu hash mới ở Identity, không lưu mật khẩu hoặc token thô trong tiến trình phối hợp.

Thay đổi chính sách Community lưu bản dự kiến và tập scope bị ảnh hưởng dưới khóa server. Khi còn pending, các thay đổi cấu trúc/chính sách hoặc membership trong server phải chờ/nhận lỗi tạm; giấy phép cho scope không bị ảnh hưởng vẫn được cấp. Tập bị ảnh hưởng là quyền hiệu lực trước/sau, tính đúng deny, ngoại lệ cá nhân và chủ server. Đổi tên không tự thu hồi quyền; xóa role deny có thể tăng quyền. Người còn quyền giữ ngữ nghĩa cũ; snapshot cũ có thể phải xin lại khi epoch thay đổi.

Thứ tự cuối ở Community: lấy giấy phép Identity trước; transaction cuối khóa fence Identity rồi trạng thái server và scope Community. Identity chỉ cài fence cục bộ ở Community, không đợi Community điều phối; Community không giữ khóa trong lúc hỏi Identity. Nhờ vậy hai authority không chờ vòng nhau. Hết phiên tự nhiên cũng chặn permit/đăng ký mới và kiểm tra hạn trên writer; phiên mới hay membership mới không mở lại scope cũ.

### Redis và kết nối đang mở

Chọn danh sách phiên hợp lệ có version: bản account và session tối thiểu gồm trạng thái, epoch, hạn và generation kho Redis; đọc nhất quán cả hai. Dữ liệu thiếu hoặc kho chưa sẵn sàng trả lỗi tạm, trạng thái deny rõ ràng trả lỗi phiên/tài khoản. Không fallback sang Identity cho đường đọc phiên của D31.

- **Cấp phiên:** commit session và việc cần công bố vào DB, ghi bản active đúng version vào Redis, xác nhận rồi mới trả token. Lỗi công bố không trả đăng nhập thành công; worker đối chiếu từ DB.
- **Refresh:** giữ cùng session/hạn, serialize với revoke trên scope session; chỉ trả token khi trạng thái Redis xác nhận đúng version. Giữ quy tắc reuse hiện hành; mất phản hồi không tự gửi lại refresh token.
- **Thu hồi:** lưu ý định, công bố deny epoch kế tiếp trước final commit; lệnh active phiên bản cũ không được ghi đè deny. Không mở khóa tài khoản bằng việc phục hồi phiên cũ.
- **Redis restart/rebuild:** ready=false, đổi generation, nạp từ PostgreSQL hiện tại và đối chiếu journal tới mốc đồng bộ trước ready=true. Mọi lệnh ghi Redis mang generation đọc trước khi lấy snapshot DB; từ chối lệnh generation cũ và version thấp. Mất dữ liệu/eviction không được tự coi là active. S2-06 triển khai script so sánh version/generation và cổng sẵn sàng này.

Messaging kiểm tra lại scope/phiên trước khi đăng ký, bù tin và ngay trước lần gửi cuối tới mỗi kết nối; nội dung trong hàng đợi không mang quyền vĩnh viễn. Watchdog độc lập kiểm tra tối đa mỗi giây; kết quả cho phép kết nối chỉ giữ tối đa 2 giây. Khi Redis/authority không dùng được, không gia hạn quyền gửi. Owner mất khả năng đọc fence hoặc watchdog không hoạt động phải ngừng phát nội dung.

Media chỉ được đăng ký làm owner khi lease ở provider tự hết hạn và dừng cả phát/nhận đúng hạn, kể cả khi Media process dừng. ACK fence không thay bằng chứng LiveKit; phần cài đặt thuộc gói Media. Với mọi owner, mục tiêu cắt 5 giây tính từ commit quyền thực tế; S2-11 đo bằng cửa sổ trước/sau commit và mốc ngừng, dùng mốc trước commit để có số đo bảo thủ, không lùi mốc theo ACK/outbox.

### Deadline, lỗi và quyền gọi

| Mục | Giá trị thiết kế ban đầu |
|---|---|
| RPC permit, cài fence, query, prepare/confirm/cancel | Tối đa 2 giây, không vượt deadline request còn lại |
| Chờ đồng bộ thao tác thay quyền/tạo phòng | 5 giây; ý định bền tiếp tục nếu HTTP kết thúc |
| Worker | Tick 1 giây; giành việc bằng lease DB 10 giây và CAS version; mỗi loại coordinator tối đa 4 việc đồng thời |
| Retry nội bộ | 1, 2, 4, 8, 16, rồi 30 giây, jitter ±20%; không giới hạn số lần cho công việc an toàn chưa xong |
| Chống trùng | Operation UUIDv7 nội bộ, UUIDv4 của client giữ nguyên; batch ID gắn nội dung bất biến; không giữ token/nội dung tin trong coordinator |
| Lưu dấu | Giữ epoch, receipt, quyết định kết thúc và fingerprint tối thiểu trong v1; không tự TTL làm thao tác cũ sống lại |

Cancellation chỉ dừng chờ; server có thể đã commit ([gRPC deadlines](https://grpc.io/docs/guides/deadlines/)). Automatic retry chỉ dành cho RPC nội bộ có cùng khóa và nội dung. HTTP mutation vẫn do người dùng chủ động gửi lại theo quy tắc từng tính năng.

| Kết quả | Xử lý |
|---|---|
| Phiên/tài khoản bị từ chối; tài nguyên ẩn; thiếu quyền; version cũ | Giữ mapping 401/403/404/409 và mã lỗi của endpoint hiện hành |
| Scope đang đổi, chưa cấp permit | `503 ACCESS_PENDING`; không tự POST lại |
| Redis/authority/generation chưa xác minh được | `503 ACCESS_CHECK_UNAVAILABLE`; không biến thành phiên bị thu hồi |
| Permit cũ/hết hạn trước commit | Rollback, `409 ACCESS_CHANGED`; tải lại quyền, không tự gửi lại thao tác ghi |
| Thu hồi/quyền còn `FENCING` sau thời gian chờ | `202` với tham chiếu thao tác; GET đối chiếu, chưa báo đã thu hồi |
| Timeout chưa rõ mutation đã commit chưa | `503 OPERATION_OUTCOME_UNKNOWN` và tham chiếu tra cứu nếu có; không coi là rollback |
| RPC sai danh tính/phạm vi, đầu vào sai, xung đột | gRPC `UNAUTHENTICATED/PERMISSION_DENIED`, `INVALID_ARGUMENT`, `ALREADY_EXISTS/FAILED_PRECONDITION`; lỗi danh tính service ánh xạ 503 ở API, không đăng xuất người dùng |
| RPC `DEADLINE_EXCEEDED/UNAVAILABLE` sau gửi lệnh | Đối chiếu cùng operation ID; GET không tìm thấy chưa chứng minh lệnh chưa tới |

Identity chỉ được cài `ACCOUNT/SESSION`; Community chỉ cài scope của Community. Community gọi Identity cho nghiệp vụ Community; Messaging cho DM/tin phòng; Media cho media. Owner trong permit/receipt phải khớp service thật, không nhận giá trị owner từ body người dùng. RPC provisioning chỉ nhận Community; RPC inbox chỉ nhận producer và event type trong allowlist. Worker được đọc tiếp tiến trình của authority tương ứng; API người dùng chỉ tra thao tác của mình và quyền hiện tại. Chi tiết REST mục tiêu nằm trong [hợp đồng phối hợp](../contracts/coordination.openapi.json).

Outbox D27 lưu kết quả nghiệp vụ và sự kiện cùng transaction cục bộ; worker theo dõi riêng từng consumer. `IntegrationInbox.Accept` chỉ ACK sau commit inbox và việc cần xử lý, unique theo producer/event ID; cùng ID khác nội dung là xung đột. Receipt inbox không chứng minh đã cài fence hoặc đã phát Hub. Consumer xử lý trùng không tạo hiệu ứng mới; sự kiện cũ/sai thứ tự chỉ kích hoạt đối chiếu quyền hiện tại, không hạ epoch hoặc tự cấp quyền từ payload. Giữ nguyên ID/type/trạng thái của outbox MVP khi migration, không đổi legacy event thành sự kiện mới để phát lại.

### Luồng gửi DM tranh với thu hồi

```mermaid
sequenceDiagram
    participant M as Messaging
    participant I as Identity
    participant MD as DB Messaging
    participant ID as DB Identity
    M->>I: AcquirePermit DM_SEND_NEW (actor, peer, operation)
    I-->>M: Epoch account/session actor và account peer
    M->>MD: Khóa SHARE fence; kiểm tra; ghi tin + operation + outbox
    I->>ID: Lưu ý định thu hồi, dừng cấp permit scope
    I->>M: Apply fence với epoch mới
    M->>MD: Chờ writer trước, nâng floor và commit receipt
    MD-->>M: Commit xong
    M-->>I: Receipt sau commit
    I->>ID: Commit thu hồi sau đủ ACK và Redis deny
```

Nếu Apply lấy khóa trước writer thì writer rollback vì epoch cũ. Nếu writer lấy khóa trước thì tin commit trước receipt và trước commit thu hồi. Gửi lại tin đó dùng `DM_REPLAY`, vẫn kiểm tra người gửi nhưng không yêu cầu peer còn hoạt động.

### Kiểm chứng bắt buộc khi triển khai

| Mã | Tình huống | Kết quả phải chứng minh |
|---|---|---|
| TC-S2-02-01 | Writer và fence tranh cả hai thứ tự | Commit trước thu hồi hoặc bị chặn; không có ghi bằng epoch cũ sau commit thu hồi |
| TC-S2-02-02 | Chưa có dòng fence; writer tới cùng Apply | Unique/khóa cục bộ vẫn giữ quy tắc trên |
| TC-S2-02-03 | Owner commit fence rồi mất ACK | Gửi lại nhận receipt, không tăng/lùi floor sai hoặc chốt thiếu owner |
| TC-S2-02-04 | Authority/owner restart; batch trùng, đảo thứ tự | Tiếp tục từ DB, floor chỉ tăng; khác nội dung cùng ID bị từ chối |
| TC-S2-02-05 | Thu hồi một phiên hoặc một phòng | Phiên khác/phòng khác và DM giữ quyền đúng phạm vi |
| TC-S2-02-06 | Peer bị khóa; tạo/mở DM, gửi mới, replay, đọc, sửa/xóa | Chặn tạo/gửi mới; mở DM cũ, lịch sử và thao tác tác giả hợp lệ còn dùng được |
| TC-S2-02-07 | Vai trò deny, ngoại lệ cá nhân, đổi chủ | Chặn đúng quyền hiệu lực; không suy từ tên sự kiện |
| TC-S2-02-08 | Rời/vào lại; khóa/mở khóa | Không dùng lại membership, phiên hoặc permit cũ |
| TC-S2-02-09 | Identity và Community cùng thu hồi | Không chờ vòng; handler fence không gọi authority |
| TC-S2-02-10 | Actor mất quyền/hết phiên lúc đổi chính sách | Abort dự kiến bằng epoch mới; lệnh tự thu hồi an toàn vẫn được hoàn tất |
| TC-S2-02-11 | Redis mất/rebuild, active cũ đến sau deny | Fail closed; generation/version cũ không phục hồi phiên |
| TC-S2-02-12 | Hạn permit/phiên trôi qua lúc chờ DB/commit | Rollback theo ngân sách DB; không báo thành công do chỉ kiểm tra lúc đầu |
| TC-S2-02-13 | Outbox chậm, watchdog/worker/mạng lỗi | Ngừng nhận nội dung trong 5 giây, không dùng ACK inbox làm ACK fence |
| TC-S2-02-14 | Owner mới, routing generation sai | Chưa bootstrap thì không ghi; không bỏ owner đang lỗi khỏi tiến trình |
| TC-S2-02-15 | Đường đọc/Hub/worker, cursor/epoch cũ | Không bypass quyền hiện tại hay lộ nội dung qua kết quả gửi lại |

## Còn mở

- S2-03/06/07 đã có DDL, Redis, permit Identity và coordinator Identity/Community cùng bằng chứng runtime. S2-08/09/10 tiếp tục thay writer Messaging, provision phòng và hợp đồng; S2-11 kiểm chứng toàn registry, tranh chấp và thời gian thu hồi.
- Phiên bản Redis, persistence, tài nguyên, HA, bảo vệ kết nối và cấp credential được chọn trong S2-06; không làm thay đổi quy tắc fail closed hoặc thứ tự công bố phiên.
- Lease/provider và bằng chứng ngắt LiveKit thuộc gói Media; không bật Media trong registry trước khi thực thi được hàng rào và thời hạn 5 giây.

---

Nguồn: DEC-083, DEC-099 (thời hạn 5 giây), [DEC-123](../records/decisions/README.md#dec-123) (D05), [DEC-124](../records/decisions/README.md#dec-124) (D06), [DEC-126](../records/decisions/README.md#dec-126) (D07), [DEC-148](../records/decisions/README.md#dec-148) (D30), [DEC-149](../records/decisions/README.md#dec-149) (D31), DEC-156 (backup sau v1). Các phương án đã cân nhắc ghi trong hồ sơ quyết định.

<a id="s2-06-runtime"></a>

## S2-06 — Triển khai kiểm tra phiên và Identity coordinator

Redis 8.2.11 chạy một bản trong Compose, AOF `appendfsync always`, 128 MB với `noeviction`; container giới hạn 256 MB. StackExchange.Redis 3.2.0 đặt trong BuildingBlocks infrastructure; Shared.Core tiếp tục thuần .NET. Credential reader/writer riêng, file qua Compose secrets; ACL chỉ cho Identity ghi và reader chạy script đọc. Redis này phục vụ xác thực, không làm broker/backplane/LiveKit.

Mỗi account là một hash `scdc:auth:user:<uuid>` chứa `generation`, `version` và snapshot JSON của trạng thái account, stamp, email đã xác minh, epoch và các phiên. Đọc một snapshot giúp kiểm tra account/session nhất quán. Không chứa mật khẩu, token, email, hồ sơ hay nội dung chat. `identity.session_publications` là journal version bền, được trigger cập nhật cùng thay đổi account, session, email, security state và scope; xóa account để lại tombstone.

Identity đọc generation trước snapshot DB. Lua so sánh version bigint bằng chuỗi thập phân để không mất độ chính xác trên 2^53; generation cũ hoặc version thấp bị từ chối. Bootstrap dùng advisory lock theo prefix, sequence generation bền trong Identity, nạp và đối chiếu journal theo lô 128 account trước khi mở ready. Reader so `run_id` ngay trong script, nên ready marker được Redis phục hồi từ process cũ không được dùng. Ready chỉ sống 2 giây, publisher đối chiếu mỗi 500 ms; hết hạn hoặc lỗi thì chặn truy cập. `/api/v1/health/access` trả 200 khi kho phiên sẵn sàng, còn lại 503.

Đăng nhập/refresh chỉ trả token sau commit DB, công bố Redis và kiểm tra lại phiên. Refresh giữ session và hạn cũ; frontend không tự dùng lại refresh token nếu kết quả công bố không rõ. Dữ liệu thiếu, đang rebuild hoặc kết nối hỏng trả `503 ACCESS_CHECK_UNAVAILABLE`; scope đang xử lý trả `503 ACCESS_PENDING`. Không dùng DB Identity dự phòng cho reader.

`identity.access_scopes`, `access_changes`, `access_change_acks` giữ epoch, ý định và receipt. Hai adapter owner thật dùng connection của Community/Messaging để cài floor và receipt trong transaction cục bộ; ACK sau commit, không có owner Media. Registry hiện cố định generation 1 và hai owner; thêm owner/đổi generation phải qua bootstrap offline trước khi nhận traffic.

Logout, logout-all, thu hồi một phiên, refresh-token reuse, đổi và reset mật khẩu tạo ý định dưới khóa user; password chỉ lưu hash đã tính tại Identity. Worker tiếp tục bằng danh tính hệ thống sau khi phiên yêu cầu bị chặn. Thu hồi giữ `FENCING` nếu thiếu owner hoặc Redis; HTTP chờ tối đa 5 giây rồi trả 202 với operation ID và đường GET `/api/v1/auth/revocations/{operationId}`. GET chỉ đọc tiến trình của account đang xác thực. Sau đủ receipt và Redis deny, transaction cuối đổi trạng thái phiên/mật khẩu, epoch, audit và outbox cùng lúc. Worker dùng lease 10 giây, CAS theo lease/version và retry 1–30 giây với jitter; không timeout rồi bỏ ý định.

**Chuyển tiếp tới S2-07/08:** đường ghi Community/DM còn giữ guard khóa user/phiên cũ; transaction cuối của Identity vẫn lấy khóa user tương ứng. Vì vậy bản này chạy được với writer cũ trong khi permit/fence được chuẩn bị cho writer mới. S2-06 không gỡ quyền DB chéo hoặc chứng minh mọi writer đã dùng permit; các việc đó thuộc S2-07/08/10/11. Outbox đã kiểm tra Redis trước phát; Hub và watchdog dùng reader mới. Các fixture SQL quản trị phải công bố journal trước khi kiểm tra hành vi HTTP; thao tác SQL trực tiếp không thay giao thức thu hồi được xác nhận qua API.


<a id="s2-07-runtime"></a>

## S2-07 — Transaction riêng và policy Community

Server, join, role, danh sách thành viên và channel lấy permit Identity trước khi mở transaction Community. Trong transaction chỉ đọc/khóa fence Identity và policy cục bộ; không truyền transaction hoặc đọc SQL Identity. Permit gắn owner, actor/session, mục đích, resource/operation và generation. Admission kiểm đủ ACCOUNT/SESSION, epoch và thời hạn; giữ khóa fence tới commit. RPC Identity/owner có deadline 2 giây, permit tối đa 5 giây, admission còn ít nhất 1,5 giây; DB dùng lock timeout 500 ms, statement/transaction timeout 1 giây và kiểm tra cuối bằng `clock_timestamp()` với biên 250 ms. S2-11 vẫn cần chứng minh ngân sách dưới tải và lệch đồng hồ.

`RoleService` đóng transaction Community trước khi gọi `IUserDirectory` qua connection Identity độc lập, deadline 2 giây. Lookup chỉ bổ sung tên; không cấp quyền. Kiểm thử đã thu hồi USAGE schema Identity của role Community mà API vẫn hoạt động.

`community.access_scopes/access_changes/access_change_acks` lưu phạm vi, thay đổi dự kiến và ACK. Bốn thao tác hiện hành đi qua coordinator: sửa/xóa role, thay tập role của thành viên và thay ACL channel. Dưới khóa server, coordinator kiểm tra request trên policy hiện hành, tính trước/sau bằng cùng evaluator dùng cho quyền thực tế, rồi rollback phần preview nếu có quyền bị mất. Chỉ các scope mất quyền được tăng epoch và đặt pending; owner, deny, allow, ngoại lệ cá nhân và đúng membership đều tham gia phép tính. Đổi tên và xóa deny làm tăng quyền không tự tạo thu hồi. Mutation cấu trúc trong server có thay đổi pending nhận lỗi tạm; scope đọc không bị ảnh hưởng vẫn dùng được.

Ý định giữ actor/session/membership gốc, command chuẩn hóa, fingerprint bất biến, generation 1 và hai owner Community/Messaging; không giữ JWT, stamp hoặc token thô. Worker dùng lease 10 giây và retry có backoff/jitter. Cài floor theo thứ tự canonical, ghi receipt trong DB owner rồi mới lưu ACK authority. Batch cùng ID khác nội dung bị từ chối; floor dùng max, không lùi. MANAGEMENT chỉ ở Community; Messaging vẫn ghi receipt thật cho batch rỗng nếu không có scope thuộc trách nhiệm của nó.

Sau đủ ACK, worker xin permit `RESUMING_ACCEPTED_OPERATION` cho đúng session gốc, khóa fence Identity rồi server, kiểm lại quyền/membership/expectedVersion trên policy cũ trước khi áp policy mới. Actor có thể tự mất quyền do chính thay đổi hợp lệ vừa gửi. Nếu điều kiện gốc mất, abort policy nhưng vẫn nâng scope tới floor đã cài. Commit policy, epoch, phase và outbox cùng transaction. Đăng nhập bằng session mới không tiếp quản thay đổi của session đã bị thu hồi. Rejoin tạo membership ID mới và retire scope của membership cũ.

202 trả tham chiếu bền, GET status chỉ đọc và kiểm tra quyền hiện tại; không lộ command/scopes/policy dự kiến. Header actor và lưu tham chiếu qua reload triển khai cùng WebClient/OpenAPI. Chi tiết vận hành tại [nâng cấp S2-07](../guides/development.md#s2-07-upgrade).

S2-07 chưa chuyển transaction tạo Messaging space sang luồng provision (S2-09), chưa thay guard DM (S2-08), chưa host RPC/gỡ hợp đồng cũ (S2-10), chưa thu hẹp toàn bộ grant tạm hoặc bật Media (S2-11/Media). Adapter `IChannelAccessGuard` nhận transaction cũ không còn được đăng ký; hiện không có caller production, channel nội bộ dùng guard cục bộ với permit đã lấy trước transaction. Không tuyên bố đã bật giao thức mới trên mọi owner khi các đường đó chưa được chuyển đổi.
