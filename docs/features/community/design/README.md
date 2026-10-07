# SCDC — Thiết kế Community

Đọc quy tắc/use case tại [đặc tả](../specs/README.md); các thiết kế gói đã chọn được liệt kê tại [hợp đồng và thiết kế](#contracts).

Thiết kế theo thành phần: [Servers](servers.md), [Memberships](memberships.md), [Invitations](invitations.md), [Channels](channels.md), [Permissions](permissions.md). Cơ chế transaction/retry/migration chung nằm tại [Integration](integration.md).

<a id="organization"></a>

## Năm thành phần và cấu trúc code

| Thành phần | Trách nhiệm | Use case | Đặc tả |
|---|---|---|---|
| Servers | Metadata, search, visibility, join mode, ownership | [UC-COM-01](../specs/servers.md#uc-com-01), [UC-COM-02](../specs/servers.md#uc-com-02), [UC-COM-03](../specs/servers.md#uc-com-03), [UC-COM-04](../specs/servers.md#uc-com-04), [UC-COM-05](../specs/servers.md#uc-com-05), [UC-COM-14](../specs/servers.md#uc-com-14) | [Servers](../specs/servers.md) |
| Memberships | Membership/epoch, join, requests, approve/reject/cancel, leave/rejoin | [UC-COM-06](../specs/memberships.md#uc-com-06), [UC-COM-07](../specs/memberships.md#uc-com-07), [UC-COM-08](../specs/memberships.md#uc-com-08), [UC-COM-15](../specs/memberships.md#uc-com-15) | [Memberships](../specs/memberships.md) |
| Invitations | Link và mời đích danh, expiry/lượt/secret/transition | [UC-COM-09](../specs/invitations.md#uc-com-09), [UC-COM-10](../specs/invitations.md#uc-com-10), [UC-COM-11](../specs/invitations.md#uc-com-11), [UC-COM-12](../specs/invitations.md#uc-com-12), [UC-COM-13](../specs/invitations.md#uc-com-13) | [Invitations](../specs/invitations.md) |
| Channels | Metadata/kind/vòng đời phòng, danh sách theo view | [UC-COM-16](../specs/channels.md#uc-com-16), [UC-COM-17](../specs/channels.md#uc-com-17), [UC-COM-18](../specs/channels.md#uc-com-18), [UC-COM-19](../specs/channels.md#uc-com-19) | [Channels](../specs/channels.md) |
| Permissions | Role, assignment, ACL, evaluator/checker/guard | [UC-COM-20](../specs/permissions.md#uc-com-20), [UC-COM-21](../specs/permissions.md#uc-com-21), [UC-COM-22](../specs/permissions.md#uc-com-22) | [Permissions](../specs/permissions.md) |

[Thiết kế tích hợp liên module](integration.md) quản lý transaction, ID/version/retry, migration, lỗi và các luồng phối hợp tin phòng/realtime. [UC-COM-23](../specs/integration.md#uc-com-23), [UC-COM-24](../specs/integration.md#uc-com-24) do Messaging thực hiện; [UC-COM-25](../specs/integration.md#uc-com-25) phối hợp Identity, Community, Messaging và WebClient. Năm phần trong bảng trên là cấu trúc nội bộ của Community; luồng tích hợp được mô tả riêng với [module phụ trách và nguồn chuẩn](../specs/integration.md#responsibilities).

### Cấu trúc code mục tiêu

Cây dưới đây là bố cục nội bộ đã thống nhất. Đối chiếu phần đang có trên từng nhánh tại [tiến độ](../status.md). Nghiệp vụ đặt theo feature, Domain/Application ở trong từng phần; các phần cùng một assembly, schema community và CommunityDbContext.

```text
services/Modules/Community/
├── CommunityModule.cs
├── Features/
│   ├── Servers/{Domain,Application}/
│   ├── Memberships/{Domain,Application}/
│   ├── Invitations/{Domain,Application}/
│   ├── Channels/{Domain,Application}/
│   └── Permissions/{Domain,Application,Infrastructure}/
└── Infrastructure/
    ├── Persistence/       # CommunityDbContext và mapping
    ├── Idempotency/       # operation key, fingerprint, key access
    └── Outbox/            # ghi/dispatch thay đổi Community
```

Controllers đặt tại services/SCDC.Api/Controllers/Community theo từng nhóm; test theo feature trong tests/SCDC.Api.Tests/Community. CommunityModule.cs là điểm ghép DI của module. Tạo file cùng chức năng thực tế, không đưa thư mục rỗng vào Git.

### Ranh giới và phụ thuộc

- Servers giữ owner_user_id là nguồn chuẩn duy nhất; Memberships kiểm tra target còn membership active khi transfer. Permissions đọc ownership để tính policy, không có bản owner riêng.
- Memberships cung cấp hành vi tạo/kích hoạt tư cách chung cho join, approve và accept. Invitations tiêu thụ lời mời/lượt cùng transaction với membership; không lặp một writer membership khác.
- Permissions Domain tính quyền trên snapshot và không gọi Application service các phần khác. Application orchestration phối hợp stores/policies; Infrastructure dựng snapshot/guard bằng dữ liệu Community. Tránh vòng gọi service giữa Channels, Memberships và Permissions.
- Các feature nội bộ dùng chung transaction/DbContext; chia thư mục không chia commit nghiệp vụ. Tạo server phải ghi owner/@everyone/operation cùng commit; accept lời mời phải ghi membership/lượt/transition cùng commit; xóa phòng phối hợp Messaging lifecycle.
- Giao tiếp Identity/Messaging/Media qua SCDC.Contracts, mỗi module chỉ đọc/ghi dữ liệu mình sở hữu. Cơ chế guard/lock order và snapshot được quản lý tại [thiết kế tích hợp](integration.md#transactions).

<a id="contracts"></a>

## Hợp đồng và thiết kế

Thiết kế theo gói: [tạo/xem](create-view.md), [tham gia trực tiếp public/immediate](direct-join.md), [tìm kiếm công khai](search.md). Tiến độ và phạm vi được kiểm chứng tại [status.md](../status.md).

[OpenAPI Community](../../../contracts/community.openapi.json) giữ schema máy đọc được; [schema realtime](../../../contracts/community-realtime.schema.json) giữ catalogue Hub chat. Các route/mutation theo từng thành phần trong đặc tả của phần đó.

Hai artefact tổng hợp hợp đồng của hành trình cộng đồng, gồm cả thao tác tin phòng do Messaging thực hiện. Prefix route `/servers/...` và mã COM/UC-COM phục vụ giao diện/truy vết; quyền sở hữu dữ liệu và nơi triển khai theo [bảng phân công](../specs/README.md#use-cases), [ranh giới module](../../../architecture.md#boundaries).

<a id="detailed-design"></a>

[Thiết kế tích hợp](integration.md#contracts) quản lý ID/version, transaction/guard, operation retry, danh sách, lỗi và COM-SQL-01–10. [Permissions](permissions.md#detailed-design) giữ role/ACL snapshot; [Invitations](invitations.md#invite-secret) giữ token/link secret; [Servers](servers.md#search) giữ thiết kế search. Các artefact giữ phạm vi mục tiêu; tiến độ các gói và bằng chứng tại [status.md](../status.md).
