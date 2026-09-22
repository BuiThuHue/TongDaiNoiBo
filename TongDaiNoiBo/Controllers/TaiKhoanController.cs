using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TongDaiNoiBo.Data;
using TongDaiNoiBo.DTOs;
using TongDaiNoiBo.Models;
using TongDaiNoiBo.Security;

namespace TongDaiNoiBo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TaiKhoanController : ControllerBase
    {
        private readonly TongDaiDbContext _context;
        private readonly RsaService _rsaService;

        public TaiKhoanController(
            TongDaiDbContext context,
            RsaService rsaService)
        {
            _context = context;
            _rsaService = rsaService;
        }

        // =========================================================
        // 1. LẤY DANH SÁCH TÀI KHOẢN
        // =========================================================
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> LayDanhSachTaiKhoan()
        {
            var danhSach = await _context.TaiKhoan
                .Select(tk => new
                {
                    tk.MaTaiKhoan,
                    tk.TenDangNhap,
                    tk.HoTen,
                    tk.Email,
                    tk.VaiTro,
                    tk.TrangThai,
                    tk.SoLanDangNhapSai,
                    tk.NgayTao,

                    CoPublicKeyRSA =
                        !string.IsNullOrEmpty(tk.PublicKeyRSA)
                })
                .ToListAsync();

            return Ok(danhSach);
        }

        // =========================================================
        // 2. LẤY THÔNG TIN MỘT TÀI KHOẢN
        // =========================================================
        [Authorize]
        [HttpGet("{maTaiKhoan}")]
        public async Task<IActionResult> LayTaiKhoan(
            int maTaiKhoan)
        {
            var taiKhoan = await _context.TaiKhoan
                .Where(tk => tk.MaTaiKhoan == maTaiKhoan)
                .Select(tk => new
                {
                    tk.MaTaiKhoan,
                    tk.TenDangNhap,
                    tk.HoTen,
                    tk.Email,
                    tk.VaiTro,
                    tk.TrangThai,
                    tk.SoLanDangNhapSai,
                    tk.NgayTao,

                    CoPublicKeyRSA =
                        !string.IsNullOrEmpty(tk.PublicKeyRSA)
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

        // =========================================================
        // 3. ADMIN TẠO TÀI KHOẢN NHÂN VIÊN
        // =========================================================
        [Authorize(Roles = "Admin")]
        [HttpPost("TaoTaiKhoan")]
        public async Task<IActionResult> TaoTaiKhoan(
            [FromBody] TaoTaiKhoanDTO dto)
        {
            // Kiểm tra dữ liệu
            if (string.IsNullOrWhiteSpace(dto.TenDangNhap) ||
                string.IsNullOrWhiteSpace(dto.MatKhau) ||
                string.IsNullOrWhiteSpace(dto.HoTen) ||
                string.IsNullOrWhiteSpace(dto.SoMay))
            {
                return BadRequest(new
                {
                    ThongBao =
                        "Vui lòng nhập đầy đủ tên đăng nhập, mật khẩu, họ tên và số máy!"
                });
            }

            // Kiểm tra tên đăng nhập
            bool tonTaiTenDangNhap =
                await _context.TaiKhoan.AnyAsync(
                    tk => tk.TenDangNhap == dto.TenDangNhap
                );

            if (tonTaiTenDangNhap)
            {
                return BadRequest(new
                {
                    ThongBao = "Tên đăng nhập đã tồn tại!"
                });
            }

            // Kiểm tra số máy
            bool tonTaiSoMay =
                await _context.SoMayNoiBo.AnyAsync(
                    sm => sm.SoMay == dto.SoMay
                );

            if (tonTaiSoMay)
            {
                return BadRequest(new
                {
                    ThongBao = "Số máy nội bộ đã tồn tại!"
                });
            }

            // =====================================================
            // Tạm thời vẫn giữ cách tạo RSA cũ
            // để các chức năng hiện tại chưa bị hỏng.
            //
            // Sau khi hoàn thành ký số phía Client,
            // chúng ta sẽ sửa phần này.
            // =====================================================
            var capKhoa =
                _rsaService.TaoCapKhoaRSA();

            // Băm mật khẩu bằng BCrypt
            string matKhauDaBam =
                BCrypt.Net.BCrypt.HashPassword(
                    dto.MatKhau
                );

            // Tạo tài khoản
            var taiKhoan = new TaiKhoan
            {
                TenDangNhap = dto.TenDangNhap,
                MatKhau = matKhauDaBam,
                HoTen = dto.HoTen,
                Email = dto.Email,

                VaiTro = "NhanVien",
                TrangThai = "HoatDong",

                SoLanDangNhapSai = 0,
                NgayTao = DateTime.UtcNow,

                PublicKeyRSA = capKhoa.PublicKey
            };

            _context.TaiKhoan.Add(taiKhoan);

            // Lưu để lấy MaTaiKhoan
            await _context.SaveChangesAsync();

            // Tạo số máy nội bộ
            var soMay = new SoMayNoiBo
            {
                SoMay = dto.SoMay,
                MaTaiKhoan = taiKhoan.MaTaiKhoan,
                TrangThai = "DaCap"
            };

            _context.SoMayNoiBo.Add(soMay);

            // Ghi log
            var log = new LogHoatDong
            {
                MaTaiKhoan = taiKhoan.MaTaiKhoan,

                HanhDong = "TAO_TAI_KHOAN",

                ThoiGian = DateTime.UtcNow,

                DiaChiIP =
                    HttpContext.Connection
                        .RemoteIpAddress?
                        .ToString(),

                NoiDung =
                    $"Admin tạo tài khoản {taiKhoan.TenDangNhap}, số máy {dto.SoMay}.",

                TrangThai = "ThanhCong"
            };

            _context.LogHoatDong.Add(log);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                ThongBao =
                    "Tạo tài khoản nhân viên thành công!",

                taiKhoan.MaTaiKhoan,
                taiKhoan.TenDangNhap,
                taiKhoan.HoTen,
                taiKhoan.VaiTro,

                SoMay = dto.SoMay,

                PublicKeyRSA =
                    capKhoa.PublicKey,

                PrivateKeyRSA =
                    capKhoa.PrivateKey
            });
        }

        // =========================================================
        // 4. ADMIN KHÓA TÀI KHOẢN
        // =========================================================
        [Authorize(Roles = "Admin")]
        [HttpPut("{maTaiKhoan}/Khoa")]
        public async Task<IActionResult> KhoaTaiKhoan(
            int maTaiKhoan)
        {
            var taiKhoan =
                await _context.TaiKhoan.FindAsync(
                    maTaiKhoan
                );

            if (taiKhoan == null)
            {
                return NotFound(new
                {
                    ThongBao =
                        "Không tìm thấy tài khoản!"
                });
            }

            if (taiKhoan.VaiTro == "Admin")
            {
                return BadRequest(new
                {
                    ThongBao =
                        "Không được khóa tài khoản Admin!"
                });
            }

            taiKhoan.TrangThai = "BiKhoa";

            var log = new LogHoatDong
            {
                MaTaiKhoan = maTaiKhoan,

                HanhDong = "KHOA_TAI_KHOAN",

                ThoiGian = DateTime.UtcNow,

                DiaChiIP =
                    HttpContext.Connection
                        .RemoteIpAddress?
                        .ToString(),

                NoiDung =
                    $"Tài khoản {taiKhoan.TenDangNhap} bị khóa.",

                TrangThai = "ThanhCong"
            };

            _context.LogHoatDong.Add(log);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                ThongBao =
                    "Khóa tài khoản thành công!"
            });
        }

        // =========================================================
        // 5. ADMIN MỞ KHÓA TÀI KHOẢN
        // =========================================================
        [Authorize(Roles = "Admin")]
        [HttpPut("{maTaiKhoan}/MoKhoa")]
        public async Task<IActionResult> MoKhoaTaiKhoan(
            int maTaiKhoan)
        {
            var taiKhoan =
                await _context.TaiKhoan.FindAsync(
                    maTaiKhoan
                );

            if (taiKhoan == null)
            {
                return NotFound(new
                {
                    ThongBao =
                        "Không tìm thấy tài khoản!"
                });
            }

            taiKhoan.TrangThai = "HoatDong";
            taiKhoan.SoLanDangNhapSai = 0;

            var log = new LogHoatDong
            {
                MaTaiKhoan = maTaiKhoan,

                HanhDong = "MO_KHOA_TAI_KHOAN",

                ThoiGian = DateTime.UtcNow,

                DiaChiIP =
                    HttpContext.Connection
                        .RemoteIpAddress?
                        .ToString(),

                NoiDung =
                    $"Tài khoản {taiKhoan.TenDangNhap} được mở khóa.",

                TrangThai = "ThanhCong"
            };

            _context.LogHoatDong.Add(log);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                ThongBao =
                    "Mở khóa tài khoản thành công!"
            });
        }

        // =========================================================
        // 6. NHÂN VIÊN THIẾT LẬP CHỮ KÝ SỐ
        // =========================================================
        //
        // Trình duyệt tạo:
        //
        // Public Key  -> gửi lên API này
        // Private Key -> giữ ở phía nhân viên
        //
        // Server KHÔNG nhận:
        // - Private Key
        // - PIN ký số
        //
        // =========================================================
        [Authorize(Roles = "NhanVien")]
        [HttpPost("ThietLapChuKySo")]
        public async Task<IActionResult> ThietLapChuKySo(
            [FromBody] ThietLapChuKySoDTO dto)
        {
            // Lấy MaTaiKhoan từ JWT
            string? maTaiKhoanClaim =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier
                );

            if (!int.TryParse(
                maTaiKhoanClaim,
                out int maTaiKhoan))
            {
                return Unauthorized(new
                {
                    ThongBao =
                        "Không xác định được tài khoản đăng nhập!"
                });
            }

            // Kiểm tra Public Key
            if (string.IsNullOrWhiteSpace(
                dto.PublicKeyRSA))
            {
                return BadRequest(new
                {
                    ThongBao =
                        "Public Key RSA không được để trống!"
                });
            }

            // Tìm tài khoản hiện tại
            var taiKhoan =
                await _context.TaiKhoan.FindAsync(
                    maTaiKhoan
                );

            if (taiKhoan == null)
            {
                return NotFound(new
                {
                    ThongBao =
                        "Không tìm thấy tài khoản!"
                });
            }

            // Kiểm tra trạng thái
            if (taiKhoan.TrangThai != "HoatDong")
            {
                return BadRequest(new
                {
                    ThongBao =
                        "Tài khoản hiện không hoạt động!"
                });
            }

            // =====================================================
            // Lưu Public Key RSA mới vào tài khoản
            // =====================================================
            taiKhoan.PublicKeyRSA =
                dto.PublicKeyRSA;

            // Ghi log
            var log = new LogHoatDong
            {
                MaTaiKhoan = maTaiKhoan,

                HanhDong =
                    "THIET_LAP_CHU_KY_SO",

                ThoiGian =
                    DateTime.UtcNow,

                DiaChiIP =
                    HttpContext.Connection
                        .RemoteIpAddress?
                        .ToString(),

                NoiDung =
                    "Nhân viên thiết lập Public Key RSA cho chữ ký số.",

                TrangThai =
                    "ThanhCong"
            };

            _context.LogHoatDong.Add(log);

            // Lưu database
            await _context.SaveChangesAsync();

            return Ok(new
            {
                ThongBao =
                    "Thiết lập chữ ký số thành công!",

                MaTaiKhoan =
                    taiKhoan.MaTaiKhoan,

                HoTen =
                    taiKhoan.HoTen
            });
        }
    }
}