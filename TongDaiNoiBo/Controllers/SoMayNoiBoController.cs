using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TongDaiNoiBo.Data;

namespace TongDaiNoiBo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SoMayNoiBoController : ControllerBase
    {
        private readonly TongDaiDbContext _context;

        public SoMayNoiBoController(TongDaiDbContext context)
        {
            _context = context;
        }


        // ==========================================
        // 1. LẤY DANH SÁCH TẤT CẢ SỐ MÁY
        // ==========================================

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> LayDanhSachSoMay()
        {
            try
            {
                var danhSach = await _context.SoMayNoiBo
                    .Include(x => x.TaiKhoan)
                    .OrderBy(x => x.SoMay)
                    .Select(x => new
                    {
                        x.MaSoMay,
                        x.SoMay,
                        x.TrangThai,

                        MaTaiKhoan = x.MaTaiKhoan,

                        TenDangNhap =
                            x.TaiKhoan != null
                                ? x.TaiKhoan.TenDangNhap
                                : null,

                        HoTen =
                            x.TaiKhoan != null
                                ? x.TaiKhoan.HoTen
                                : null
                    })
                    .ToListAsync();

                return Ok(danhSach);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    ThongBao =
                        "Lỗi khi lấy danh sách số máy!",

                    ChiTiet =
                        ex.Message
                });
            }
        }


        // ==========================================
        // 2. LẤY CÁC SỐ MÁY CHƯA CẤP
        // ==========================================

        [Authorize(Roles = "Admin")]
        [HttpGet("ChuaCap")]
        public async Task<IActionResult> LaySoMayChuaCap()
        {
            try
            {
                var danhSach =
                    await _context.SoMayNoiBo
                        .Where(x => x.MaTaiKhoan == null)
                        .OrderBy(x => x.SoMay)
                        .Select(x => new
                        {
                            x.MaSoMay,
                            x.SoMay,
                            x.TrangThai
                        })
                        .ToListAsync();

                return Ok(danhSach);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    ThongBao =
                        "Lỗi khi lấy danh sách số máy chưa cấp!",

                    ChiTiet =
                        ex.Message
                });
            }
        }


        // ==========================================
        // 3. LẤY SỐ MÁY CỦA MỘT TÀI KHOẢN
        // ==========================================

        [Authorize]
        [HttpGet("CuaTaiKhoan/{maTaiKhoan}")]
        public async Task<IActionResult> LaySoMayCuaTaiKhoan(
            int maTaiKhoan)
        {
            try
            {
                var soMay =
                    await _context.SoMayNoiBo
                        .Include(x => x.TaiKhoan)
                        .Where(x =>
                            x.MaTaiKhoan == maTaiKhoan)
                        .Select(x => new
                        {
                            x.MaSoMay,
                            x.SoMay,
                            x.TrangThai,

                            MaTaiKhoan =
                                x.MaTaiKhoan,

                            TenDangNhap =
                                x.TaiKhoan != null
                                    ? x.TaiKhoan.TenDangNhap
                                    : null,

                            HoTen =
                                x.TaiKhoan != null
                                    ? x.TaiKhoan.HoTen
                                    : null
                        })
                        .FirstOrDefaultAsync();

                if (soMay == null)
                {
                    return NotFound(new
                    {
                        ThongBao =
                            "Tài khoản này chưa được cấp số máy!"
                    });
                }

                return Ok(soMay);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    ThongBao =
                        "Lỗi khi lấy số máy của tài khoản!",

                    ChiTiet =
                        ex.Message
                });
            }
        }


        // ==========================================
        // 4. KIỂM TRA MỘT SỐ MÁY
        // ==========================================

        [Authorize]
        [HttpGet("KiemTra/{soMay}")]
        public async Task<IActionResult> KiemTraSoMay(
            string soMay)
        {
            try
            {
                var ketQua =
                    await _context.SoMayNoiBo
                        .Include(x => x.TaiKhoan)
                        .Where(x => x.SoMay == soMay)
                        .Select(x => new
                        {
                            x.MaSoMay,
                            x.SoMay,
                            x.TrangThai,

                            MaTaiKhoan =
                                x.MaTaiKhoan,

                            TenDangNhap =
                                x.TaiKhoan != null
                                    ? x.TaiKhoan.TenDangNhap
                                    : null,

                            HoTen =
                                x.TaiKhoan != null
                                    ? x.TaiKhoan.HoTen
                                    : null
                        })
                        .FirstOrDefaultAsync();

                if (ketQua == null)
                {
                    return NotFound(new
                    {
                        ThongBao =
                            "Số máy nội bộ không tồn tại!"
                    });
                }

                return Ok(ketQua);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    ThongBao =
                        "Lỗi khi kiểm tra số máy!",

                    ChiTiet =
                        ex.Message
                });
            }
        }
    }
}