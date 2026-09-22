using System.ComponentModel.DataAnnotations;

namespace TongDaiNoiBo.DTOs
{
    public class TaoTaiKhoanDTO
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập!")]
        [StringLength(50)]
        public string TenDangNhap { get; set; } = string.Empty;


        [Required(ErrorMessage = "Vui lòng nhập mật khẩu!")]
        [StringLength(100, MinimumLength = 6)]
        public string MatKhau { get; set; } = string.Empty;


        [Required(ErrorMessage = "Vui lòng nhập họ tên!")]
        [StringLength(100)]
        public string HoTen { get; set; } = string.Empty;


        [StringLength(100)]
        public string? Email { get; set; }


        [Required(ErrorMessage = "Vui lòng nhập số máy!")]
        [StringLength(10)]
        public string SoMay { get; set; } = string.Empty;
    }
}