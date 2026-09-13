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
        public string TenDangNhap { get; set; }

        [Required]
        [StringLength(255)]
        public string MatKhau { get; set; }

        [Required]
        [StringLength(100)]
        public string HoTen { get; set; }

        [StringLength(100)]
        public string? Email { get; set; }

        [Required]
        [StringLength(20)]
        public string VaiTro { get; set; }

        [Required]
        [StringLength(20)]
        public string TrangThai { get; set; }

        public int SoLanDangNhapSai { get; set; }

        public DateTime NgayTao { get; set; }

        // Khóa công khai RSA dùng để xác thực chữ ký
        [Required]
        public string PublicKeyRSA { get; set; }
    }
}