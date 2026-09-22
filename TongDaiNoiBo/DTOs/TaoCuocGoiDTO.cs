namespace TongDaiNoiBo.DTOs
{
    public class TaoCuocGoiDTO
    {
        // Mã tài khoản người nhận
        public int MaNguoiNhan { get; set; }

        // Thời điểm tạo yêu cầu ký
        public long ThoiGianTicks { get; set; }

        // Chuỗi ngẫu nhiên chống gửi lại yêu cầu cũ
        public string Nonce { get; set; } = string.Empty;

        // Chữ ký số được tạo ở máy của người gọi
        // bằng Private Key RSA của chính người gọi
        public string ChuKySo { get; set; } = string.Empty;
    }
}