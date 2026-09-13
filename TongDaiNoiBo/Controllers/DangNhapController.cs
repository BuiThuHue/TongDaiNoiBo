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

        [HttpPost]
        public async Task<IActionResult> DangNhap(DangNhapDTO dto)
        {
            try
            {
                // Tìm tài khoản
                var taiKhoan = await _context.TaiKhoan
                    .FirstOrDefaultAsync(x =>
                        x.TenDangNhap == dto.TenDangNhap);

                // Không tìm thấy tài khoản
                if (taiKhoan == null)
                {
                    return Unauthorized(new
                    {
                        ThongBao = "Tên đăng nhập hoặc mật khẩu không đúng!"
                    });
                }

                // Kiểm tra tài khoản bị khóa
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

                // Kiểm tra mật khẩu bằng BCrypt
                bool matKhauDung = BCrypt.Net.BCrypt.Verify(
                    dto.MatKhau,
                    taiKhoan.MatKhau
                );

                // MẬT KHẨU SAI
                if (!matKhauDung)
                {
                    taiKhoan.SoLanDangNhapSai++;

                    // Sai đủ 3 lần → khóa tài khoản
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

                // ĐĂNG NHẬP THÀNH CÔNG
                taiKhoan.SoLanDangNhapSai = 0;

                await _context.SaveChangesAsync();

                await GhiLog(
                    taiKhoan.MaTaiKhoan,
                    "DangNhap",
                    "Đăng nhập thành công",
                    "ThanhCong"
                );

                // ==============================
                // TẠO JWT TOKEN
                // ==============================

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

                var key = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(
                        "TongDaiNoiBo_Secure_Key_2026_VeryStrong!"
                    )
                );

                var credentials = new SigningCredentials(
                    key,
                    SecurityAlgorithms.HmacSha256
                );

                var token = new JwtSecurityToken(
                    claims: claims,
                    expires: DateTime.UtcNow.AddHours(2),
                    signingCredentials: credentials
                );

                var accessToken = new JwtSecurityTokenHandler()
                    .WriteToken(token);

                // Trả kết quả đăng nhập
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
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    ThongBao = "Có lỗi xảy ra khi đăng nhập!",
                    ChiTiet = ex.Message
                });
            }
        }

        // ==============================
        // HÀM GHI LOG
        // ==============================
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
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                NoiDung = noiDung,
                TrangThai = trangThai
            };

            _context.LogHoatDong.Add(log);

            await _context.SaveChangesAsync();
        }
    }
}