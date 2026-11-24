# CI — Kiểm thử cạnh tranh khi mở DM

Hồ sơ ghi lỗi kiểm thử sau khi S2-10 vào `main` tại `77466fa` và cách kiểm chứng bản sửa trên nhánh `fix/ci-dm-concurrency`.

**Trạng thái:** hoàn tất kiểm chứng local ngày 2026-11-24; 381/381 backend đạt trên PostgreSQL 18 và Redis thật. CI GitHub được theo dõi trên PR của nhánh.

## Lỗi và nguyên nhân

[CI trên main](https://github.com/Chyeonma/scdc/actions/runs/45429689121) đạt 378/379 backend; ca `Forty_barrier_requests_force_unique_conflicts_and_leave_only_winning_space` không chờ được đủ 40 request tại advisory lock trong 20 giây.

Barrier nằm trong transaction Messaging, sau khi insert space. Từ S2-08, runtime đặt `lock_timeout=500ms`, `statement_timeout=1s` và `transaction_timeout=1s`. Nếu các request đến lệch nhau, request trước hết hạn trước khi request cuối đến barrier. Tăng thời gian chờ của helper không giải quyết được điều kiện này.

Trên máy local với `DOTNET_PROCESSOR_COUNT=2`, barrier cũ tái hiện cùng lỗi khi request cuối đến muộn 1,5 giây. Lượt chạy riêng không có độ lệch vẫn có thể đạt.

## Thay đổi

- Hai ca gửi 40 request, chia đều hai chiều, cùng start barrier ở phía client; một ca cho request cuối đến muộn 1,5 giây. Cả hai phải trả cùng ID, có đúng một space và hai thành viên. Mỗi ca dùng cặp tài khoản khác nhau.
- Ca unique conflict dùng interceptor riêng trong test: sau khi request thua đã insert space và kiểm tra cặp, một request HTTP thật ở chiều ngược lại tạo và commit hội thoại. INSERT của request thua phải nhận PostgreSQL `23505` tại `ux_direct_conversations_pair`, rollback space thừa và trả về hội thoại thắng.
- Khởi tạo fence của hai actor trước khi ép tranh chấp để kiểm tra unique constraint độc lập với tranh chấp tạo fence lần đầu.

Các timeout và xử lý lỗi của runtime giữ nguyên. Interceptor chỉ đăng ký trong host của ca kiểm thử unique conflict.

## Kiểm chứng

Database `scdc_ci_dm_test` được tạo riêng trong PostgreSQL kiểm thử tại cổng 15459, dùng schema và ba role hiện hành; Redis tại cổng 16399, prefix `scdc:auth:ci-dm:`. Stack và dữ liệu dev giữ nguyên. Runner đặt `DOTNET_PROCESSOR_COUNT=2` và dùng cấu hình song song mặc định của xUnit như workflow CI.

| Kiểm tra | Kết quả |
|---|---|
| Tái hiện barrier cũ với request đến muộn | Lỗi cùng điều kiện chờ đủ 40 request |
| Nhóm DirectConversation sau sửa | 14/14 đạt, gồm hai ca 40 request và một ca unique conflict thật |
| Backend toàn solution | 381/381 đạt, không bỏ qua; 3 phút 8 giây; cấu hình song song mặc định, 2 CPU |
| Build Release | Đạt trong lượt kiểm thử toàn solution |
| Liên kết tài liệu | 112 trang Markdown và 2735 liên kết local, 0 lỗi |

Artifact local tại `artifacts/ci-dm-concurrency/`: `baseline-arrival-skew.log`, `baseline-arrival-skew.trx`, `direct-conversations.log`, `direct-conversations.trx`, `backend.log`, `results/ci-dm.trx` và `test.sh`. Artifact không commit.

Các lượt kiểm tra đầu gặp lỗi khởi tạo phiên của fixture; một lượt đầu của ca unique conflict gặp tranh chấp tạo fence lần đầu. Kết luận lấy lượt đã khởi tạo fence: 14/14 DirectConversation và 381/381 toàn bộ backend đạt.

Nguồn: [hồ sơ S2-08](../architecture/s2-08-2026-11-24.md), [hồ sơ S2-10](../architecture/s2-10-2026-11-24.md), [workflow CI](../../../../.github/workflows/ci.yml).
