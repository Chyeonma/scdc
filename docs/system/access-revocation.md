# SCDC — Thu hồi phiên và quyền truy cập

Khi một người bị đăng xuất, bị khóa tài khoản, rời server hay mất quyền xem phòng, hệ thống phải cắt mọi đường truy cập của họ: request HTTP mới, thao tác ghi, kết nối chat và media đang mở. Trang này mô tả yêu cầu thời gian, trách nhiệm của từng service, và thiết kế v1 khi các service tách rời nhau.

**Trạng thái:** MVP đã có thu hồi cho DM (một ứng dụng duy nhất). Thiết kế v1 đã chốt (D05, D06, D07, D30, D31) nhưng chưa có code.

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
| Dữ liệu trong Redis | Trạng thái phiên và tài khoản cần cho xác thực, kèm hạn và phiên bản (schema chưa chốt) |
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

## Còn mở

**D05, D06, D07:**

- Schema hàng rào và giấy phép ghi; danh sách owner cho từng loại thu hồi; thứ tự khi có nhiều authority.
- File `.proto`, thời hạn và lịch thử lại, quyền gọi RPC, thông tin người thực hiện truyền theo lệnh.
- Phản hồi API và giao diện khi thu hồi chưa xong.
- Cách ghi nhiều DB một cách nguyên tử cho các nghiệp vụ cần.
- Quyền của người yêu cầu khi tiến trình thu hồi đang chạy; phiên hết hạn tự nhiên; bên đọc, worker gửi sự kiện và LiveKit.
- Số luồng xử lý, cách giành việc và thử lại của worker.
- Bằng chứng trước khi làm v1: owner dừng rồi chạy lại, mất xác nhận sau commit, authority hoặc owner khởi động lại, lỗi mạng một phần, quyền cũ sau khi đã cài hàng rào.

**D30, D31:**

- Schema; danh sách đen hay danh sách trắng; TTL; xử lý phiên hết hạn tự nhiên.
- Giao thức phối hợp DB với Redis khi cấp phiên, làm mới token, thu hồi; trạng thái sẵn sàng sau khởi động.
- Mã lỗi và giao diện cho lỗi tạm thời; thời gian chờ; giới hạn lời gọi Redis; điều kiện phục hồi.
- Phiên bản Redis, persistence, eviction, tài nguyên, HA; bảo vệ kết nối, quyền và thông tin đăng nhập Redis.

---

Nguồn: DEC-083, DEC-099 (thời hạn 5 giây), [DEC-123](../records/decisions/README.md#dec-123) (D05), [DEC-124](../records/decisions/README.md#dec-124) (D06), [DEC-126](../records/decisions/README.md#dec-126) (D07), [DEC-148](../records/decisions/README.md#dec-148) (D30), [DEC-149](../records/decisions/README.md#dec-149) (D31), DEC-156 (backup sau v1). Các phương án đã cân nhắc ghi trong hồ sơ quyết định.
