using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TongDaiNoiBo.Models
{
    [Table("SO_MAY_NOI_BO")]
    public class SoMayNoiBo
    {
        [Key]
        public int MaSoMay { get; set; }

        [Required]
        [StringLength(10)]
        public string SoMay { get; set; }

        public int? MaTaiKhoan { get; set; }

        [Required]
        [StringLength(20)]
        public string TrangThai { get; set; }

        // Liên kết với tài khoản được cấp số máy
        [ForeignKey("MaTaiKhoan")]
        public TaiKhoan? TaiKhoan { get; set; }
    }
}