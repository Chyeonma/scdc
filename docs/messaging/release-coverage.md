# P9-T01 — Ma trận kiểm thử Messaging cho mốc phát hành

Nhánh `test/msg-p9-t01-release-coverage` được xếp chồng lên `feat/msg-p8-t03-moderation`. Các thay đổi chưa vào `main` của P7-T02, P8-T01 và P8-T02 đã được tích hợp vào nhánh này để kiểm tra chung attachment, search, block và moderation. PR của task này cần giữ dạng draft cho tới khi toàn bộ integration test chạy trên PostgreSQL test và đạt.

| Phạm vi | Test | Điều kiện cần chứng minh |
|---|---|---|
| PostgreSQL: constraint, rollback | `MessagingReleaseCoverageTests.PostgreSql_constraints_reject_invalid_and_duplicate_messages_and_rollback_partial_writes` | Check constraint và unique index từ chối dữ liệu sai; transaction lỗi không để lại tin nhắn |
| PostgreSQL: idempotency, cursor, concurrency | `DirectConversationFlowTests.Sending_a_message_is_idempotent_and_updates_the_space_and_outbox_together`, `History_uses_stable_before_and_after_cursors_and_hides_tombstone_content`, `Concurrent_sends_are_serialized_and_leave_the_latest_sequence_as_the_space_projection` | Không nhân đôi tin, cursor ổn định và projection giữ sequence mới nhất |
| Login → DM → realtime → refresh → reconnect | `MessagingReleaseCoverageTests.Login_to_dm_realtime_refresh_and_reconnect_catch_up_keeps_each_message_once`; `releaseJourney.test.mjs` | Event đến qua Hub, phiên mới đọc được lịch sử, tải bù tin khi mất kết nối và không nhân đôi trên client |
| Group/channel, thu hồi quyền, sửa/xóa, unread | `UnifiedSpaceMessageFlowTests.Dm_group_and_channels_share_message_flow_and_enforce_current_rights`, `Edit_and_delete_enforce_author_version_and_tombstone`, `Read_state_is_monotonic_and_unread_counts_actual_messages_across_spaces` | Các loại space chung flow; quyền hiện tại, version và trạng thái đọc được thực thi |
| File, report và truy cập chéo space | `UnifiedSpaceMessageFlowTests.Attachment_upload_is_scanned_staged_and_owned_by_the_sender_and_space`, `Reports_require_message_access_and_moderator_actions_remove_with_audit`, `Search_finds_unloaded_history_and_respects_edits_deletes_filters_and_revoked_access` | File chỉ tải được trong đúng space; tin có file có thể báo cáo; quyền report/download mất sau revoke; search không lộ tin |
| XSS nội dung và tên file | `renderSafety.test.mjs` | Markup độc hại được escape khi render message và attachment |
| Hết hạn phiên, rate limit, log | `releaseJourney.test.mjs`; `MessagingReleaseCoverageTests.Login_to_dm_realtime_refresh_and_reconnect_catch_up_keeps_each_message_once`, `Message_rate_limit_rejects_the_next_send_without_persisting_it`, `Default_request_logging_does_not_record_access_tokens_or_message_content` | Refresh lỗi xóa phiên client; logout-all làm token cũ mất quyền đọc; request thứ 31 bị từ chối và không ghi DB; log mặc định không chứa token/nội dung chat |
| Ranh giới module | `ModuleBoundaryTests.Feature_modules_reference_only_contracts_and_building_blocks` | Các module chỉ reference Contracts và BuildingBlocks, không reference trực tiếp nhau |

Chạy từ repository root với database test riêng `scdc_chat_test` trên cổng `5433` (xem [fixture](testing-fixtures.md)):

```powershell
docker compose -f compose.test.yaml up -d
dotnet test tests/SCDC.Api.Tests/SCDC.Api.Tests.csproj
npm.cmd --prefix clients/WebClient test
npm.cmd --prefix clients/WebClient run build
dotnet build SCDC.slnx
```

Các test frontend dùng HTTP mock và SSR component; chúng không thay thế kiểm thử trình duyệt với backend thật. Suite API dùng `SCDCWebApplicationFactory`, Identity API thật, PostgreSQL thật và SignalR test server. Test tạo actor riêng và dọn dữ liệu theo ID. Không chạy suite với database development/production.

Tại thời điểm lập PR này, frontend test, build, và test ranh giới module đã chạy đạt; PostgreSQL `localhost:5433` chưa hoạt động và Docker daemon không sẵn sàng. Vì vậy các hàng cần PostgreSQL chỉ mới được biên dịch, chưa được nghiệm thu. Sau khi fixture hoạt động, chạy toàn bộ lệnh trên, sửa bất kỳ lỗi mất/lộ/nhân đôi dữ liệu nào, rồi mới chuyển PR khỏi draft và đánh dấu các subtask còn lại trong roadmap.

Rà soát tĩnh đã phát hiện hai lỗi và nhánh này đã sửa ở mức code: retry cùng `clientMessageId` trả lại tin đã lưu ngay cả khi cửa sổ rate limit đầy; upload và gửi tin có file trong channel đòi `CanAttach` độc lập với `CanSend`. Migration `20261002_p9_t01_channel_attachments.sql` cấp `attach_files` cho default/Owner role hiện hữu; role tùy chỉnh cần cấp quyền này rõ ràng. Server mới cấp quyền cho role mặc định/Owner khi tạo. Test hồi quy đã bổ sung, nhưng hai sửa đổi vẫn cần kiểm chứng với PostgreSQL thật trước khi nghiệm thu.
