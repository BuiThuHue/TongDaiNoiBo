using Microsoft.EntityFrameworkCore;
using TongDaiNoiBo.Models;

namespace TongDaiNoiBo.Data
{
    public class TongDaiDbContext : DbContext
    {
        public TongDaiDbContext(DbContextOptions<TongDaiDbContext> options)
            : base(options)
        {
        }

        public DbSet<TaiKhoan> TaiKhoan { get; set; }

        public DbSet<SoMayNoiBo> SoMayNoiBo { get; set; }

        public DbSet<CuocGoi> CuocGoi { get; set; }

        public DbSet<BanGhiCuocGoi> BanGhiCuocGoi { get; set; }

        public DbSet<KhoaMaHoa> KhoaMaHoa { get; set; }

        public DbSet<ChuKySo> ChuKySo { get; set; }

        public DbSet<LogHoatDong> LogHoatDong { get; set; }
    }
}