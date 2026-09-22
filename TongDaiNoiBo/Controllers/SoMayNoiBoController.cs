using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TongDaiNoiBo.Data;
using TongDaiNoiBo.Models;

namespace TongDaiNoiBo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SoMayNoiBoController : ControllerBase
    {
        private readonly TongDaiDbContext _context;

        public SoMayNoiBoController(
            TongDaiDbContext context)
        {
            _context = context;
        }


        // =========================================================
        // 1. LẤY DANH SÁCH SỐ MÁY
        // GET: /api/SoMayNoiBo
        // =========================================================

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> LayDanhSachSoMay()
        {
            try
            {
                var danhSach =
                    await _context.SoMayNoiBo
                        .Include(x => x.TaiKhoan)
                        .OrderBy(x => x.MaSoMay)
                        .Select(x => new
                        {
                            x.MaSoMay,

                            x.SoMay,

                            x.MaTaiKhoan,

                            TenDangNhap =
                                x.TaiKhoan != null
                                    ? x.TaiKhoan.TenDangNhap
                                    : null,

                            HoTen =
                                x.TaiKhoan != null
                                    ? x.TaiKhoan.HoTen
                                    : null,

                            x.TrangThai
                        })
                        .ToListAsync();

                return Ok(danhSach);
            }
            catch (Exception)
            {
                return StatusCode(
                    500,
                    new
                    {
                        ThongBao =
                            "Lỗi khi lấy danh sách số máy!"
                    }
                );
            }
        }


        // =========================================================
        // 2. XEM CHI TIẾT SỐ MÁY
        // GET: /api/SoMayNoiBo/1
        // =========================================================

        [Authorize]
        [HttpGet("{maSoMay}")]
        public async Task<IActionResult> LayChiTietSoMay(
            int maSoMay)
        {
            try
            {
                if (maSoMay <= 0)
                {
                    return BadRequest(
                        new
                        {
                            ThongBao =
                                "Mã số máy không hợp lệ!"
                        }
                    );
                }

                var soMay =
                    await _context.SoMayNoiBo
                        .Include(x => x.TaiKhoan)
                        .FirstOrDefaultAsync(
                            x =>
                                x.MaSoMay == maSoMay
                        );

                if (soMay == null)
                {
                    return NotFound(
                        new
                        {
                            ThongBao =
                                "Số máy không tồn tại!"
                        }
                    );
                }

                return Ok(
                    new
                    {
                        soMay.MaSoMay,

                        soMay.SoMay,

                        soMay.MaTaiKhoan,

                        TenDangNhap =
                            soMay.TaiKhoan != null
                                ? soMay.TaiKhoan.TenDangNhap
                                : null,

                        HoTen =
                            soMay.TaiKhoan != null
                                ? soMay.TaiKhoan.HoTen
                                : null,

                        Email =
                            soMay.TaiKhoan != null
                                ? soMay.TaiKhoan.Email
                                : null,

                        soMay.TrangThai
                    }
                );
            }
            catch (Exception)
            {
                return StatusCode(
                    500,
                    new
                    {
                        ThongBao =
                            "Lỗi khi lấy thông tin số máy!"
                    }
                );
            }
        }


        // =========================================================
        // 3. TÌM SỐ MÁY
        // GET: /api/SoMayNoiBo/TimKiem/101
        // =========================================================

        [Authorize]
        [HttpGet("TimKiem/{soMay}")]
        public async Task<IActionResult> TimKiemSoMay(
            string soMay)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(soMay))
                {
                    return BadRequest(
                        new
                        {
                            ThongBao =
                                "Vui lòng nhập số máy!"
                        }
                    );
                }

                var ketQua =
                    await _context.SoMayNoiBo
                        .Include(x => x.TaiKhoan)
                        .Where(
                            x =>
                                x.SoMay == soMay
                        )
                        .Select(x => new
                        {
                            x.MaSoMay,

                            x.SoMay,

                            x.MaTaiKhoan,

                            TenDangNhap =
                                x.TaiKhoan != null
                                    ? x.TaiKhoan.TenDangNhap
                                    : null,

                            HoTen =
                                x.TaiKhoan != null
                                    ? x.TaiKhoan.HoTen
                                    : null,

                            x.TrangThai
                        })
                        .FirstOrDefaultAsync();

                if (ketQua == null)
                {
                    return NotFound(
                        new
                        {
                            ThongBao =
                                "Không tìm thấy số máy!"
                        }
                    );
                }

                return Ok(ketQua);
            }
            catch (Exception)
            {
                return StatusCode(
                    500,
                    new
                    {
                        ThongBao =
                            "Lỗi khi tìm kiếm số máy!"
                    }
                );
            }
        }
    }
}