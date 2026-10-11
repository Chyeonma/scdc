# SCDC — Tài khoản

Phạm vi REQ-010/SCP-002. Identity sở hữu danh tính, hồ sơ, mật khẩu, email và phiên. [MVP](../../releases/mvp.md) chọn các luồng nền; [v1](../../releases/v1.md) hoàn thiện vòng đời tài khoản.

## Mục lục

| Chủ đề | Nội dung và mã truy vết |
|---|---|
| [Đăng ký và xác minh](registration-verification.md) | UC-ACC-01–03; ACC-001/008/009; form, API, transaction, AC và gaps |
| [Đăng nhập và phiên](sessions.md) | UC-ACC-04/09–11; ACC-002/010/011; rotation, logout/revoke, HTTP và realtime |
| [Khôi phục và đổi mật khẩu](password-recovery.md) | UC-ACC-05/06/08; ACC-006/014/016; reset pending và mật khẩu trùng |
| [Hồ sơ](profile.md) | UC-ACC-07; ACC-003/004/013; dữ liệu riêng/công khai và validation |
| [Trạng thái/khóa tài khoản](account-state.md) | UC-ACC-12; ACC-005/007/015; ACL-01/02 và công cụ vận hành |
| [Liên kết và giao email](email-links.md) | ACC-012; cơ chế thực sự dùng chung verify/reset, worker và AC/TC liên kết |

## Phạm vi đọc và bàn giao

Mỗi chủ đề giữ quy tắc, luồng/UX, API/dữ liệu/transaction, AC/TC và hiện trạng tương ứng. Ma trận source/test được đối chiếu ngày 2026-10-04/05; việc tổ chức lại tài liệu không ghi nhận một lần chạy mới. Kết quả chạy gắn commit/build/môi trường vào hồ sơ kiểm chứng của gói; danh mục test hoặc fixture không tự ghi thành đạt.

Gói MVP-ID và người phụ trách nằm trong [hồ sơ MVP](../../releases/mvp.md#packages); phần hoàn thiện ở [v1](../../releases/v1.md). Chưa có hồ sơ nghiệm thu riêng cho gói Accounts. MVP dùng một API host; shared transaction/guard được rà soát khi chuyển microservice v1 theo DEC-116.

<a id="use-case-rules"></a>

## Truy vết quy tắc

| Quy tắc | Use case thực hiện | Nguồn quy tắc |
|---|---|---|
| ACC-001 | UC-ACC-01 | [Tài liệu chủ đề](registration-verification.md#rules) |
| ACC-002 | UC-ACC-04 | [Tài liệu chủ đề](sessions.md#rules) |
| ACC-003 | UC-ACC-07 | [Tài liệu chủ đề](profile.md#rules) |
| ACC-004 | UC-ACC-01/07/10/12 | [Tài liệu chủ đề](profile.md#rules) |
| ACC-005 | UC-ACC-02/04 | [Tài liệu chủ đề](account-state.md#rules) |
| ACC-006 | UC-ACC-05/06 | [Tài liệu chủ đề](password-recovery.md#rules) |
| ACC-007 | UC-ACC-01/02/04/05/06 | [Tài liệu chủ đề](account-state.md#rules) |
| ACC-008 | UC-ACC-01/07 | [Tài liệu chủ đề](registration-verification.md#rules) |
| ACC-009 | UC-ACC-01/04/05 | [Tài liệu chủ đề](registration-verification.md#rules) |
| ACC-010 | UC-ACC-01/04/06/08 | [Tài liệu chủ đề](sessions.md#rules) |
| ACC-011 | UC-ACC-04/06/08/09/10/11 | [Tài liệu chủ đề](sessions.md#rules) |
| ACC-012 | UC-ACC-01/02/03/05/06 | [Tài liệu chủ đề](email-links.md#rules) |
| ACC-013 | UC-ACC-07 | [Tài liệu chủ đề](profile.md#rules) |
| ACC-014 | UC-ACC-05/06 | [Tài liệu chủ đề](password-recovery.md#rules) |
| ACC-015 | UC-ACC-12 | [Tài liệu chủ đề](account-state.md#rules) |
| ACC-016 | UC-ACC-06/08 | [Tài liệu chủ đề](password-recovery.md#rules) |
