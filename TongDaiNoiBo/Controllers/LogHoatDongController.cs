using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TongDaiNoiBo.Data;

namespace TongDaiNoiBo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LogHoatDongController : ControllerBase
    {
        private readonly TongDaiDbContext _context;

        public LogHoatDongController(TongDaiDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> LayDanhSachLog()
        {
            try
            {
                var danhSachLog = await _context.LogHoatDong
                    .Include(x => x.TaiKhoan)
                    .OrderByDescending(x => x.ThoiGian)
                    .Select(x => new
                    {
                        x.MaLog,
                        x.MaTaiKhoan,
                        TenDangNhap = x.TaiKhoan != null
                            ? x.TaiKhoan.TenDangNhap
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