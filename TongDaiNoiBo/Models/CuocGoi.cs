using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TongDaiNoiBo.Models
{
    [Table("CUOC_GOI")]
    public class CuocGoi
    {
        [Key]
        public int MaCuocGoi { get; set; }

        public int MaNguoiGoi { get; set; }

        public int MaNguoiNhan { get; set; }

        public DateTime ThoiGianBatDau { get; set; }

        public DateTime? ThoiGianKetThuc { get; set; }

        // Thời lượng tính bằng giây
        public int? ThoiLuong { get; set; }

        [Required]
        [StringLength(30)]
        public string TrangThai { get; set; }

        // Dữ liệu chống phát lại (Replay Attack)
        [Required]
        [StringLength(100)]
        public string Nonce { get; set; }

        // Dữ liệu được sử dụng để xác thực cuộc gọi
        [Required]
        public string DuLieuXacThuc { get; set; }

        // Chữ ký số của người gọi
        [Required]
        public string ChuKyNguoiGoi { get; set; }

        // Chữ ký số của người nhận
        public string? ChuKyNguoiNhan { get; set; }

        [Required]
        [StringLength(30)]
        public string TrangThaiXacThuc { get; set; }

        // Quan hệ với tài khoản người gọi
        [ForeignKey("MaNguoiGoi")]
        public TaiKhoan? NguoiGoi { get; set; }

        // Quan hệ với tài khoản người nhận
        [ForeignKey("MaNguoiNhan")]
        public TaiKhoan? NguoiNhan { get; set; }

        // Quan hệ với bản ghi cuộc gọi
        public BanGhiCuocGoi? BanGhiCuocGoi { get; set; }
    }
}