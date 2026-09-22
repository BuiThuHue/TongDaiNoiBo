namespace TongDaiNoiBo.DTOs
{
    public class ThietLapChuKySoDTO
    {
        // Public Key được tạo ở phía máy nhân viên.
        // Chỉ Public Key được gửi lên Server.
        public string PublicKeyRSA { get; set; } = string.Empty;
    }
}