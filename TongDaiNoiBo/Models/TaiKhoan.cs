using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TongDaiNoiBo.Models
{
    [Table("TAI_KHOAN")]
    public class TaiKhoan
    {
        [Key]
        public int MaTaiKhoan { get; set; }

        [Required]
        [StringLength(50)]
        public string TenDangNhap { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string MatKhau { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string HoTen { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Email { get; set; }

        [Required]
        [StringLength(20)]
        public string VaiTro { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string TrangThai { get; set; } = string.Empty;

        public int SoLanDangNhapSai { get; set; }

        public DateTime NgayTao { get; set; }

        // =====================================================
        // PUBLIC KEY RSA
        // =====================================================
        //
        // Khi Admin vừa tạo tài khoản:
        //      PublicKeyRSA = null
        //
        // Khi nhân viên đăng nhập lần đầu và thiết lập chữ ký số:
        //      Trình duyệt tạo RSA
        //      Public Key  -> gửi lên Server
        //      Private Key -> mã hóa bằng PIN và giữ trên thiết bị
        //
        // Admin KHÔNG tạo và KHÔNG biết Private Key của nhân viên.
        // =====================================================

        public string? PublicKeyRSA { get; set; }
    }
}