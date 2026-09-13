using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TongDaiNoiBo.Models
{
    [Table("LOG_HOAT_DONG")]
    public class LogHoatDong
    {
        [Key]
        public int MaLog { get; set; }

        public int? MaTaiKhoan { get; set; }

        [Required]
        [StringLength(100)]
        public string HanhDong { get; set; }

        public DateTime ThoiGian { get; set; }

        [StringLength(45)]
        public string? DiaChiIP { get; set; }

        public string? NoiDung { get; set; }

        [Required]
        [StringLength(20)]
        public string TrangThai { get; set; }

        // Quan hệ với tài khoản
        [ForeignKey("MaTaiKhoan")]
        public TaiKhoan? TaiKhoan { get; set; }
    }
}