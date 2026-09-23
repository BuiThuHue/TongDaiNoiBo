using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TongDaiNoiBo.Data;
using TongDaiNoiBo.DTOs;
using TongDaiNoiBo.Models;

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
        // 1. ADMIN - LẤY DANH SÁCH TÀI KHOẢN
        // =========================================================
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> LayDanhSachTaiKhoan()
        {
            var danhSach = await _context.TaiKhoan
                .OrderBy(tk => tk.MaTaiKhoan)
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
                        !string.IsNullOrEmpty(tk.PublicKeyRSA),

                    SoMay = _context.SoMayNoiBo
                        .Where(sm => sm.MaTaiKhoan == tk.MaTaiKhoan)
                        .Select(sm => sm.SoMay)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(danhSach);
        }

        // =========================================================
        // 2. ADMIN - LẤY THÔNG TIN MỘT TÀI KHOẢN
        // =========================================================
        [Authorize(Roles = "Admin")]
        [HttpGet("{maTaiKhoan}")]
        public async Task<IActionResult> LayTaiKhoan(int maTaiKhoan)
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
                        !string.IsNullOrEmpty(tk.PublicKeyRSA),

                    SoMay = _context.SoMayNoiBo
                        .Where(sm => sm.MaTaiKhoan == tk.MaTaiKhoan)
                        .Select(sm => sm.SoMay)
                        .FirstOrDefault()
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
        // 3. ADMIN - TẠO TÀI KHOẢN NHÂN VIÊN
        // =========================================================
        [Authorize(Roles = "Admin")]
        [HttpPost("TaoTaiKhoan")]
        public async Task<IActionResult> TaoTaiKhoan(
            [FromBody] TaoTaiKhoanDTO dto)
        {
            // -----------------------------------------------------
            // Kiểm tra dữ liệu
            // -----------------------------------------------------
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

            // Xóa khoảng trắng thừa
            string tenDangNhap = dto.TenDangNhap.Trim();
            string hoTen = dto.HoTen.Trim();
            string soMayMoi = dto.SoMay.Trim();

            string? email = string.IsNullOrWhiteSpace(dto.Email)
                ? null
                : dto.Email.Trim();

            // -----------------------------------------------------
            // Kiểm tra tên đăng nhập
            // -----------------------------------------------------
            bool tonTaiTenDangNhap =
                await _context.TaiKhoan.AnyAsync(
                    tk => tk.TenDangNhap == tenDangNhap
                );

            if (tonTaiTenDangNhap)
            {
                return BadRequest(new
                {
                    ThongBao = "Tên đăng nhập đã tồn tại!"
                });
            }

            // -----------------------------------------------------
            // Kiểm tra Email nếu có nhập
            // -----------------------------------------------------
            if (!string.IsNullOrWhiteSpace(email))
            {
                bool tonTaiEmail =
                    await _context.TaiKhoan.AnyAsync(
                        tk => tk.Email == email
                    );

                if (tonTaiEmail)
                {
                    return BadRequest(new
                    {
                        ThongBao = "Email đã được sử dụng!"
                    });
                }
            }

            // -----------------------------------------------------
            // Kiểm tra số máy
            // -----------------------------------------------------
            bool tonTaiSoMay =
                await _context.SoMayNoiBo.AnyAsync(
                    sm => sm.SoMay == soMayMoi
                );

            if (tonTaiSoMay)
            {
                return BadRequest(new
                {
                    ThongBao = "Số máy nội bộ đã tồn tại!"
                });
            }

            // -----------------------------------------------------
            // Băm mật khẩu bằng BCrypt
            // -----------------------------------------------------
            string matKhauDaBam =
                BCrypt.Net.BCrypt.HashPassword(dto.MatKhau);

            // -----------------------------------------------------
            // Tạo tài khoản
            //
            // QUAN TRỌNG:
            // Admin KHÔNG tạo RSA cho nhân viên.
            //
            // PublicKeyRSA = null khi tài khoản vừa được tạo.
            //
            // Sau này nhân viên đăng nhập:
            // Browser tự tạo RSA.
            //
            // Public Key  -> gửi Server.
            // Private Key -> giữ trên thiết bị nhân viên.
            // -----------------------------------------------------
            var taiKhoan = new TaiKhoan
            {
                TenDangNhap = tenDangNhap,
                MatKhau = matKhauDaBam,
                HoTen = hoTen,
                Email = email,

                VaiTro = "NhanVien",
                TrangThai = "HoatDong",

                SoLanDangNhapSai = 0,
                NgayTao = DateTime.UtcNow,

                PublicKeyRSA = null
            };

            _context.TaiKhoan.Add(taiKhoan);

            // Lưu trước để lấy MaTaiKhoan
            await _context.SaveChangesAsync();

            // -----------------------------------------------------
            // Tạo số máy nội bộ cho nhân viên
            // -----------------------------------------------------
            var soMay = new SoMayNoiBo
            {
                SoMay = soMayMoi,
                MaTaiKhoan = taiKhoan.MaTaiKhoan,
                TrangThai = "DaCap"
            };

            _context.SoMayNoiBo.Add(soMay);

            // -----------------------------------------------------
            // Ghi log
            // -----------------------------------------------------
            int? maAdmin = LayMaTaiKhoanDangNhap();

            var log = new LogHoatDong
            {
                MaTaiKhoan = maAdmin,

                HanhDong = "TAO_TAI_KHOAN",

                ThoiGian = DateTime.UtcNow,

                DiaChiIP =
                    HttpContext.Connection
                        .RemoteIpAddress?
                        .ToString(),

                NoiDung =
                    $"Admin tạo tài khoản {taiKhoan.TenDangNhap}, " +
                    $"số máy {soMayMoi}.",

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
                taiKhoan.Email,
                taiKhoan.VaiTro,
                taiKhoan.TrangThai,

                SoMay = soMayMoi,

                DaThietLapChuKySo = false
            });
        }

        // =========================================================
        // 4. ADMIN - KHÓA TÀI KHOẢN
        // =========================================================
        [Authorize(Roles = "Admin")]
        [HttpPut("{maTaiKhoan}/Khoa")]
        public async Task<IActionResult> KhoaTaiKhoan(
            int maTaiKhoan)
        {
            var taiKhoan =
                await _context.TaiKhoan.FindAsync(maTaiKhoan);

            if (taiKhoan == null)
            {
                return NotFound(new
                {
                    ThongBao = "Không tìm thấy tài khoản!"
                });
            }

            // Không cho khóa Admin
            if (taiKhoan.VaiTro == "Admin")
            {
                return BadRequest(new
                {
                    ThongBao =
                        "Không được khóa tài khoản Admin!"
                });
            }

            if (taiKhoan.TrangThai == "BiKhoa")
            {
                return BadRequest(new
                {
                    ThongBao =
                        "Tài khoản này đã bị khóa!"
                });
            }

            taiKhoan.TrangThai = "BiKhoa";

            // -----------------------------------------------------
            // Ghi log
            // -----------------------------------------------------
            int? maAdmin = LayMaTaiKhoanDangNhap();

            var log = new LogHoatDong
            {
                MaTaiKhoan = maAdmin,

                HanhDong = "KHOA_TAI_KHOAN",

                ThoiGian = DateTime.UtcNow,

                DiaChiIP =
                    HttpContext.Connection
                        .RemoteIpAddress?
                        .ToString(),

                NoiDung =
                    $"Admin khóa tài khoản " +
                    $"{taiKhoan.TenDangNhap} " +
                    $"(MaTaiKhoan: {taiKhoan.MaTaiKhoan}).",

                TrangThai = "ThanhCong"
            };

            _context.LogHoatDong.Add(log);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                ThongBao = "Khóa tài khoản thành công!",

                taiKhoan.MaTaiKhoan,
                taiKhoan.TenDangNhap,
                taiKhoan.TrangThai
            });
        }

        // =========================================================
        // 5. ADMIN - MỞ KHÓA TÀI KHOẢN
        // =========================================================
        [Authorize(Roles = "Admin")]
        [HttpPut("{maTaiKhoan}/MoKhoa")]
        public async Task<IActionResult> MoKhoaTaiKhoan(
            int maTaiKhoan)
        {
            var taiKhoan =
                await _context.TaiKhoan.FindAsync(maTaiKhoan);

            if (taiKhoan == null)
            {
                return NotFound(new
                {
                    ThongBao = "Không tìm thấy tài khoản!"
                });
            }

            if (taiKhoan.VaiTro == "Admin")
            {
                return BadRequest(new
                {
                    ThongBao =
                        "Không thực hiện mở khóa tài khoản Admin!"
                });
            }

            if (taiKhoan.TrangThai == "HoatDong")
            {
                return BadRequest(new
                {
                    ThongBao =
                        "Tài khoản này đang hoạt động!"
                });
            }

            taiKhoan.TrangThai = "HoatDong";

            // Reset số lần đăng nhập sai
            taiKhoan.SoLanDangNhapSai = 0;

            // -----------------------------------------------------
            // Ghi log
            // -----------------------------------------------------
            int? maAdmin = LayMaTaiKhoanDangNhap();

            var log = new LogHoatDong
            {
                MaTaiKhoan = maAdmin,

                HanhDong = "MO_KHOA_TAI_KHOAN",

                ThoiGian = DateTime.UtcNow,

                DiaChiIP =
                    HttpContext.Connection
                        .RemoteIpAddress?
                        .ToString(),

                NoiDung =
                    $"Admin mở khóa tài khoản " +
                    $"{taiKhoan.TenDangNhap} " +
                    $"(MaTaiKhoan: {taiKhoan.MaTaiKhoan}).",

                TrangThai = "ThanhCong"
            };

            _context.LogHoatDong.Add(log);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                ThongBao =
                    "Mở khóa tài khoản thành công!",

                taiKhoan.MaTaiKhoan,
                taiKhoan.TenDangNhap,
                taiKhoan.TrangThai
            });
        }

        // =========================================================
        // 6. NHÂN VIÊN - THIẾT LẬP CHỮ KÝ SỐ
        // =========================================================
        //
        // Trình duyệt tạo cặp khóa RSA:
        //
        // Public Key
        //      ↓
        // gửi lên API này
        //
        // Private Key
        //      ↓
        // mã hóa bằng PIN + AES-GCM
        //      ↓
        // lưu tại thiết bị nhân viên
        //
        // Server KHÔNG nhận:
        // - Private Key
        // - PIN chữ ký số
        //
        // =========================================================
        [Authorize(Roles = "NhanVien")]
        [HttpPost("ThietLapChuKySo")]
        public async Task<IActionResult> ThietLapChuKySo(
            [FromBody] ThietLapChuKySoDTO dto)
        {
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

            // -----------------------------------------------------
            // Kiểm tra Public Key
            // -----------------------------------------------------
            if (string.IsNullOrWhiteSpace(
                dto.PublicKeyRSA))
            {
                return BadRequest(new
                {
                    ThongBao =
                        "Public Key RSA không được để trống!"
                });
            }

            // -----------------------------------------------------
            // Tìm tài khoản
            // -----------------------------------------------------
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

            // -----------------------------------------------------
            // Chỉ tài khoản hoạt động mới được thiết lập
            // -----------------------------------------------------
            if (taiKhoan.TrangThai != "HoatDong")
            {
                return BadRequest(new
                {
                    ThongBao =
                        "Tài khoản hiện không hoạt động!"
                });
            }

            // -----------------------------------------------------
            // Đảm bảo đúng vai trò nhân viên
            // -----------------------------------------------------
            if (taiKhoan.VaiTro != "NhanVien")
            {
                return Forbid();
            }

            // -----------------------------------------------------
            // Lưu Public Key RSA
            // -----------------------------------------------------
            taiKhoan.PublicKeyRSA =
                dto.PublicKeyRSA.Trim();

            // -----------------------------------------------------
            // Ghi log
            // -----------------------------------------------------
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

            await _context.SaveChangesAsync();

            return Ok(new
            {
                ThongBao =
                    "Thiết lập chữ ký số thành công!",

                MaTaiKhoan =
                    taiKhoan.MaTaiKhoan,

                HoTen =
                    taiKhoan.HoTen,

                DaThietLapChuKySo = true
            });
        }

        // =========================================================
        // HÀM HỖ TRỢ
        // LẤY MÃ TÀI KHOẢN ĐANG ĐĂNG NHẬP TỪ JWT
        // =========================================================
        private int? LayMaTaiKhoanDangNhap()
        {
            string? claim =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier
                );

            if (int.TryParse(claim, out int maTaiKhoan))
            {
                return maTaiKhoan;
            }

            return null;
        }
    }
}