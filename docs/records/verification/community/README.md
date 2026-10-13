<a id="scdc--đối-chiếu-ucactc-với-bằng-chứng-community"></a>

# SCDC — Đối chiếu UC/AC/TC với bằng chứng Community

Cập nhật: 2026-10-08. Bảng này nối [truy vết đặc tả đầy đủ](../../../features/community/traceability.md#use-case-coverage) với test thực tế của các gói đã nghiệm thu. Trạng thái theo khả năng ghi tại các chủ đề trong [Community](../../../features/community/README.md); gate cho mốc đã chọn ở [MVP](../../../releases/mvp.md#community-acceptance). “Đạt phần” chỉ áp dụng assertion nêu trong hàng, không đóng toàn bộ UC/AC/TC có lịch sử/tin/Media/realtime.

<a id="revision-và-cách-đọc-bằng-chứng"></a>

## Revision và cách đọc bằng chứng

| Gói | Backend / WebClient commit | Kết quả lịch sử và nguồn |
|---|---|---|
| Tạo/xem | `79fa627` / `4203e05` | 63 backend, 16 Node, 14 Chromium; [acceptance](create-view.md) |
| Public/immediate join | `e5b5750` / `e0cf116` | 81 backend, 16 Node, 24 Chromium; [acceptance](direct-join.md) |
| Search | `7a883b7` (fixture patch `b10851b`) / `b5be14f` | 111 backend, 18 Node, 31 Chromium; [acceptance](search.md) |
| Roles | `9a513d1` / `eae64c3` | 158 backend, 21 Node, 39 Chromium; [acceptance](roles.md) |
| Phòng text/ACL | `eaaa2e3` / `f8d582d` | 191 backend, 24 Node, 50 Chromium; Release/production build đạt; [acceptance](channels-access.md) |

Các số là tổng suite tại từng revision, gồm hồi quy; không cộng các gói để tính tổng test hoặc tiến độ. Bảng bên dưới đối chiếu source backend `eaaa2e3`, WebClient `f8d582d` trên `feat/community-channels` với kết quả gói cuối. Bổ sung tài liệu này không chạy lại test sản phẩm. TRX đã ghi ở `artifacts/community-channels/backend/community-channels.trx`, browser output/ảnh ở `artifacts/community-channels/`; artifact local bị ignore, không bảo đảm có sẵn trong checkout mới. Muốn bằng chứng mới dùng [hướng dẫn chạy lại](../../../guides/community-development.md), ghi revision và actual result mới vào acceptance tương ứng.

Tên test dùng dạng `file::method` hoặc `file::test title`, tra theo commit nguồn của gói; các file feature đã được hợp nhất vào `main`. Backend prefix `tests/SCDC.Api.Tests/Community/`; browser prefix `clients/WebClient/e2e/`; Node prefix `clients/WebClient/tests/`. Các method theory có nhiều trường hợp trong TRX. Bảng giữ assertion đại diện cho từng nhóm tiêu chí; đọc toàn file/acceptance để xem fixture, fault injection và các assertion bổ sung. Những mô tả chưa merge trong acceptance cũ phản ánh thời điểm kiểm chứng của gói; trạng thái hiện tại theo [status](../../../features/community/README.md).

<a id="main-merge"></a>

<a id="kiểm-chứng-hợp-nhất-vào-main--2026-10-08"></a>

## Kiểm chứng hợp nhất vào main — 2026-10-08

Hợp nhất `feat/community-channels` tại `2052c60` vào `main` nền `998daf8`, giữ lịch sử các gói. Code backend/WebClient, SQL và test không thay đổi so với nhánh nguồn; xử lý xung đột README, bỏ các trang tài liệu cũ đã được tách và cập nhật hiện trạng/hướng dẫn trên `main`. Không đưa các nhánh Messaging vào lần hợp nhất này.

Môi trường: .NET SDK 10.0.112, PostgreSQL 18 trong container Podman tạm thời không gắn volume ứng dụng, Node/Chromium trong image `mcr.microsoft.com/playwright:v1.63.0-noble` đã có sẵn. DB tổng hợp `scdc_community_main_merge_test` ở `127.0.0.1:15438`; API/Vite ở cổng 15038/15338. Nạp `database/postgres/schema.sql` vào DB mới trước khi chạy; không migrate DB ứng dụng.

| Kiểm tra thực tế | Kết quả |
|---|---|
| `dotnet build SCDC.slnx --configuration Release --no-restore -m:1` | Đạt, 0 warning / 0 error |
| `dotnet test SCDC.slnx --configuration Release --no-build --no-restore -m:1` trên DB riêng | 191 đạt, 0 lỗi, 0 bỏ qua; TRX tại `artifacts/community-main-merge/backend/community-main-merge.trx` |
| WebClient `npm test` | 24 đạt, 0 lỗi, 0 bỏ qua |
| WebClient `npm run build` | Production build đạt, Vite 8.2.2 |
| `npm run test:e2e -- --output ../../artifacts/community-main-merge/browser --reporter=line` với API/DB thật | 50 Chromium đạt trong 54,4 giây; log `artifacts/community-main-merge/browser.log` |
| `podman-compose config` với khóa tổng hợp chỉ cho kiểm tra | Đạt cấu hình HMAC và volume keyring Community |

Các artifact local bị ignore; checkout mới cần chạy lại theo [hướng dẫn](../../../guides/community-development.md). Kết quả này kiểm chứng nền Community/Identity hiện có và migrations, không thay proof tích hợp writer tin, Hub/dispatcher, reconnect hoặc deadline thu hồi ≤5 giây. Các gate chat MVP tiếp tục chưa được nghiệm thu; giới hạn theo [phần chưa kiểm chứng](#unverified).

<a id="backend"></a>

<a id="đối-chiếu-backend"></a>

## Đối chiếu backend

| UC / AC / TC và phạm vi | Test thực tế | Kết quả và giới hạn |
|---|---|---|
| UC-COM-01; phần tạo AC-COM-29/36, TC-COM-02/23 | `CommunityApiTests.cs::Create_initializes_owner_everyone_and_pending_outbox_and_retry_reads_current_detail` | Đạt: server/owner/@everyone/operation/outbox nguyên tử, retry đọc detail hiện hành. Sửa server/private switch chưa có |
| UC-COM-01; TC-COM-19/23/26 | `CommunityApiTests.cs::Utf16_limits_emoji_and_preserved_whitespace_follow_the_contract`; `CommunityConcurrencyTests.cs::Failure_after_entity_inserts_rolls_back_server_member_role_operation_and_event` | Đạt giới hạn đầu vào và rollback create; không thay proof mọi mutation |
| UC-COM-01/03; operation/restart và account guard | `CommunityApiTests.cs::Persisted_retry_survives_restart_rotation_and_missing_old_key_fails_closed`; `CommunityConcurrencyTests.cs::Identity_security_writer_waits_for_create_commit_then_old_token_is_rejected` | Đạt retry qua restart/rotation và Identity writer chờ create commit; không có proof session revoke trên Hub |
| Phần UC-COM-03; private/own membership, TC-COM-11/16 | `CommunityApiTests.cs::Public_projection_hides_private_data_and_private_own_left_membership_does_not_restore_access`; `CommunityApiTests.cs::List_uses_keyset_and_cursor_rejects_cross_actor_limit_and_tamper_and_survives_restart` | Đạt projection/list/detail/cursor và left fixture; chưa có leave API hoặc lịch sử tin |
| UC-COM-02; AC-COM-01/37, TC-COM-01/18 | `CommunitySearchTests.cs::Search_only_returns_public_active_summaries_and_never_creates_membership`; `CommunitySearchTests.cs::Search_normalizes_case_and_nfc_preserves_accents_and_ranks_duplicate_exact_names_first` | Đạt public discovery, không tự join, Unicode/exact rank; approval chưa có |
| UC-COM-02; phần cursor/visibility AC-COM-37, TC-COM-18 | `CommunitySearchTests.cs::Search_uses_keyset_across_exact_and_partial_names_and_cursor_binds_actor_query_limit_and_purpose`; `CommunitySearchTests.cs::Search_old_cursor_excludes_newly_private_inactive_and_deleted_servers` | Đạt scope cursor và recheck fixture thay trạng thái; không chứng minh API private switch/pending |
| Phần UC-COM-06; AC-COM-02, TC-COM-03 | `CommunityJoinTests.cs::Join_commits_default_membership_and_one_event_and_active_retries_survive_restart`; `CommunityJoinTests.cs::Concurrent_join_waits_for_server_lock_then_returns_the_committed_epoch_without_another_event` | Đạt public/immediate, retry/đồng thời một epoch/event; approval trả conflict theo gói, chưa tạo pending |
| Phần rejoin AC-COM-35, TC-COM-16/22 | `CommunityJoinTests.cs::Rejoin_rotates_epoch_and_removes_old_assignments_overrides_and_legacy_fields`; `CommunityJoinTests.cs::Outbox_fault_rolls_back_rejoin_cleanup_membership_and_server_versions` | Đạt rejoin từ left fixture, cleanup và rollback kể cả ACL version; chưa có lifecycle leave/request/invitation |
| UC-COM-20/21; AC-COM-28/36, TC-ACL-06/09 | `CommunityRoleTests.cs::Roles_crud_assignment_union_noops_and_delete_update_current_permissions_atomically`; `CommunityRoleTests.cs::Role_last_slot_is_serialized_and_only_one_create_commits` | Đạt role/assignment, union/no-op/limit/system restrictions; tác động live chat còn thiếu |
| UC-COM-21; phần epoch AC-COM-35, TC-COM-16/22 | `CommunityRoleTests.cs::Assignment_cas_and_epoch_foreign_keys_stop_requests_from_before_rejoin`; `CommunityRoleConcurrencyTests.cs::Concurrent_member_role_sets_use_one_epoch_and_one_version_winner` | Đạt CAS/FK epoch và writer đồng thời; không thay proof stale subscription |
| Policy dùng cho UC-COM-17/20/21/22; TC-ACL-08 | `CommunityPermissionPolicyTests.cs::Evaluator_matches_published_view_and_management_policy` | Đạt fixture domain; bảng API bên dưới chứng minh view HTTP, chưa có send/Hub consumer |
| UC-COM-16 text; AC-COM-07/11, TC-COM-23 | `CommunityChannelTests.cs::Channel_create_space_metadata_operation_event_and_replay_are_atomic_and_current`; `CommunityChannelConcurrencyTests.cs::Channel_create_replay_survives_restart_key_rotation_and_remains_scoped_per_server` | Đạt create text/space/operation/outbox và replay; voice và lifecycle delete chưa có |
| Phần UC-COM-17/22; AC-COM-06/25/26/27, TC-ACL-01–05/12 | `CommunityChannelTests.cs::Channel_list_and_detail_match_default_role_deny_wins_and_personal_priority`; `CommunityChannelTests.cs::Channel_owner_still_requires_current_identity` | Đạt list/detail, role deny-wins/personal/owner, account/session nền; chưa đọc/gửi/nhận tin hoặc DELETE |
| Phần UC-COM-17; AC-COM-06, TC-ACL-12 | `CommunityChannelTests.cs::Channel_pages_filter_hidden_before_limit_recheck_access_and_bind_cursor_to_scope` | Đạt lọc trước LIMIT, recheck view và cursor phòng; không phải cursor lịch sử tin |
| UC-COM-18; phần edit AC-COM-34/38/42, TC-COM-19 và TC-ACL-12 trừ DELETE | `CommunityChannelTests.cs::Channel_unicode_collision_topic_limits_and_kind_immutability_are_enforced`; `CommunityChannelTests.cs::Channel_management_does_not_grant_hidden_view_or_other_management_and_acl_self_removal_commits` | Đạt edit name/topic/version/no-op, hidden manager và kind bất biến; nhánh delete/cutoff của AC-COM-34 chưa có |
| Phần UC-COM-22; AC-COM-16/42, TC-ACL-10/12 | `CommunityChannelTests.cs::Acl_full_replacement_rejects_cross_scope_duplicates_stale_epochs_and_versions_without_partial_state`; `CommunityChannelConcurrencyTests.cs::Concurrent_channel_create_one_operation_and_concurrent_acl_compare_and_swap_have_one_writer` | Đạt full snapshot ACL, CAS/epoch/cross-scope, writer thắng duy nhất; subscription chưa có |
| UC-COM-20/22; phần ACL version TC-ACL-11 | `CommunityChannelConcurrencyTests.cs::Removing_role_override_advances_channel_access_version_and_prevents_stale_acl_replacement` | Đạt role cleanup tăng accessVersion và chặn ACL snapshot cũ; chưa đo thu hồi live chat khi view đổi |
| Nền UC-COM-23/25; phần transaction TC-COM-25 | `CommunityChannelConcurrencyTests.cs::Channel_admission_guard_holds_server_and_channel_share_locks_until_caller_commit` | Đạt caller transaction giữ share lock, role/ACL writer chờ; caller là test transaction, chưa phải message writer/Hub, không đạt deadline ≤5 giây |
| UC-COM-16/22; rollback lifecycle/outbox/lease | `CommunityChannelConcurrencyTests.cs::Channel_outbox_fault_rolls_back_space_channel_acl_operation_and_versions`; `CommunityChannelConcurrencyTests.cs::Lifecycle_failure_and_expired_commit_lease_do_not_leave_orphaned_spaces` | Đạt rollback create/ACL/space/operation/version; không suy dispatcher đã publish |
| Migration 001–004; phần TC-COM-26 của gói đã làm | `CommunityMigrationTests.cs::Legacy_mapping_is_explicit_and_preserves_version_and_membership`; `CommunitySearchMigrationTests.cs::Search_upgrade_backfills_multiple_batches_and_preserves_display_versions_visibility_and_memberships`; `CommunityRoleMigrationTests.cs::Role_upgrade_preserves_names_permissions_timestamps_and_epochs_then_replays`; `CommunityChannelMigrationTests.cs::Channel_upgrade_reviewed_mapping_crosses_batch_boundary_preserves_names_epochs_space_and_acl_then_replays` | Đạt reviewed mapping, batch, bảo toàn dữ liệu/epoch và ledger trong fixture; pending/invitations/Media/restore ngoài phạm vi |
| Migration 004 fail/rollback; TC-COM-26 | `CommunityChannelMigrationTests.cs::Channel_upgrade_preflight_requires_review_and_preserves_invalid_legacy_data`; `CommunityChannelMigrationTests.cs::Channel_upgrade_fault_rolls_back_ddl_keys_and_trigger_and_requires_baselines`; `CommunityChannelMigrationTests.cs::Channel_bootstrap_seed_and_ledger_agree_with_active_name_uniqueness` | Đạt preflight/rollback/bootstrap/ledger; không chứng minh nâng cấp production đã thực hiện |

<a id="frontend"></a>

<a id="đối-chiếu-webclient"></a>

## Đối chiếu WebClient

Các hàng browser dưới đây đạt tại `f8d582d` trong suite 50 Chromium; các hàng Node đạt trong suite 24 Node. Browser sử dụng API/DB thật cho CRUD/quyền/conflict; mất/trễ response, lỗi 401/503/cursor/storage được tiêm theo acceptance. Không gọi toàn bộ suite là proof Hub hoặc không fault injection.

| UC / tình huống | Test thực tế | Phần chưa chứng minh |
|---|---|---|
| UC-COM-01/03 tạo → detail/list/reload | `community.spec.js::real login → empty list → private create → detail → list → reload, with no mock channels or hub` | Phòng/tin/Hub không nằm trong ca này |
| TC-COM-23 response create mất rồi phục hồi | `community.spec.js::lost response after commit survives reload and explicit replay returns 200 without duplicate` | Không thay proof operation gửi tin |
| UC-COM-06 mobile join/reload; kết quả không rõ | `communityJoin.spec.js::shared public URL → real join → default member detail/list/reload on mobile`; `communityJoin.spec.js::lost join response after commit is reconciled by GET with no replay POST` | Approval/lời mời/leave chưa có |
| UC-COM-02 → preview → join, TC-COM-18 | `communitySearch.spec.js::real search → public preview → join → same query → own list on mobile`; `communitySearch.spec.js::real keyset pages reset after a cursor failure and never reuse a cursor for new query` | Gửi request pending và API private switch chưa có |
| UC-COM-20/21 mobile role/assignment | `communityRoles.spec.js::owner creates Unicode role, assigns/revokes, edits and deletes through real APIs on mobile`; `communityRoles.spec.js::a concurrent real role edit conflicts; UI rereads and only saves after another explicit action` | Thu hồi trên kết nối chat chưa có |
| UC-COM-16/18/22 và phần HTTP TC-ACL-02/03/10 | `communityChannels.spec.js::mobile real create/edit and everyone deny with personal allow persist and filter a member`; `communityChannels.spec.js::manage_channel_access member can self-remove view and UI drops hidden metadata after commit` | Lịch sử/gửi/nhận/subscribe chưa có |
| TC-COM-23 create phòng; CAS metadata/ACL | `communityChannels.spec.js::lost committed create response survives reload and replays the exact operation once`; `communityChannels.spec.js::real concurrent ACL edit reloads accessVersion without automatically replacing the snapshot` | Operation tin và dispatcher chưa có |
| Actor/selection, roster epoch của UC-COM-17/22 | `communityChannels.spec.js::delayed channel selection and a later actor switch cannot restore stale private state`; `communityChannels.spec.js::member pagination retains an existing epoch override while loading another page` | Mất quyền live khi không phát sinh HTTP chưa có |
| Validation/storage phòng | `communityChannels.test.js::channel Unicode limits, topic normalization and published create defaults match the API contract`; `communityChannels.test.js::corrupt/noncanonical/discarded channel storage blocks replacement and POST` | Node test không chứng minh API/transaction |

<a id="unverified"></a>

<a id="bằng-chứng-chưa-có"></a>

## Bằng chứng chưa có

| Phạm vi đầy đủ còn thiếu | UC/AC/TC liên quan | Việc cần chứng minh |
|---|---|---|
| Lịch sử và gửi tin | UC-COM-17/23; AC-COM-12 phần gửi, 13/14/15/17/18/22/24; TC-COM-09/10/12/25 | Reader/writer Messaging thật, sequence/pagination/retry/lease/rollback và quyền hiện hành. Guard/space create không thay test tin |
| Realtime và mất quyền | UC-COM-25; phần nhận/thu hồi của AC-COM-11/25/26/42; TC-ACL-07/11, TC-COM-25 | Dispatcher/Hub, registry/subscription/resume, dedup/reconnect/routing và đo từ commit đến cutoff chat ≤5 giây; proof session/account/role/ACL/fail-close |
| Mutation quản lý mở rộng | UC-COM-04/05/07–15/19/24; AC/TC theo [traceability](../../../features/community/traceability.md#use-case-coverage) | Pending/invitation/leave/transfer/delete/edit tin và các race riêng. Fixture chỉnh DB/left không thay API lifecycle |
| Voice, retention/restore và vận hành | Phần Media của UC-COM-16/17/25 và DATA-GAP/RLS-GAP theo nguồn | Media admission/cutoff, worker/kho bảo vệ/restore, tải/RAM và môi trường phát hành cần thực thi theo gói; chưa có kết quả đo từ suite Community |

Khi thêm gói, cập nhật test/commit/actual result và giới hạn từng hàng; giữ nguyên số ca và kết quả lịch sử ở hồ sơ cũ. Bổ sung hoặc bỏ assertion phải ghi lý do. Không đổi “chưa kiểm chứng” thành “đạt” chỉ từ schema/OpenAPI hoặc một mock.

## Quy trình và thứ tự gói đã ghi nhận

Phần dưới giữ thỏa thuận và thứ tự gói tại thời điểm bàn giao cũ; không đặt thêm điều kiện phê duyệt cho công việc đã được cho phép. Quy trình dự án hiện hành ở [workflow](../../../project/workflow.md).

<a id="history-use-case-delivery"></a>

<a id="history-kế-hoạch-và-gói-triển-khai-đầu-tiên"></a>

### Kế hoạch và gói triển khai đầu tiên

Áp dụng [quy trình dự án](../../../project/workflow.md#process) cho từng nhóm UC: rà soát luồng/ngoại lệ và AC/TC → đối chiếu UX, API, dữ liệu/giao dịch → xác định phụ thuộc và gói việc → triển khai cùng kiểm thử → tích hợp và ghi bằng chứng. Các bước được lặp theo nhóm chức năng; không đợi hoàn tất mọi module mới kiểm thử luồng đầu tiên.

Trước mỗi bước, trình bày phạm vi, đầu ra và nội dung cần quyết định để người dùng duyệt. Sau mỗi bước, báo những gì đã làm, kết quả kiểm tra và phần còn thiếu. Việc duyệt một bước chỉ áp dụng cho phạm vi bước đó.

Theo thỏa thuận ngày 2026-10-07, các phần đã hoàn thành và kiểm tra được commit theo tiến độ: tài liệu độc lập trên `main`, code trên nhánh theo gói chức năng (gói đầu là `feat/community-create-view`). Người dùng đã chọn tự push sau lỗi xác thực HTTPS của môi trường. Quyền commit/push không thay thế việc duyệt bước tiếp theo hoặc duyệt merge nhánh code.

Các nhóm dưới đây phân loại đặc tả đầy đủ, không bắt buộc hoàn thành cả nhóm 3 rồi mới làm nhóm 4. Theo [phạm vi MVP đã chọn](../../../releases/mvp.md#community-scope), nền tin/lịch sử/gửi/nhận và mất quyền được ưu tiên sau các gói nền. Thứ tự công việc, phụ thuộc và đầu mối được quản lý tại [COM-W01–13](../../../project/planning.md#community-work-items); phần mở rộng vẫn giữ đặc tả v1.

Bố cục gói năm 2026-10-07 dùng plan/design/acceptance riêng. Từ 2026-10-11, phạm vi, thiết kế và trạng thái của gói được cập nhật trong chủ đề; kết quả theo revision lưu tại `records/verification/`. Nội dung cần chuẩn bị và cách kiểm chứng theo [quy trình](../../../project/workflow.md#topic).

| Nhóm | Use case | Đầu vào kỹ thuật cần có | Đầu ra cần kiểm chứng |
|---|---|---|---|
| 1. Server và tư cách | [UC-COM-01](../../../features/community/servers.md#uc-com-01), [UC-COM-02](../../../features/community/servers.md#uc-com-02), [UC-COM-03](../../../features/community/servers.md#uc-com-03), [UC-COM-04](../../../features/community/servers.md#uc-com-04), [UC-COM-06](../../../features/community/memberships.md#uc-com-06) | Account guard, shared transaction, migration server/membership/@everyone/version/operation và search | Tạo nguyên tử, join trực tiếp, list/detail đúng quyền và tên Unicode; private switch và đóng pending của [UC-COM-04](../../../features/community/servers.md#uc-com-04), [UC-COM-06](../../../features/community/memberships.md#uc-com-06) hoàn thiện cùng request ở nhóm 3 |
| 2. Vai trò và phòng | [UC-COM-16](../../../features/community/channels.md#uc-com-16), [UC-COM-17](../../../features/community/channels.md#uc-com-17), [UC-COM-18](../../../features/community/channels.md#uc-com-18), [UC-COM-19](../../../features/community/channels.md#uc-com-19), [UC-COM-20](../../../features/community/access-control.md#uc-com-20), [UC-COM-21](../../../features/community/access-control.md#uc-com-21), [UC-COM-22](../../../features/community/access-control.md#uc-com-22) | Permission evaluator/catalog, migration role/ACL/epoch, channel guard và Messaging lifecycle | Role limit, assignment/ACL nguyên tử, hidden channel, create/delete và race quyền; voice cần Media lifecycle riêng |
| 3. Tham gia và lời mời | [UC-COM-05](../../../features/community/servers.md#uc-com-05), [UC-COM-06](../../../features/community/memberships.md#uc-com-06), [UC-COM-07](../../../features/community/memberships.md#uc-com-07), [UC-COM-08](../../../features/community/memberships.md#uc-com-08), [UC-COM-09](../../../features/community/invitations.md#uc-com-09), [UC-COM-10](../../../features/community/invitations.md#uc-com-10), [UC-COM-11](../../../features/community/invitations.md#uc-com-11), [UC-COM-12](../../../features/community/invitations.md#uc-com-12), [UC-COM-13](../../../features/community/invitations.md#uc-com-13), [UC-COM-14](../../../features/community/servers.md#uc-com-14), [UC-COM-15](../../../features/community/memberships.md#uc-com-15) | Role/quyền từ nhóm 2, request/invitation migration, token protection/key ring, guard actor/target | Pending/approve/cancel/private switch, lượt cuối, accept/expiry, transfer/leave/rejoin và retry |
| 4. Tin phòng và cập nhật | [UC-COM-17](../../../features/community/channels.md#uc-com-17), [UC-COM-23](../../../features/messaging/channel-messaging.md#uc-com-23), [UC-COM-24](../../../features/messaging/channel-messaging.md#uc-com-24), [UC-COM-25](../../../features/messaging/channel-messaging.md#uc-com-25) | Messaging writer/history dùng chung DM, outbox/Hub/registry/revoker và frontend API | Lưu bền/chống trùng/tác giả, reconnect/routing, thu hồi chat ≤5 giây, scope cache đúng epoch |

Các nhóm gồm API và trạng thái frontend tương ứng, có kiểm thử quyền/đồng thời ngay trong gói. Các guard/lifecycle/shared transaction và migrations được triển khai theo phạm vi từng gói; scope room deleted/restore/media và các đầu vào chưa chốt tiếp tục được theo dõi ở [vấn đề còn mở](../../../project/planning.md#community-work-items), không đánh dấu đã nghiệm thu từ danh mục UC.


<a id="history-các-bước-và-đầu-ra-đã-thống-nhất"></a>

### Các bước và đầu ra đã thống nhất

Trạng thái các bước nằm tại [các chủ đề Community](#history-steps); bảng này chỉ giữ đầu ra và thứ tự thực hiện.

| Bước | Đầu ra cần bàn giao |
|---|---|
| 1. Chốt phạm vi và thứ tự | Gói đầu UC-COM-01 và phần danh sách/detail/tư cách của UC-COM-03; tiêu chí hoàn thành, phần để sau và thứ tự phụ thuộc |
| 2. Rà soát nghiệp vụ gói đầu | [Kết quả rà soát](create-view.md#scope-first-package-business): điều kiện tạo, trạng thái ban đầu, ownership/membership/@everyone, dữ liệu hợp lệ, luồng lỗi và ngoại lệ; ghi riêng đầu vào kỹ thuật còn cần chốt |
| 3. Chốt thiết kế kỹ thuật | [Thiết kế gói tạo/xem](../../../features/community/servers.md): model/schema/migration, API/DTO/lỗi, transaction/retry, hợp đồng Identity và kế hoạch kiểm thử |
| 4. Triển khai backend | Persistence, application, DI/API và kiểm thử quyền/tạo nguyên tử/thử lại/đọc dữ liệu |
| 5. Giao diện và nghiệm thu gói đầu | Nối form/danh sách/detail với API; chạy luồng thật và ghi bằng chứng theo tiêu chí gói |
| 6. Mở rộng Community theo gói | Các gói nền: join public/immediate, search, role/assignment, phòng text/ACL. Các đường approval/lời mời/leave và quản lý mở rộng có kế hoạch riêng; không chặn nền tin của MVP chỉ vì còn ngoài scope. Phạm vi từng gói được duyệt riêng. |
| 7. Tích hợp Messaging và realtime | Messaging lưu/đọc/gửi tin phòng trên quyền Community; sau đó kiểm chứng Hub, reconnect và xử lý mất quyền |

Các gói đầu của bước 6: [tham gia trực tiếp public/immediate](direct-join.md) trên `feat/community-join`, rồi [tìm kiếm công khai](search.md) trên `feat/community-search`. Mỗi nhánh kế thừa gói trước; requests và các gói còn lại được duyệt riêng. Bằng chứng và phạm vi còn lại ở [các chủ đề Community](../../../features/community/README.md).

Evaluator Permissions được triển khai ở mức cần cho gói hiện hành và đối chiếu fixture khi mở role/ACL. Outbox/revoker cho mutation thu hồi phải hoàn thiện trước khi gói đó được coi đạt; tin phòng/realtime/Media có bằng chứng riêng theo phụ thuộc.

## Các bước đã ghi nhận ngày 2026-10-08

<a id="history-steps"></a>

<a id="history-các-bước-đã-thống-nhất"></a>

### Các bước đã thống nhất

| Bước | Trạng thái | Nguồn |
|---|---|---|
| 1. Chốt phạm vi | Đã duyệt gói tạo/xem | [Kế hoạch](create-view.md#scope-first-package) |
| 2. Rà soát nghiệp vụ | Đã rà soát theo quyết định hiện có | [Rà soát](create-view.md#scope-first-package-business) |
| 3. Chốt thiết kế | Đã chốt thiết kế gói ngày 2026-10-07 | [Thiết kế](../../../features/community/servers.md) |
| 4. Backend | Đã triển khai và ghi bằng chứng | [Nghiệm thu backend](create-view.md#results-backend) |
| 5. WebClient và kiểm chứng luồng | Đã triển khai và ghi bằng chứng | [Nghiệm thu frontend](create-view.md#results-frontend) |
| Tổ chức lại tài liệu trước bước 6 | Đã áp dụng bố cục mới cho Community | [Tổng quan](../../../features/community/README.md), [quy ước quản lý](../../../features/community/README.md#mục-lục) |
| 6. Mở rộng Community theo gói | Join public/immediate, search/discovery, role/assignment và phòng text/ACL đã kiểm chứng trong phạm vi từng gói; các gói còn lại chưa bắt đầu | [Gói join](direct-join.md), [tìm kiếm](search.md), [vai trò](roles.md), [phòng/ACL](channels-access.md) |
| 7. Messaging và realtime | Chưa bắt đầu | [Tích hợp](../../../features/messaging/channel-messaging.md#use-cases) |
