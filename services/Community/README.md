# Community service

Community có bốn project ứng dụng và project hợp đồng gRPC riêng. Host riêng có chế độ chuẩn bị mặc định và chế độ tích hợp Identity/Messaging qua gRPC/mTLS. Dev vẫn phục vụ Community qua API chung; chuyển dữ liệu và gateway thuộc bước 3.

| Project | Vai trò |
|---|---|
| `SCDC.Community.Domain` | Model và bộ tính quyền thuần .NET |
| `SCDC.Community.Application` | Interface ca sử dụng, command và DTO; không tham chiếu SQL hoặc ASP.NET |
| `SCDC.Community.Infrastructure` | Implementation hiện hành, SQL/transaction, cursor, outbox, coordinator và đăng ký worker |
| `SCDC.Community.Api` | HTTP Community, Swagger, health, nhận Identity fence và cấp channel readiness permit |
| `SCDC.Community.Grpc` | Mã sinh hợp đồng fence và channel readiness; chỉ chứa message/client/server base |

Namespace cũ được giữ để tương thích. Controller và bộ lọc OpenAPI Community nằm trong project API mới; `SCDC.Api` biên dịch các file này bằng liên kết nguồn trong giai đoạn chuyển tiếp. Các adapter HTTP kỹ thuật dùng chung cũng được biên dịch vào từng host; host Community không tham chiếu assembly `SCDC.Api` hoặc implementation Messaging/Identity.

## Chạy preview riêng

Preview tạo một instance PostgreSQL và các volume thử nghiệm riêng. Nó không kết nối database dev, không dùng khóa Identity thật và không thay định tuyến gateway. Đích v1 vẫn là database Community riêng trên instance chung với Messaging.

Từ gốc repo, cần Python 3, OpenSSL và Docker Compose:

```bash
export SCDC_COMMUNITY_PREVIEW_SECRET_DIR="$HOME/.local/share/scdc/community-preview-secrets"
python3 scripts/init_community_preview_secrets.py "$SCDC_COMMUNITY_PREVIEW_SECRET_DIR" --label-for-containers
docker compose -f compose.community-preview.yaml --profile community-preview up --build -d
```

Với Podman, dùng `podman-compose` thay cho `docker compose` trong lệnh trên. Các service chỉ bật khi chọn profile `community-preview`. Script tạo mật khẩu admin/runtime độc lập, HMAC và public key JWT thử; chạy lại giữ các giá trị cũ. Mật khẩu admin chỉ mount vào PostgreSQL, API chỉ nhận mật khẩu runtime và không có private key JWT.

| URL | Kết quả ở bước 1 |
|---|---|
| `http://localhost:5082/health/live` | 200, mode `preparation`, worker chưa bật |
| `http://localhost:5082/health/database` | 200 nếu database Community riêng và role runtime hợp lệ |
| `http://localhost:5082/health/ready` | 503 `COMMUNITY_INTEGRATIONS_NOT_CONFIGURED` |
| `http://localhost:5082/swagger/community/` | Swagger Community trong Development |
| Các API nghiệp vụ Community | 503 trước khi xác thực, đọc hoặc ghi dữ liệu |

Readiness 503 là trạng thái chuẩn bị; liveness và database khỏe chưa đủ điều kiện nhận lưu lượng. Host chưa bật `CommunityAccessWorker`/`ChannelProvisionWorker`; các adapter Messaging đang thiếu luôn báo không sẵn sàng, không trả kết quả provision hoặc receipt thành công. Host chung vẫn đăng ký worker và các adapter trong process như trước.

Dừng preview, giữ dữ liệu thử:

```bash
docker compose -f compose.community-preview.yaml --profile community-preview down
```

## Chế độ tích hợp qua mạng

Đặt `CommunityHost__Integrated=true` khi chạy host Community trên stack thử riêng. Host kiểm tra database riêng và role runtime trước khi bật `CommunityAccessWorker`/`ChannelProvisionWorker`; từ chối khởi động nếu tắt RPC hoặc thiếu endpoint HTTPS. Mỗi host chỉ nhận connection string của owner mình. Identity giữ private key JWT; hai consumer chỉ cần public key và cùng cấu hình Redis phiên.

| Host | Cấu hình cho stack tích hợp |
|---|---|
| Community | `CommunityHost__Integrated=true`; `ConnectionStrings__Community`; `Modules__Community__KeyRingPath` và HMAC keyring bền; endpoint Identity/Messaging HTTPS; TLS peer `community-service`, nhận `identity-service` và `chat-service` |
| Messaging trong `SCDC.Api` | `Hosting__CommunityEnabled=false` bỏ đăng ký Community, controller và receiver fence Community; `ConnectionStrings__Messaging`; endpoint Identity HTTPS; TLS peer `chat-service`, nhận `identity-service` và `community-service` |
| Identity | `ConnectionStrings__Identity`; TLS peer `identity-service`, nhận `chat-service` và `community-service`; callback fence Community trỏ host Community, callback Messaging trỏ API cũ |

