namespace TongDaiNoiBo.DTOs
{
    public class NhanCuocGoiDTO
    {
        // Thời điểm B thực hiện ký số
        public long ThoiGianTicks { get; set; }

        // Nonce do trình duyệt B tạo để chống Replay Attack
        public string Nonce { get; set; } = string.Empty;

        // Chữ ký RSA do trình duyệt B tạo bằng Private Key của B
        public string ChuKySo { get; set; } = string.Empty;
    }
}