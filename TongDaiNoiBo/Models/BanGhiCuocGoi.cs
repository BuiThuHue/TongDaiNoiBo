using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TongDaiNoiBo.Models
{
    [Table("BAN_GHI_CUOC_GOI")]
    public class BanGhiCuocGoi
    {
        [Key]
        public int MaBanGhi { get; set; }

        public int MaCuocGoi { get; set; }

        [Required]
        [StringLength(255)]
        public string TenFile { get; set; }

        [Required]
        [StringLength(500)]
        public string DuongDanFile { get; set; }

        public long? KichThuocFile { get; set; }

        // Thời lượng bản ghi tính bằng giây
        public int? ThoiLuong { get; set; }

        public DateTime ThoiGianTao { get; set; }

        [Required]
        [StringLength(20)]
        public string TrangThai { get; set; }

        // Quan hệ với cuộc gọi
        [ForeignKey("MaCuocGoi")]
        public CuocGoi? CuocGoi { get; set; }

        // Quan hệ với khóa mã hóa
        public KhoaMaHoa? KhoaMaHoa { get; set; }

        // Quan hệ với chữ ký số
        public ChuKySo? ChuKySo { get; set; }
    }
}