Mỗi host cấu hình `InternalRpc__CertificatePath`, `InternalRpc__PrivateKeyPath`, `InternalRpc__CaPath`, `InternalRpc__AllowedPeers__0`/`__1`. HTTP mặc định cổng 8080, RPC HTTP/2 với mTLS cổng 8443. Endpoint gọi ra đặt bằng `InternalRpc__Identity__Endpoint`, `InternalRpc__Messaging__Endpoint`, `InternalRpc__Community__Endpoint` trên host tương ứng.

Identity cần thêm các thiết lập sau để phân quyền theo chứng chỉ:

```text
InternalRpc__PeerOwners__community-service__0=Community
InternalRpc__PeerOwners__chat-service__0=Messaging
InternalRpc__PermitDelegations__community-service__Messaging__0=ChannelRead
InternalRpc__Community__Peer=community-service
```

Delegation chỉ cho Community xin permit `ChannelRead` mang owner Messaging để tạo readiness permit; không cấp toàn bộ quyền Messaging hay quyền DM. Receiver Identity fence chỉ chấp nhận `identity-service`; provision và Community-policy fence ở Messaging chỉ chấp nhận `community-service`; readiness permit ở Community chỉ chấp nhận `chat-service`. Allowlist TLS và kiểm tra peer trên từng RPC đều áp dụng.

Cấp bộ chứng chỉ ba peer vào **thư mục thử mới**, không dùng lại thư mục secret dev hiện có:

```bash
python3 scripts/init_rpc_certificates.py /absolute/path/to/new-community-rpc-secrets --include-community --label-for-containers
```

Không có `--include-community`, script giữ hành vi hai peer cũ. Script từ chối nâng một thư mục hai peer có sẵn thành ba peer để tránh đổi CA/key đang dùng. Public JWT key, HMAC, Data Protection keyring và Redis dùng dữ liệu thử, không lấy secret thật. Giữ cùng environment/application name và keyring bền khi kiểm tra restart.

Các RPC provision `Prepare/Confirm/Cancel/Get/Activate` và fence giữ operation ID, fingerprint, generation và scope qua serialize/deserialize. Mỗi lời gọi có deadline 2 giây và nhận cancellation của caller; transport không tự retry. Coordinator/worker dùng journal bền để gửi lại sau mất phản hồi hoặc restart; chỉ ACK receipt khớp toàn bộ batch và đúng owner. Các quyết định terminal không đổi khi gửi lại.

Trong chế độ tích hợp, `/health/ready` chỉ trả 200 khi database Community, Redis phiên và hai dependency qua mTLS đều sẵn sàng. Messaging mất kết nối làm readiness 503; các luồng độc lập như đọc server vẫn hoạt động. Thao tác cần Messaging giữ kết quả pending/lỗi theo hợp đồng và tiếp tục bằng journal khi service trở lại. Profile preview phía trên vẫn mặc định `preparation`, không tự nối sang dev.

Các thiết lập này phải được áp dụng đồng bộ trong stack thử. Đổi callback Identity, gateway và dữ liệu dev thật chỉ thực hiện cùng đợt cutover ở bước 3 theo [checklist](../../docs/guides/operations.md#service-extraction).

## Kiểm tra và phần tiếp theo

`CommunityHostTests` kiểm tra chế độ chuẩn bị, database mới, quyền runtime, Swagger và chặn API. `CommunityNetworkTests` chạy ba Kestrel host thật qua mTLS với ba database riêng; kiểm tra provision, receipt, delegation, lỗi mạng, deadline/cancellation, thu hồi quyền, restart và đóng SignalR sau logout. Bộ test Community/DM/Identity hiện hành kiểm tra hồi quy host chung. Build cả solution bằng `dotnet build SCDC.slnx --configuration Release`; cách chuẩn bị PostgreSQL/Redis cho test xem [hướng dẫn](../../docs/guides/development.md#community).

Baseline database thử nằm trong [database/postgres/community](../../database/postgres/community/README.md). Bước 3 còn phải chuyển dữ liệu dev, đóng writer cũ, đổi callback/gateway và diễn tập rollback theo [checklist tách service](../../docs/guides/operations.md#service-extraction). S3-10 còn mở tới khi có bằng chứng chuyển và nghiệm thu.

Nguồn: [task S3-10](../../docs/project/tasks/step-3.md), [cấu trúc backend](../../docs/system/backend.md#v1-code-layout), D12, D40, D43, D47.
