using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TongDaiNoiBo.Data;
using TongDaiNoiBo.DTOs;
using TongDaiNoiBo.Models;

namespace TongDaiNoiBo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DangNhapController : ControllerBase
    {
        private readonly TongDaiDbContext _context;

        public DangNhapController(TongDaiDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // ĐĂNG NHẬP
        // POST: /api/DangNhap
        // =========================================================

        [HttpPost]
        public async Task<IActionResult> DangNhap(
            [FromBody] DangNhapDTO dto)
        {
            try
            {
                // =====================================================
                // 1. KIỂM TRA DỮ LIỆU
                // =====================================================

                if (dto == null)
                {
                    return BadRequest(new
                    {
                        ThongBao = "Dữ liệu đăng nhập không hợp lệ!"
                    });
                }

                if (string.IsNullOrWhiteSpace(dto.TenDangNhap))
                {
                    return BadRequest(new
                    {
                        ThongBao = "Vui lòng nhập tên đăng nhập!"
                    });
                }

                if (string.IsNullOrWhiteSpace(dto.MatKhau))
                {
                    return BadRequest(new
                    {
                        ThongBao = "Vui lòng nhập mật khẩu!"
                    });
                }

                // =====================================================
                // 2. TÌM TÀI KHOẢN
                // =====================================================

                var taiKhoan =
                    await _context.TaiKhoan
                        .FirstOrDefaultAsync(
                            x => x.TenDangNhap == dto.TenDangNhap
                        );

                if (taiKhoan == null)
                {
                    return Unauthorized(new
                    {
                        ThongBao =
                            "Tên đăng nhập hoặc mật khẩu không đúng!"
                    });
                }

                // =====================================================
                // 3. KIỂM TRA TRẠNG THÁI
                // =====================================================

                if (taiKhoan.TrangThai != "HoatDong")
                {
                    await GhiLog(
                        taiKhoan.MaTaiKhoan,
                        "DangNhapThatBai",
                        "Tài khoản đã bị khóa",
                        "ThatBai"
                    );

                    return Unauthorized(new
                    {
                        ThongBao = "Tài khoản đã bị khóa!"
                    });
                }

                // =====================================================
                // 4. KIỂM TRA MẬT KHẨU BCRYPT
                // =====================================================

                bool matKhauDung;

                try
                {
                    matKhauDung =
                        BCrypt.Net.BCrypt.Verify(
                            dto.MatKhau,
                            taiKhoan.MatKhau
                        );
                }
                catch
                {
                    matKhauDung = false;
                }

                // =====================================================
                // 5. MẬT KHẨU SAI
                // =====================================================

                if (!matKhauDung)
                {
                    taiKhoan.SoLanDangNhapSai++;

                    if (taiKhoan.SoLanDangNhapSai >= 3)
                    {
                        taiKhoan.TrangThai = "Khoa";

                        await _context.SaveChangesAsync();

                        await GhiLog(
                            taiKhoan.MaTaiKhoan,
                            "TaiKhoanBiKhoa",
                            "Tài khoản bị khóa do nhập sai mật khẩu 3 lần",
                            "ThatBai"
                        );

                        return Unauthorized(new
                        {
                            ThongBao =
                                "Bạn đã nhập sai 3 lần. Tài khoản đã bị khóa!"
                        });
                    }

                    await _context.SaveChangesAsync();

                    await GhiLog(
                        taiKhoan.MaTaiKhoan,
                        "DangNhapThatBai",
                        $"Nhập sai mật khẩu lần {taiKhoan.SoLanDangNhapSai}",
                        "ThatBai"
                    );

                    return Unauthorized(new
                    {
                        ThongBao =
                            $"Mật khẩu không đúng! Bạn còn {3 - taiKhoan.SoLanDangNhapSai} lần thử."
                    });
                }

                // =====================================================
                // 6. ĐĂNG NHẬP THÀNH CÔNG
                // =====================================================

                taiKhoan.SoLanDangNhapSai = 0;

                await _context.SaveChangesAsync();

                await GhiLog(
                    taiKhoan.MaTaiKhoan,
                    "DangNhap",
                    "Đăng nhập thành công",
                    "ThanhCong"
                );

                // =====================================================
                // 7. TẠO JWT CLAIM
                // =====================================================

                var claims = new[]
                {
                    new Claim(
                        ClaimTypes.NameIdentifier,
                        taiKhoan.MaTaiKhoan.ToString()
                    ),

                    new Claim(
                        ClaimTypes.Name,
                        taiKhoan.TenDangNhap
                    ),

                    new Claim(
                        ClaimTypes.Role,
                        taiKhoan.VaiTro
                    ),

                    new Claim(
                        "HoTen",
                        taiKhoan.HoTen
                    )
                };

                // =====================================================
                // 8. TẠO KEY
                // =====================================================

                var key = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(
                        "TongDaiNoiBo_Secure_Key_2026_VeryStrong!"
                    )
                );

                var credentials =
                    new SigningCredentials(
                        key,
                        SecurityAlgorithms.HmacSha256
                    );

                // =====================================================
                // 9. TẠO TOKEN
                // =====================================================

                var token = new JwtSecurityToken(
                    issuer: "TongDaiNoiBo",
                    audience: "TongDaiNoiBoClient",
                    claims: claims,
                    expires: DateTime.UtcNow.AddHours(2),
                    signingCredentials: credentials
                );

                string accessToken =
                    new JwtSecurityTokenHandler()
                        .WriteToken(token);

                // =====================================================
                // 10. TRẢ KẾT QUẢ
                // =====================================================

                return Ok(new
                {
                    ThongBao = "Đăng nhập thành công!",

                    AccessToken = accessToken,

                    MaTaiKhoan = taiKhoan.MaTaiKhoan,

                    TenDangNhap = taiKhoan.TenDangNhap,

                    HoTen = taiKhoan.HoTen,

                    VaiTro = taiKhoan.VaiTro
                });
            }
            catch
            {
                return StatusCode(500, new
                {
                    ThongBao =
                        "Có lỗi xảy ra khi đăng nhập. Vui lòng thử lại sau!"
                });
            }
        }

        // =========================================================
        // GHI LOG
        // =========================================================

        private async Task GhiLog(
            int maTaiKhoan,
            string hanhDong,
            string noiDung,
            string trangThai)
        {
            var log = new LogHoatDong
            {
                MaTaiKhoan = maTaiKhoan,

                HanhDong = hanhDong,

                ThoiGian = DateTime.Now,

                DiaChiIP =
                    HttpContext.Connection
                        .RemoteIpAddress?
                        .ToString(),

                NoiDung = noiDung,

                TrangThai = trangThai
            };

            _context.LogHoatDong.Add(log);

            await _context.SaveChangesAsync();
        }
    }
}