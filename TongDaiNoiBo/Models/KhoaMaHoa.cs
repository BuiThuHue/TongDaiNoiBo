using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TongDaiNoiBo.Models
{
    [Table("KHOA_MA_HOA")]
    public class KhoaMaHoa
    {
        [Key]
        public int MaKhoa { get; set; }

        public int MaBanGhi { get; set; }

        [Required]
        public string KhoaAESDaBaoVe { get; set; }

        [Required]
        [StringLength(50)]
        public string PhuongThucMaHoa { get; set; }

        public DateTime ThoiGianTao { get; set; }

        // Quan hệ với bản ghi cuộc gọi
        [ForeignKey("MaBanGhi")]
        public BanGhiCuocGoi? BanGhiCuocGoi { get; set; }
    }
}