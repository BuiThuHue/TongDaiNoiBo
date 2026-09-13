using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TongDaiNoiBo.Data;

namespace TongDaiNoiBo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TaiKhoanController : ControllerBase
    {
        private readonly TongDaiDbContext _context;

        public TaiKhoanController(TongDaiDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // 1. LẤY DANH SÁCH TÀI KHOẢN
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> LayDanhSachTaiKhoan()
        {
            try
            {
                var danhSach = await _context.TaiKhoan
                    .Select(x => new
                    {
                        x.MaTaiKhoan,
                        x.TenDangNhap,
                        x.HoTen,
                        x.Email,
                        x.VaiTro,
                        x.TrangThai,
                        x.SoLanDangNhapSai,
                        x.NgayTao
                    })
                    .ToListAsync();

                return Ok(danhSach);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    ThongBao = "Lỗi khi đọc danh sách tài khoản!",
                    ChiTiet = ex.Message
                });
            }
        }

        // =========================================================
        // 2. KIỂM TRA API BẢO MẬT
        // =========================================================
        [Authorize]
        [HttpGet("BaoMat")]
        public IActionResult KiemTraBaoMat()
        {
            return Ok(new
            {
                ThongBao =
                    "Bạn đã đăng nhập và được phép truy cập API bảo mật!"
            });
        }

        // =========================================================
        // 3. CHỈ ADMIN ĐƯỢC TRUY CẬP
        // =========================================================
        [Authorize(Roles = "Admin")]
        [HttpGet("ChiAdmin")]
        public IActionResult ChiAdmin()
        {
            return Ok(new
            {
                ThongBao =
                    "Bạn là Admin nên được phép truy cập chức năng này!"
            });
        }

        // =========================================================
        // 4. LẤY THÔNG TIN MỘT TÀI KHOẢN
        // =========================================================
        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> LayTaiKhoan(int id)
        {
            try
            {
                var taiKhoan = await _context.TaiKhoan
                    .Where(x => x.MaTaiKhoan == id)
                    .Select(x => new
                    {
                        x.MaTaiKhoan,
                        x.TenDangNhap,
                        x.HoTen,
                        x.Email,
                        x.VaiTro,
                        x.TrangThai,
                        x.SoLanDangNhapSai,
                        x.NgayTao
                    })
                    .FirstOrDefaultAsync();

                if (taiKhoan == null)
                {
                    return NotFound(new
                    {
                        ThongBao = "Không tìm thấy tài khoản!"
                    });
                }

                return Ok(taiKhoan);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    ThongBao = "Lỗi khi lấy thông tin tài khoản!",
                    ChiTiet = ex.Message
                });
            }
        }

        // =========================================================
        // 5. KIỂM TRA TÀI KHOẢN HIỆN TẠI
        // =========================================================
        [Authorize]
        [HttpGet("TaiKhoanHienTai")]
        public async Task<IActionResult> LayTaiKhoanHienTai()
        {
            try
            {
                var maTaiKhoanClaim =
                    User.FindFirst(
                        System.Security.Claims.ClaimTypes.NameIdentifier
                    )?.Value;

                if (string.IsNullOrEmpty(maTaiKhoanClaim))
                {
                    return Unauthorized(new
                    {
                        ThongBao = "Không xác định được tài khoản đăng nhập!"
                    });
                }

                int maTaiKhoan = int.Parse(maTaiKhoanClaim);

                var taiKhoan = await _context.TaiKhoan
                    .Where(x => x.MaTaiKhoan == maTaiKhoan)
                    .Select(x => new
                    {
                        x.MaTaiKhoan,
                        x.TenDangNhap,
                        x.HoTen,
                        x.Email,
                        x.VaiTro,
                        x.TrangThai,
                        x.NgayTao
                    })
                    .FirstOrDefaultAsync();

                if (taiKhoan == null)
                {
                    return NotFound(new
                    {
                        ThongBao = "Không tìm thấy tài khoản!"
                    });
                }

                return Ok(taiKhoan);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    ThongBao = "Lỗi khi lấy tài khoản hiện tại!",
                    ChiTiet = ex.Message
                });
            }
        }
    }
}