using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TongDaiNoiBo.Models
{
    [Table("CHU_KY_SO")]
    public class ChuKySo
    {
        [Key]
        public int MaChuKy { get; set; }

        public int MaBanGhi { get; set; }

        [Required]
        [StringLength(64)]
        public string HamBamSHA256 { get; set; } = string.Empty;

        [Required]
        [Column("ChuKySo")]
        public string ChuKy { get; set; } = string.Empty;

        public DateTime ThoiGianKy { get; set; }

        [Required]
        [StringLength(20)]
        public string TrangThai { get; set; } = string.Empty;

        [ForeignKey("MaBanGhi")]
        public BanGhiCuocGoi? BanGhiCuocGoi { get; set; }
    }
}