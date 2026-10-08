# SCDC — Đối chiếu UC/AC/TC với bằng chứng Community

Cập nhật: 2026-10-08. Bảng này nối [truy vết đặc tả đầy đủ](../specs/traceability.md#use-case-coverage) với test thực tế của các gói đã nghiệm thu. Trạng thái công việc chỉ ghi tại [status](../status.md); gate cho mốc đã chọn ở [MVP](../../../releases/mvp.md#community-acceptance). “Đạt phần” chỉ áp dụng assertion nêu trong hàng, không đóng toàn bộ UC/AC/TC có lịch sử/tin/Media/realtime.

## Revision và cách đọc bằng chứng

| Gói | Backend / WebClient commit | Kết quả lịch sử và nguồn |
|---|---|---|
| Tạo/xem | `79fa627` / `4203e05` | 63 backend, 16 Node, 14 Chromium; [acceptance](create-view/acceptance.md) |
| Public/immediate join | `e5b5750` / `e0cf116` | 81 backend, 16 Node, 24 Chromium; [acceptance](direct-join/acceptance.md) |
| Search | `7a883b7` (fixture patch `b10851b`) / `b5be14f` | 111 backend, 18 Node, 31 Chromium; [acceptance](search/acceptance.md) |
| Roles | `9a513d1` / `eae64c3` | 158 backend, 21 Node, 39 Chromium; [acceptance](roles/acceptance.md) |
| Phòng text/ACL | `eaaa2e3` / `f8d582d` | 191 backend, 24 Node, 50 Chromium; Release/production build đạt; [acceptance](channels-access/acceptance.md) |

Các số là tổng suite tại từng revision, gồm hồi quy; không cộng các gói để tính tổng test hoặc tiến độ. Bảng bên dưới đối chiếu source backend `eaaa2e3`, WebClient `f8d582d` trên `feat/community-channels` với kết quả gói cuối. Bổ sung tài liệu này không chạy lại test sản phẩm. TRX đã ghi ở `artifacts/community-channels/backend/community-channels.trx`, browser output/ảnh ở `artifacts/community-channels/`; artifact local bị ignore, không bảo đảm có sẵn trong checkout mới. Muốn bằng chứng mới dùng [hướng dẫn chạy lại](../development.md), ghi revision và actual result mới vào acceptance tương ứng.

Tên test dùng dạng `file::method` hoặc `file::test title`, tra theo commit nguồn của gói; các file feature đã được hợp nhất vào `main`. Backend prefix `tests/SCDC.Api.Tests/Community/`; browser prefix `clients/WebClient/e2e/`; Node prefix `clients/WebClient/tests/`. Các method theory có nhiều trường hợp trong TRX. Bảng giữ assertion đại diện cho từng nhóm tiêu chí; đọc toàn file/acceptance để xem fixture, fault injection và các assertion bổ sung. Những mô tả chưa merge trong acceptance cũ phản ánh thời điểm kiểm chứng của gói; trạng thái hiện tại theo [status](../status.md).

<a id="main-merge"></a>

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

Các artifact local bị ignore; checkout mới cần chạy lại theo [hướng dẫn](../development.md). Kết quả này kiểm chứng nền Community/Identity hiện có và migrations, không thay proof tích hợp writer tin, Hub/dispatcher, reconnect hoặc deadline thu hồi ≤5 giây. Các gate chat MVP tiếp tục chưa được nghiệm thu; giới hạn theo [phần chưa kiểm chứng](#unverified).

<a id="backend"></a>

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

## Bằng chứng chưa có

| Phạm vi đầy đủ còn thiếu | UC/AC/TC liên quan | Việc cần chứng minh |
|---|---|---|
| Lịch sử và gửi tin | UC-COM-17/23; AC-COM-12 phần gửi, 13/14/15/17/18/22/24; TC-COM-09/10/12/25 | Reader/writer Messaging thật, sequence/pagination/retry/lease/rollback và quyền hiện hành. Guard/space create không thay test tin |
| Realtime và mất quyền | UC-COM-25; phần nhận/thu hồi của AC-COM-11/25/26/42; TC-ACL-07/11, TC-COM-25 | Dispatcher/Hub, registry/subscription/resume, dedup/reconnect/routing và đo từ commit đến cutoff chat ≤5 giây; proof session/account/role/ACL/fail-close |
| Mutation quản lý mở rộng | UC-COM-04/05/07–15/19/24; AC/TC theo [traceability](../specs/traceability.md#use-case-coverage) | Pending/invitation/leave/transfer/delete/edit tin và các race riêng. Fixture chỉnh DB/left không thay API lifecycle |
| Voice, retention/restore và vận hành | Phần Media của UC-COM-16/17/25 và DATA-GAP/RLS-GAP theo nguồn | Media admission/cutoff, worker/kho bảo vệ/restore, tải/RAM và môi trường phát hành cần thực thi theo gói; chưa có kết quả đo từ suite Community |

Khi thêm gói, cập nhật test/commit/actual result và giới hạn từng hàng; giữ nguyên số ca và kết quả lịch sử ở hồ sơ cũ. Bổ sung hoặc bỏ assertion phải ghi lý do. Không đổi “chưa kiểm chứng” thành “đạt” chỉ từ schema/OpenAPI hoặc một mock.
