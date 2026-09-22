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
                // =================================================
                // 1. KIỂM TRA DỮ LIỆU
                // =================================================

                if (dto == null)
                {
                    return BadRequest(new
                    {
                        ThongBao =
                            "Dữ liệu đăng nhập không hợp lệ!"
                    });
                }

                if (string.IsNullOrWhiteSpace(
                    dto.TenDangNhap))
                {
                    return BadRequest(new
                    {
                        ThongBao =
                            "Vui lòng nhập tên đăng nhập!"
                    });
                }

                if (string.IsNullOrWhiteSpace(
                    dto.MatKhau))
                {
                    return BadRequest(new
                    {
                        ThongBao =
                            "Vui lòng nhập mật khẩu!"
                    });
                }


                // =================================================
                // 2. TÌM TÀI KHOẢN
                // =================================================

                var taiKhoan =
                    await _context.TaiKhoan
                        .FirstOrDefaultAsync(
                            x =>
                                x.TenDangNhap ==
                                dto.TenDangNhap.Trim()
                        );


                // =================================================
                // 3. KHÔNG TÌM THẤY TÀI KHOẢN
                // =================================================

                if (taiKhoan == null)
                {
                    return Unauthorized(new
                    {
                        ThongBao =
                            "Tên đăng nhập hoặc mật khẩu không đúng!"
                    });
                }


                // =================================================
                // 4. KIỂM TRA TÀI KHOẢN
                // =================================================

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
                        ThongBao =
                            "Tài khoản đã bị khóa!"
                    });
                }


                // =================================================
                // 5. KIỂM TRA MẬT KHẨU
                // =================================================

                bool matKhauDung = false;

                // -------------------------------------------------
                // KIỂM TRA XEM MẬT KHẨU ĐÃ LÀ BCrypt CHƯA
                // -------------------------------------------------

                bool laBCrypt =
                    !string.IsNullOrWhiteSpace(
                        taiKhoan.MatKhau
                    )
                    &&
                    (
                        taiKhoan.MatKhau.StartsWith("$2a$")
                        ||
                        taiKhoan.MatKhau.StartsWith("$2b$")
                        ||
                        taiKhoan.MatKhau.StartsWith("$2y$")
                    );


                // =================================================
                // 6. NẾU ĐÃ LÀ BCrypt
                // =================================================

                if (laBCrypt)
                {
                    matKhauDung =
                        BCrypt.Net.BCrypt.Verify(
                            dto.MatKhau,
                            taiKhoan.MatKhau
                        );
                }


                // =================================================
                // 7. NẾU DỮ LIỆU CŨ ĐANG LÀ MẬT KHẨU THƯỜNG
                // =================================================

                else
                {
                    // ---------------------------------------------
                    // So sánh mật khẩu người dùng nhập
                    // với mật khẩu cũ trong SQL
                    // ---------------------------------------------

                    if (dto.MatKhau ==
                        taiKhoan.MatKhau)
                    {
                        matKhauDung = true;

                        // -----------------------------------------
                        // ĐÚNG → TỰ ĐỘNG CHUYỂN SANG BCrypt
                        // -----------------------------------------

                        taiKhoan.MatKhau =
                            BCrypt.Net.BCrypt.HashPassword(
                                dto.MatKhau
                            );

                        await _context.SaveChangesAsync();
                    }
                }


                // =================================================
                // 8. MẬT KHẨU SAI
                // =================================================

                if (!matKhauDung)
                {
                    taiKhoan.SoLanDangNhapSai++;


                    // ---------------------------------------------
                    // SAI 3 LẦN → KHÓA
                    // ---------------------------------------------

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


                // =================================================
                // 9. ĐĂNG NHẬP THÀNH CÔNG
                // =================================================

                taiKhoan.SoLanDangNhapSai = 0;

                await _context.SaveChangesAsync();


                // =================================================
                // 10. GHI LOG
                // =================================================

                await GhiLog(
                    taiKhoan.MaTaiKhoan,
                    "DangNhap",
                    "Đăng nhập thành công",
                    "ThanhCong"
                );


                // =================================================
                // 11. TẠO CLAIM
                // =================================================

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


                // =================================================
                // 12. KHÓA JWT
                // =================================================

                var key =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            "TongDaiNoiBo_Secure_Key_2026_VeryStrong!"
                        )
                    );


                var credentials =
                    new SigningCredentials(
                        key,
                        SecurityAlgorithms.HmacSha256
                    );


                // =================================================
                // 13. TẠO TOKEN
                // =================================================

                var token =
                    new JwtSecurityToken(
                        claims: claims,

                        expires:
                            DateTime.UtcNow.AddHours(2),

                        signingCredentials:
                            credentials
                    );


                // =================================================
                // 14. CHUYỂN TOKEN THÀNH CHUỖI
                // =================================================

                var accessToken =
                    new JwtSecurityTokenHandler()
                        .WriteToken(token);


                // =================================================
                // 15. TRẢ KẾT QUẢ
                // =================================================

                return Ok(new
                {
                    ThongBao =
                        "Đăng nhập thành công!",

                    AccessToken =
                        accessToken,

                    MaTaiKhoan =
                        taiKhoan.MaTaiKhoan,

                    TenDangNhap =
                        taiKhoan.TenDangNhap,

                    HoTen =
                        taiKhoan.HoTen,

                    VaiTro =
                        taiKhoan.VaiTro
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    ThongBao =
                        "Có lỗi xảy ra khi đăng nhập. Vui lòng thử lại sau!"
                });
            }
        }


        // =========================================================
        // HÀM GHI LOG
        // =========================================================

        private async Task GhiLog(
            int maTaiKhoan,
            string hanhDong,
            string noiDung,
            string trangThai)
        {
            var log = new LogHoatDong
            {
                MaTaiKhoan =
                    maTaiKhoan,

                HanhDong =
                    hanhDong,

                ThoiGian =
                    DateTime.Now,

                DiaChiIP =
                    HttpContext.Connection
                        .RemoteIpAddress?
                        .ToString(),

                NoiDung =
                    noiDung,

                TrangThai =
                    trangThai
            };

            _context.LogHoatDong.Add(log);

            await _context.SaveChangesAsync();
        }
    }
}