# SCDC — Bàn giao DM

Gói MVP-DM tại [hồ sơ MVP](../../../releases/mvp.md#packages); phần hoàn thiện tại [v1](../../../releases/v1.md). Chọn AC/TC tại [đặc tả](../specs/acceptance.md), rà soát API DM cùng [Messaging dùng chung](../../../shared/messaging/README.md) và quyền tin phòng.

Chưa có hồ sơ nghiệm thu riêng của gói DM trong thư mục này. Khi khóa một gói, ghi `<gói>/plan.md`; kết quả gắn commit vào `<gói>/acceptance.md` theo [mẫu](../../../templates/release-record.md), rồi dẫn từ [status](../status.md).


Gói từng task hiện tại: [kế hoạch DM](../../../plans/direct-messaging.md), [P0 baseline được duyệt](../../../acceptance/direct-messaging/DM-P0-T01.md) và [P1 tìm người được duyệt FE/BE](../../../acceptance/direct-messaging/DM-P1-T01.md). Đây là hồ sơ acceptance task; chưa có release record nghiệm thu toàn MVP-DM.

P1-T01 đã được người dùng xác nhận PASS FE/BE build `9d995d0`; merge/push vào `message` tại `d18d9a4`. [Biên bản P1](../../../acceptance/direct-messaging/DM-P1-T01.md) giữ build, xác nhận, ancestry và smoke. Tạo/lấy hội thoại P1-T02 đã được người dùng PASS FE/BE build ea0c311 và merge message tại e5170b4, [biên bản](../../../acceptance/direct-messaging/DM-P1-T02.md); đây chưa là nghiệm thu toàn UC DM.

[Inbox P1-T03](../../../acceptance/direct-messaging/DM-P1-T03.md): reload tải dữ liệu thật, phân trang và tách tài khoản; đã push nhánh task, **Chờ người dùng test** trước merge vào message. Mục người vừa nhắn tin có loader, proof với tin thật chờ P2-T01.
