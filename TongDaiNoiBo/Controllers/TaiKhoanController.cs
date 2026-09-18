using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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


        // ==========================================
        // 1. LẤY DANH SÁCH TÀI KHOẢN
        // ==========================================

        [Authorize(Roles = "Admin")]
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
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    ThongBao =
                        "Lỗi khi đọc danh sách tài khoản!"
                });
            }
        }


        // ==========================================
        // 2. KIỂM TRA API BẢO MẬT
        // ==========================================

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


        // ==========================================
        // 3. CHỈ ADMIN ĐƯỢC TRUY CẬP
        // ==========================================

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


        // ==========================================
        // 4. LẤY THÔNG TIN MỘT TÀI KHOẢN
        // ==========================================

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
                        ThongBao =
                            "Không tìm thấy tài khoản!"
                    });
                }

                return Ok(taiKhoan);
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    ThongBao =
                        "Lỗi khi lấy thông tin tài khoản!"
                });
            }
        }


        // ==========================================
        // 5. LẤY TÀI KHOẢN HIỆN TẠI
        // ==========================================

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
                        ThongBao =
                            "Không xác định được tài khoản đăng nhập!"
                    });
                }

                if (!int.TryParse(
                    maTaiKhoanClaim,
                    out int maTaiKhoan))
                {
                    return Unauthorized(new
                    {
                        ThongBao =
                            "Thông tin tài khoản không hợp lệ!"
                    });
                }

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
                        ThongBao =
                            "Không tìm thấy tài khoản!"
                    });
                }

                return Ok(taiKhoan);
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    ThongBao =
                        "Lỗi khi lấy tài khoản hiện tại!"
                });
            }
        }


        // ==========================================
        // 6. ADMIN TẠO TÀI KHOẢN NHÂN VIÊN
        // ==========================================

        [Authorize(Roles = "Admin")]
        [HttpPost("TaoNhanVien")]
        public async Task<IActionResult> TaoNhanVien(
            TaoTaiKhoanDTO dto)
        {
            try
            {
                // ==========================================
                // 6.1 KIỂM TRA DỮ LIỆU ĐẦU VÀO
                // ==========================================

                if (dto == null)
                {
                    return BadRequest(new
                    {
                        ThongBao =
                            "Dữ liệu tạo tài khoản không hợp lệ!"
                    });
                }

                if (string.IsNullOrWhiteSpace(dto.TenDangNhap) ||
                    string.IsNullOrWhiteSpace(dto.MatKhau) ||
                    string.IsNullOrWhiteSpace(dto.HoTen) ||
                    string.IsNullOrWhiteSpace(dto.SoMay))
                {
                    return BadRequest(new
                    {
                        ThongBao =
                            "Vui lòng nhập đầy đủ thông tin!"
                    });
                }


                // ==========================================
                // 6.2 KIỂM TRA TÊN ĐĂNG NHẬP
                // ==========================================

                bool tonTaiTenDangNhap =
                    await _context.TaiKhoan
                        .AnyAsync(x =>
                            x.TenDangNhap ==
                            dto.TenDangNhap);

                if (tonTaiTenDangNhap)
                {
                    return BadRequest(new
                    {
                        ThongBao =
                            "Tên đăng nhập đã tồn tại!"
                    });
                }


                // ==========================================
                // 6.3 KIỂM TRA EMAIL
                // ==========================================

                if (!string.IsNullOrWhiteSpace(dto.Email))
                {
                    bool tonTaiEmail =
                        await _context.TaiKhoan
                            .AnyAsync(x =>
                                x.Email == dto.Email);

                    if (tonTaiEmail)
                    {
                        return BadRequest(new
                        {
                            ThongBao =
                                "Email đã được sử dụng!"
                        });
                    }
                }


                // ==========================================
                // 6.4 KIỂM TRA SỐ MÁY
                // ==========================================

                var soMay = await _context.SoMayNoiBo
                    .FirstOrDefaultAsync(x =>
                        x.SoMay == dto.SoMay);

                if (soMay == null)
                {
                    return BadRequest(new
                    {
                        ThongBao =
                            "Số máy nội bộ không tồn tại!"
                    });
                }

                if (soMay.MaTaiKhoan != null)
                {
                    return BadRequest(new
                    {
                        ThongBao =
                            "Số máy này đã được cấp cho tài khoản khác!"
                    });
                }


                // ==========================================
                // 6.5 TẠO CẶP KHÓA RSA
                // ==========================================

                var capKhoaRSA =
                    _rsaService.TaoCapKhoaRSA();


                // ==========================================
                // 6.6 BĂM MẬT KHẨU BẰNG BCRYPT
                // ==========================================

                string matKhauDaBam =
                    BCrypt.Net.BCrypt.HashPassword(
                        dto.MatKhau
                    );


                // ==========================================
                // 6.7 TẠO TÀI KHOẢN NHÂN VIÊN
                // ==========================================

                var taiKhoan = new TaiKhoan
                {
                    TenDangNhap =
                        dto.TenDangNhap,

                    MatKhau =
                        matKhauDaBam,

                    HoTen =
                        dto.HoTen,

                    Email =
                        string.IsNullOrWhiteSpace(dto.Email)
                            ? null
                            : dto.Email,

                    VaiTro =
                        "NhanVien",

                    TrangThai =
                        "HoatDong",

                    SoLanDangNhapSai =
                        0,

                    NgayTao =
                        DateTime.Now,

                    // ======================================
                    // CHỈ LƯU PUBLIC KEY TRÊN SERVER
                    // ======================================

                    PublicKeyRSA =
                        capKhoaRSA.PublicKey
                };

                _context.TaiKhoan.Add(taiKhoan);

                await _context.SaveChangesAsync();


                // ==========================================
                // 6.8 CẤP SỐ MÁY CHO NHÂN VIÊN
                // ==========================================

                soMay.MaTaiKhoan =
                    taiKhoan.MaTaiKhoan;

                soMay.TrangThai =
                    "DaCap";

                await _context.SaveChangesAsync();


                // ==========================================
                // 6.9 LẤY ID ADMIN ĐANG THỰC HIỆN
                // ==========================================

                var maAdminClaim =
                    User.FindFirst(
                        System.Security.Claims.ClaimTypes.NameIdentifier
                    )?.Value;

                int? maAdmin = null;

                if (int.TryParse(
                    maAdminClaim,
                    out int idAdmin))
                {
                    maAdmin = idAdmin;
                }


                // ==========================================
                // 6.10 GHI LOG
                // ==========================================

                var log = new LogHoatDong
                {
                    MaTaiKhoan =
                        maAdmin,

                    HanhDong =
                        "TaoTaiKhoanNhanVien",

                    ThoiGian =
                        DateTime.Now,

                    DiaChiIP =
                        HttpContext.Connection
                            .RemoteIpAddress?
                            .ToString(),

                    NoiDung =
                        $"Admin tạo tài khoản {dto.TenDangNhap}, " +
                        $"cấp số máy {dto.SoMay}",

                    TrangThai =
                        "ThanhCong"
                };

                _context.LogHoatDong.Add(log);

                await _context.SaveChangesAsync();


                // ==========================================
                // 6.11 TRẢ KẾT QUẢ
                // ==========================================
                //
                // Public Key có thể lưu trên server.
                //
                // Private Key KHÔNG lưu vào database.
                //
                // Private Key chỉ được trả về một lần
                // để người được cấp tài khoản lưu giữ.
                //
                // ==========================================

                return Ok(new
                {
                    ThongBao =
                        "Tạo tài khoản nhân viên thành công!",

                    MaTaiKhoan =
                        taiKhoan.MaTaiKhoan,

                    TenDangNhap =
                        taiKhoan.TenDangNhap,

                    HoTen =
                        taiKhoan.HoTen,

                    SoMay =
                        soMay.SoMay,

                    VaiTro =
                        taiKhoan.VaiTro,

                    PublicKeyRSA =
                        taiKhoan.PublicKeyRSA,

                    PrivateKeyRSA =
                        capKhoaRSA.PrivateKey
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    ThongBao =
                        "Có lỗi xảy ra khi tạo tài khoản nhân viên!"
                });
            }
        }
    }
}