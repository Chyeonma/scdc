# SCDC — Thiết kế gói tham gia trực tiếp

[Phạm vi](../delivery/direct-join/plan.md) chọn public/immediate của [UC-COM-06](../specs/memberships.md#uc-com-06). Contract chờ duyệt tiếp tục là mục tiêu. [Trạng thái và bằng chứng](../status.md) được cập nhật theo gói.

## API và lỗi

`POST /api/v1/servers/{serverId}/join` không body; actor lấy từ JWT sub/sid/sst. 200 Membership sau commit. Không clientOperationId: server lock và unique `(serverId,userId)` bảo vệ join lặp; retry chủ động trả tư cách hiện hành.

| HTTP / errorCode | Hành vi |
|---|---|
| 400 VALIDATION_FAILED | Body hoặc đầu vào sai; không nhận userId/role từ client |
| 401 / SESSION_INVALID | Thiếu phiên hoặc guard/expiry không hợp lệ |
| 403 ACCOUNT_ACCESS_DENIED | Account không active hoặc primary email chưa xác minh |
| 404 RESOURCE_NOT_FOUND | Private/inactive/deleted/unknown; không lộ metadata |
| 409 JOIN_APPROVAL_REQUIRED | Người chưa active gặp approval; không ghi membership/request |
| 409 VERSION_LIMIT_REACHED | Version cần tăng đạt giới hạn integer; không wrap hoặc ghi dở |
| 503 ACCESS_CHECK_UNAVAILABLE / COMMUNITY_TEMPORARILY_UNAVAILABLE | Guard/DB/lock tạm lỗi; đối soát bằng GET |

Member active trên public nhận cùng membership kể cả mode approval, không ghi event/version lần nữa. Private chặn route join công khai; active membership trong private vẫn đọc được qua API riêng.

## Giao dịch

1. Shared work scope READ COMMITTED; Identity guard giữ user share lock và session expiry.
2. SELECT server FOR UPDATE, đọc lại active/deleted/visibility/mode sau khóa. Thứ tự Identity → server → child records áp dụng cho mọi mutation tương lai.
3. Đọc own membership dưới server lock. Active trả record hiện hành; chưa active gặp approval dừng conflict.
4. Kiểm tra overflow server version/accessVersion và membership version cần tăng. Join mới dùng UUIDv7/version 1; rejoin dùng UUIDv7 mới, membership version +1, reset joinedAt/leftAt/nickname/timeout/inviter.
5. Rejoin xóa member_roles và channel_user_overrides của cặp server/user cùng transaction. @everyone áp ngầm. Chưa có writer role/ACL; FK/CAS epoch COM-SQL-04 phải hoàn thiện ở gói quản lý quyền, cleanup chưa là toàn bộ proof ABA.
6. Tăng accessVersion đúng một lần; server metadata version do trigger tăng. Ghi một Community.MembershipJoined.v1 (aggregate community.server, version từ UPDATE), payload serverId/userId/membershipId/membershipVersion/accessVersion; published_at NULL, chưa phát Hub.
7. Đọc Membership, kiểm tra lại thời gian lease và commit. Mọi cleanup/write/event rollback cùng nhau khi lỗi; không replay mutation trong service.

Không thêm migration: các bảng/cột cần dùng đã có sau migration 001. Khi bổ sung UC-COM-07/08 phải thêm schema/transition pending joined_elsewhere và kiểm chứng lại join.

## WebClient

Public summary qua URL chia sẻ là điểm vào, chưa có màn hình search. Nonmember/left public immediate thấy Tham gia; active/private/approval không có nút join trực tiếp.

Mutation dùng retry:false, bound actor, AbortController và chặn double submit. Đổi route/actor bỏ response cũ. Thành công tải detail/own list bằng GET; lỗi read sau commit không tự POST lại.

Mất mạng/401/503/500 giữ trạng thái cần kiểm tra: Kiểm tra kết quả chỉ GET detail/membership. Active mở detail/cập nhật list; chưa active sau GET thành công và còn public/immediate mới cho người dùng bấm Tham gia lại. Reload chỉ GET; 404 bỏ summary cũ; conflict approval đọc lại mode rồi dừng join. Không giả đã vào phòng khi chưa có phòng/lịch sử.
