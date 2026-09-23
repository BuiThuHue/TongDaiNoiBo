using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TongDaiNoiBo.Data;

namespace TongDaiNoiBo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class LogHoatDongController : ControllerBase
    {
        private readonly TongDaiDbContext _context;

        public LogHoatDongController(TongDaiDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // ADMIN - LẤY TOÀN BỘ NHẬT KÝ HOẠT ĐỘNG
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> LayDanhSachLog()
        {
            try
            {
                var danhSachLog = await _context.LogHoatDong
                    .AsNoTracking()
                    .Include(x => x.TaiKhoan)
                    .OrderByDescending(x => x.ThoiGian)
                    .Select(x => new
                    {
                        x.MaLog,

                        x.MaTaiKhoan,

                        TenDangNhap = x.TaiKhoan != null
                            ? x.TaiKhoan.TenDangNhap
                            : null,

                        HoTen = x.TaiKhoan != null
                            ? x.TaiKhoan.HoTen
                            : null,

                        x.HanhDong,

                        x.ThoiGian,

                        x.DiaChiIP,

                        x.NoiDung,

                        x.TrangThai
                    })
                    .ToListAsync();

                return Ok(danhSachLog);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    ThongBao = "Lỗi khi lấy nhật ký hoạt động!",
                    ChiTiet = ex.Message
                });
            }
        }
    }
}