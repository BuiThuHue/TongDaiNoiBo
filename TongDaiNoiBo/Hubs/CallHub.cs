using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace TongDaiNoiBo.Hubs
{
    [Authorize(Roles = "NhanVien")]
    public class CallHub : Hub
    {
        // =====================================================
        // LẤY MÃ TÀI KHOẢN TỪ JWT
        // =====================================================
        private int LayMaTaiKhoan()
        {
            string? maTaiKhoanClaim =
                Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(maTaiKhoanClaim) ||
                !int.TryParse(maTaiKhoanClaim, out int maTaiKhoan))
            {
                throw new HubException(
                    "Không xác định được tài khoản đang đăng nhập."
                );
            }

            return maTaiKhoan;
        }


        // =====================================================
        // KHI CLIENT KẾT NỐI SIGNALR
        // =====================================================
        public override async Task OnConnectedAsync()
        {
            int maTaiKhoan = LayMaTaiKhoan();

            // Mỗi tài khoản có một Group riêng.
            // Ví dụ:
            // TaiKhoan_2
            // TaiKhoan_3
            string tenNhom = $"TaiKhoan_{maTaiKhoan}";

            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                tenNhom
            );

            await base.OnConnectedAsync();
        }


        // =====================================================
        // GỬI WEBRTC OFFER
        // A -> B
        // =====================================================
        public async Task GuiOffer(
            int maNguoiNhan,
            string offer
        )
        {
            int maNguoiGui = LayMaTaiKhoan();

            if (maNguoiNhan <= 0)
            {
                throw new HubException(
                    "Người nhận không hợp lệ."
                );
            }

            if (string.IsNullOrWhiteSpace(offer))
            {
                throw new HubException(
                    "WebRTC Offer không hợp lệ."
                );
            }

            await Clients
                .Group($"TaiKhoan_{maNguoiNhan}")
                .SendAsync(
                    "NhanOffer",
                    maNguoiGui,
                    offer
                );
        }


        // =====================================================
        // GỬI WEBRTC ANSWER
        // B -> A
        // =====================================================
        public async Task GuiAnswer(
            int maNguoiNhan,
            string answer
        )
        {
            int maNguoiGui = LayMaTaiKhoan();

            if (maNguoiNhan <= 0)
            {
                throw new HubException(
                    "Người nhận không hợp lệ."
                );
            }

            if (string.IsNullOrWhiteSpace(answer))
            {
                throw new HubException(
                    "WebRTC Answer không hợp lệ."
                );
            }

            await Clients
                .Group($"TaiKhoan_{maNguoiNhan}")
                .SendAsync(
                    "NhanAnswer",
                    maNguoiGui,
                    answer
                );
        }


        // =====================================================
        // GỬI ICE CANDIDATE
        // Dùng cho cả A -> B và B -> A
        // =====================================================
        public async Task GuiIceCandidate(
            int maNguoiNhan,
            string candidate
        )
        {
            int maNguoiGui = LayMaTaiKhoan();

            if (maNguoiNhan <= 0)
            {
                throw new HubException(
                    "Người nhận không hợp lệ."
                );
            }

            if (string.IsNullOrWhiteSpace(candidate))
            {
                return;
            }

            await Clients
                .Group($"TaiKhoan_{maNguoiNhan}")
                .SendAsync(
                    "NhanIceCandidate",
                    maNguoiGui,
                    candidate
                );
        }


        // =====================================================
        // THÔNG BÁO KẾT THÚC WEBRTC
        // =====================================================
        public async Task ThongBaoKetThuc(
            int maNguoiNhan,
            int maCuocGoi
        )
        {
            int maNguoiGui = LayMaTaiKhoan();

            if (maNguoiNhan <= 0 || maCuocGoi <= 0)
            {
                throw new HubException(
                    "Thông tin cuộc gọi không hợp lệ."
                );
            }

            await Clients
                .Group($"TaiKhoan_{maNguoiNhan}")
                .SendAsync(
                    "CuocGoiDaKetThuc",
                    maNguoiGui,
                    maCuocGoi
                );
        }
    }
}