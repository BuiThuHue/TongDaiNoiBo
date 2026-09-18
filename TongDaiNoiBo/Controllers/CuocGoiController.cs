using System.Security.Claims;
using System.Security.Cryptography;
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
    public class CuocGoiController : ControllerBase
    {
        private readonly TongDaiDbContext _context;
        private readonly RsaService _rsaService;

        public CuocGoiController(
            TongDaiDbContext context,
            RsaService rsaService)
        {
            _context = context;
            _rsaService = rsaService;
        }

        // =========================================================
        // HÀM LẤY MÃ TÀI KHOẢN TỪ JWT
        // =========================================================

        private bool LayMaTaiKhoan(out int maTaiKhoan)
        {
            maTaiKhoan = 0;

            string? value =
                User.FindFirst(
                    ClaimTypes.NameIdentifier
                )?.Value;

            return int.TryParse(value, out maTaiKhoan);
        }

        // =========================================================
        // 1. TẠO CUỘC GỌI
        // POST: /api/CuocGoi/TaoCuocGoi
        // =========================================================

        [Authorize]
        [HttpPost("TaoCuocGoi")]
        public async Task<IActionResult> TaoCuocGoi(
            [FromBody] TaoCuocGoiDTO dto)
        {
            try
            {
                // -----------------------------------------------------
                // Kiểm tra dữ liệu
                // -----------------------------------------------------

                if (dto == null)
                {
                    return BadRequest(new
                    {
                        ThongBao =
                            "Dữ liệu cuộc gọi không được để trống!"
                    });
                }

                if (dto.MaNguoiNhan <= 0)
                {
                    return BadRequest(new
                    {
                        ThongBao =
                            "Mã người nhận không hợp lệ!"
                    });
                }

                if (string.IsNullOrWhiteSpace(
                    dto.PrivateKeyRSA))
                {
                    return BadRequest(new
                    {
                        ThongBao =
                            "Chưa cung cấp Private Key RSA!"
                    });
                }

                // -----------------------------------------------------
                // Lấy người gọi từ JWT
                // -----------------------------------------------------

                if (!LayMaTaiKhoan(
                    out int maNguoiGoi))
                {
                    return Unauthorized(new
                    {
                        ThongBao =
                            "Không xác định được người gọi!"
                    });
                }

                // -----------------------------------------------------
                // Không gọi chính mình
                // -----------------------------------------------------

                if (maNguoiGoi ==
                    dto.MaNguoiNhan)
                {
                    return BadRequest(new
                    {
                        ThongBao =
                            "Không thể gọi cho chính tài khoản của mình!"
                    });
                }

                // -----------------------------------------------------
                // Tìm người gọi
                // -----------------------------------------------------

                var nguoiGoi =
                    await _context.TaiKhoan
                        .FirstOrDefaultAsync(
                            x =>
                                x.MaTaiKhoan ==
                                maNguoiGoi
                        );

                if (nguoiGoi == null)
                {
                    return NotFound(new
                    {
                        ThongBao =
                            "Tài khoản người gọi không tồn tại!"
                    });
                }

                if (nguoiGoi.TrangThai !=
                    "HoatDong")
                {
                    return BadRequest(new
                    {
                        ThongBao =
                            "Tài khoản người gọi đang bị khóa!"
                    });
                }

                // -----------------------------------------------------
                // Tìm người nhận
                // -----------------------------------------------------

                var nguoiNhan =
                    await _context.TaiKhoan
                        .FirstOrDefaultAsync(
                            x =>
                                x.MaTaiKhoan ==
                                dto.MaNguoiNhan
                        );

                if (nguoiNhan == null)
                {
                    return NotFound(new
                    {
                        ThongBao =
                            "Tài khoản người nhận không tồn tại!"
                    });
                }

                if (nguoiNhan.TrangThai !=
                    "HoatDong")
                {
                    return BadRequest(new
                    {
                        ThongBao =
                            "Tài khoản người nhận đang bị khóa!"
                    });
                }

                // -----------------------------------------------------
                // Kiểm tra Public Key
                // -----------------------------------------------------

                if (string.IsNullOrWhiteSpace(
                    nguoiGoi.PublicKeyRSA))
                {
                    return BadRequest(new
                    {
                        ThongBao =
                            "Người gọi chưa có Public Key RSA!"
                    });
                }

                // -----------------------------------------------------
                // Tạo Nonce chống Replay Attack
                // -----------------------------------------------------

                byte[] nonceBytes =
                    RandomNumberGenerator
                        .GetBytes(32);

                string nonce =
                    Convert.ToBase64String(
                        nonceBytes
                    );

                // -----------------------------------------------------
                // Thời gian
                // -----------------------------------------------------

                DateTime thoiGian =
                    DateTime.UtcNow;

                // -----------------------------------------------------
                // Tạo dữ liệu xác thực
                // -----------------------------------------------------

                string duLieuXacThuc =
                    $"{maNguoiGoi}|{dto.MaNguoiNhan}|{thoiGian.Ticks}|{nonce}";

                // -----------------------------------------------------
                // Ký bằng Private Key RSA
                // -----------------------------------------------------

                string chuKy =
                    _rsaService.KyDuLieu(
                        duLieuXacThuc,
                        dto.PrivateKeyRSA
                    );

                // -----------------------------------------------------
                // Kiểm tra chữ ký
                // -----------------------------------------------------

                bool chuKyHopLe =
                    _rsaService.KiemTraChuKy(
                        duLieuXacThuc,
                        chuKy,
                        nguoiGoi.PublicKeyRSA
                    );

                if (!chuKyHopLe)
                {
                    return BadRequest(new
                    {
                        ThongBao =
                            "Chữ ký RSA không hợp lệ!"
                    });
                }

                // -----------------------------------------------------
                // Tạo cuộc gọi
                // -----------------------------------------------------

                var cuocGoi = new CuocGoi
                {
                    MaNguoiGoi =
                        maNguoiGoi,

                    MaNguoiNhan =
                        dto.MaNguoiNhan,

                    ThoiGianBatDau =
                        thoiGian,

                    ThoiGianKetThuc =
                        null,

                    ThoiLuong =
                        null,

                    TrangThai =
                        "DangGoi",

                    Nonce =
                        nonce,

                    DuLieuXacThuc =
                        duLieuXacThuc,

                    ChuKyNguoiGoi =
                        chuKy,

                    ChuKyNguoiNhan =
                        null,

                    TrangThaiXacThuc =
                        "DaXacThuc"
                };

                _context.CuocGoi.Add(cuocGoi);

                // -----------------------------------------------------
                // Ghi log
                // -----------------------------------------------------

                await GhiLog(
                    maNguoiGoi,
                    "TaoCuocGoi",
                    $"Gọi tài khoản {dto.MaNguoiNhan}",
                    "ThanhCong"
                );

                await _context.SaveChangesAsync();

                // -----------------------------------------------------
                // Trả kết quả
                // -----------------------------------------------------

                return Ok(new
                {
                    ThongBao =
                        "Tạo cuộc gọi thành công!",

                    MaCuocGoi =
                        cuocGoi.MaCuocGoi,

                    MaNguoiGoi =
                        cuocGoi.MaNguoiGoi,

                    MaNguoiNhan =
                        cuocGoi.MaNguoiNhan,

                    TrangThai =
                        cuocGoi.TrangThai,

                    TrangThaiXacThuc =
                        cuocGoi.TrangThaiXacThuc,

                    Nonce =
                        cuocGoi.Nonce,

                    ChuKyRSA =
                        cuocGoi.ChuKyNguoiGoi
                });
            }
            catch (CryptographicException)
            {
                return BadRequest(new
                {
                    ThongBao =
                        "Private Key RSA không hợp lệ!"
                });
            }
            catch (FormatException)
            {
                return BadRequest(new
                {
                    ThongBao =
                        "Private Key RSA không đúng định dạng Base64!"
                });
            }
            catch
            {
                return StatusCode(500, new
                {
                    ThongBao =
                        "Lỗi khi tạo cuộc gọi!"
                });
            }
        }

        // =========================================================
        // 2. LẤY DANH SÁCH CUỘC GỌI
        // GET: /api/CuocGoi
        // =========================================================

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> LayDanhSachCuocGoi()
        {
            try
            {
                if (!LayMaTaiKhoan(
                    out int maTaiKhoan))
                {
                    return Unauthorized(new
                    {
                        ThongBao =
                            "Không xác định được tài khoản!"
                    });
                }

                var danhSach =
                    await _context.CuocGoi
                        .Include(x => x.NguoiGoi)
                        .Include(x => x.NguoiNhan)
                        .Where(x =>
                            x.MaNguoiGoi ==
                                maTaiKhoan
                            ||
                            x.MaNguoiNhan ==
                                maTaiKhoan)
                        .OrderByDescending(
                            x => x.MaCuocGoi
                        )
                        .Select(x => new
                        {
                            x.MaCuocGoi,

                            MaNguoiGoi =
                                x.MaNguoiGoi,

                            TenNguoiGoi =
                                x.NguoiGoi != null
                                    ? x.NguoiGoi.TenDangNhap
                                    : null,

                            HoTenNguoiGoi =
                                x.NguoiGoi != null
                                    ? x.NguoiGoi.HoTen
                                    : null,

                            MaNguoiNhan =
                                x.MaNguoiNhan,

                            TenNguoiNhan =
                                x.NguoiNhan != null
                                    ? x.NguoiNhan.TenDangNhap
                                    : null,

                            HoTenNguoiNhan =
                                x.NguoiNhan != null
                                    ? x.NguoiNhan.HoTen
                                    : null,

                            x.ThoiGianBatDau,

                            x.ThoiGianKetThuc,

                            x.ThoiLuong,

                            x.TrangThai,

                            x.TrangThaiXacThuc
                        })
                        .ToListAsync();

                return Ok(danhSach);
            }
            catch
            {
                return StatusCode(500, new
                {
                    ThongBao =
                        "Lỗi khi lấy danh sách cuộc gọi!"
                });
            }
        }

        // =========================================================
        // 3. CHI TIẾT CUỘC GỌI
        // GET: /api/CuocGoi/{maCuocGoi}
        // =========================================================

        [Authorize]
        [HttpGet("{maCuocGoi}")]
        public async Task<IActionResult> LayChiTietCuocGoi(
            int maCuocGoi)
        {
            try
            {
                if (!LayMaTaiKhoan(
                    out int maTaiKhoan))
                {
                    return Unauthorized(new
                    {
                        ThongBao =
                            "Không xác định được tài khoản!"
                    });
                }

                if (maCuocGoi <= 0)
                {
                    return BadRequest(new
                    {
                        ThongBao =
                            "Mã cuộc gọi không hợp lệ!"
                    });
                }

                var cuocGoi =
                    await _context.CuocGoi
                        .Include(x => x.NguoiGoi)
                        .Include(x => x.NguoiNhan)
                        .FirstOrDefaultAsync(
                            x =>
                                x.MaCuocGoi ==
                                    maCuocGoi
                                &&
                                (
                                    x.MaNguoiGoi ==
                                        maTaiKhoan
                                    ||
                                    x.MaNguoiNhan ==
                                        maTaiKhoan
                                )
                        );

                if (cuocGoi == null)
                {
                    return NotFound(new
                    {
                        ThongBao =
                            "Cuộc gọi không tồn tại hoặc bạn không có quyền xem!"
                    });
                }

                return Ok(new
                {
                    cuocGoi.MaCuocGoi,

                    MaNguoiGoi =
                        cuocGoi.MaNguoiGoi,

                    TenNguoiGoi =
                        cuocGoi.NguoiGoi?
                            .TenDangNhap,

                    HoTenNguoiGoi =
                        cuocGoi.NguoiGoi?
                            .HoTen,

                    MaNguoiNhan =
                        cuocGoi.MaNguoiNhan,

                    TenNguoiNhan =
                        cuocGoi.NguoiNhan?
                            .TenDangNhap,

                    HoTenNguoiNhan =
                        cuocGoi.NguoiNhan?
                            .HoTen,

                    cuocGoi.ThoiGianBatDau,

                    cuocGoi.ThoiGianKetThuc,

                    cuocGoi.ThoiLuong,

                    cuocGoi.TrangThai,

                    cuocGoi.Nonce,

                    cuocGoi.DuLieuXacThuc,

                    cuocGoi.ChuKyNguoiGoi,

                    cuocGoi.ChuKyNguoiNhan,

                    cuocGoi.TrangThaiXacThuc
                });
            }
            catch
            {
                return StatusCode(500, new
                {
                    ThongBao =
                        "Lỗi khi lấy chi tiết cuộc gọi!"
                });
            }
        }

        // =========================================================
        // 4. NHẬN CUỘC GỌI
        // PUT: /api/CuocGoi/{maCuocGoi}/NhanCuocGoi
        // =========================================================

        [Authorize]
        [HttpPut("{maCuocGoi}/NhanCuocGoi")]
        public async Task<IActionResult> NhanCuocGoi(
            int maCuocGoi)
        {
            try
            {
                if (!LayMaTaiKhoan(
                    out int maTaiKhoan))
                {
                    return Unauthorized(new
                    {
                        ThongBao =
                            "Không xác định được tài khoản!"
                    });
                }

                var cuocGoi =
                    await _context.CuocGoi
                        .FirstOrDefaultAsync(
                            x =>
                                x.MaCuocGoi ==
                                maCuocGoi
                        );

                if (cuocGoi == null)
                {
                    return NotFound(new
                    {
                        ThongBao =
                            "Cuộc gọi không tồn tại!"
                    });
                }

                if (cuocGoi.MaNguoiNhan !=
                    maTaiKhoan)
                {
                    return Forbid();
                }

                if (cuocGoi.TrangThai !=
                    "DangGoi")
                {
                    return BadRequest(new
                    {
                        ThongBao =
                            "Cuộc gọi không còn ở trạng thái đang gọi!"
                    });
                }

                cuocGoi.TrangThai =
                    "DangNoi";

                /*
                 * Tạm thời đánh dấu người nhận đã nhận.
                 *
                 * Sau này có thể thay bằng
                 * chữ ký RSA thật của người nhận.
                 */

                cuocGoi.ChuKyNguoiNhan =
                    "DaNhan";

                cuocGoi.TrangThaiXacThuc =
                    "DaXacThuc";

                await GhiLog(
                    maTaiKhoan,
                    "NhanCuocGoi",
                    $"Nhận cuộc gọi {maCuocGoi}",
                    "ThanhCong"
                );

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    ThongBao =
                        "Đã nhận cuộc gọi!",

                    MaCuocGoi =
                        cuocGoi.MaCuocGoi,

                    TrangThai =
                        cuocGoi.TrangThai
                });
            }
            catch
            {
                return StatusCode(500, new
                {
                    ThongBao =
                        "Lỗi khi nhận cuộc gọi!"
                });
            }
        }

        // =========================================================
        // 5. TỪ CHỐI CUỘC GỌI
        // PUT: /api/CuocGoi/{maCuocGoi}/TuChoi
        // =========================================================

        [Authorize]
        [HttpPut("{maCuocGoi}/TuChoi")]
        public async Task<IActionResult> TuChoiCuocGoi(
            int maCuocGoi)
        {
            try
            {
                if (!LayMaTaiKhoan(
                    out int maTaiKhoan))
                {
                    return Unauthorized(new
                    {
                        ThongBao =
                            "Không xác định được tài khoản!"
                    });
                }

                var cuocGoi =
                    await _context.CuocGoi
                        .FirstOrDefaultAsync(
                            x =>
                                x.MaCuocGoi ==
                                maCuocGoi
                        );

                if (cuocGoi == null)
                {
                    return NotFound(new
                    {
                        ThongBao =
                            "Cuộc gọi không tồn tại!"
                    });
                }

                if (cuocGoi.MaNguoiNhan !=
                    maTaiKhoan)
                {
                    return Forbid();
                }

                if (cuocGoi.TrangThai !=
                    "DangGoi")
                {
                    return BadRequest(new
                    {
                        ThongBao =
                            "Cuộc gọi không thể từ chối!"
                    });
                }

                DateTime ketThuc =
                    DateTime.UtcNow;

                cuocGoi.TrangThai =
                    "TuChoi";

                cuocGoi.ThoiGianKetThuc =
                    ketThuc;

                cuocGoi.ThoiLuong =
                    0;

                await GhiLog(
                    maTaiKhoan,
                    "TuChoiCuocGoi",
                    $"Từ chối cuộc gọi {maCuocGoi}",
                    "ThanhCong"
                );

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    ThongBao =
                        "Đã từ chối cuộc gọi!",

                    MaCuocGoi =
                        cuocGoi.MaCuocGoi,

                    TrangThai =
                        cuocGoi.TrangThai
                });
            }
            catch
            {
                return StatusCode(500, new
                {
                    ThongBao =
                        "Lỗi khi từ chối cuộc gọi!"
                });
            }
        }

        // =========================================================
        // 6. KẾT THÚC CUỘC GỌI
        // PUT: /api/CuocGoi/{maCuocGoi}/KetThuc
        // =========================================================

        [Authorize]
        [HttpPut("{maCuocGoi}/KetThuc")]
        public async Task<IActionResult> KetThucCuocGoi(
            int maCuocGoi)
        {
            try
            {
                if (!LayMaTaiKhoan(
                    out int maTaiKhoan))
                {
                    return Unauthorized(new
                    {
                        ThongBao =
                            "Không xác định được tài khoản!"
                    });
                }

                var cuocGoi =
                    await _context.CuocGoi
                        .FirstOrDefaultAsync(
                            x =>
                                x.MaCuocGoi ==
                                maCuocGoi
                        );

                if (cuocGoi == null)
                {
                    return NotFound(new
                    {
                        ThongBao =
                            "Cuộc gọi không tồn tại!"
                    });
                }

                // Người gọi hoặc người nhận
                if (
                    cuocGoi.MaNguoiGoi !=
                        maTaiKhoan
                    &&
                    cuocGoi.MaNguoiNhan !=
                        maTaiKhoan
                )
                {
                    return Forbid();
                }

                if (cuocGoi.TrangThai !=
                    "DangNoi")
                {
                    return BadRequest(new
                    {
                        ThongBao =
                            "Cuộc gọi chưa ở trạng thái đang nói!"
                    });
                }

                DateTime ketThuc =
                    DateTime.UtcNow;

                cuocGoi.ThoiGianKetThuc =
                    ketThuc;

                cuocGoi.TrangThai =
                    "DaKetThuc";

                TimeSpan thoiLuong =
                    ketThuc -
                    cuocGoi.ThoiGianBatDau;

                cuocGoi.ThoiLuong =
                    Math.Max(
                        0,
                        (int)thoiLuong.TotalSeconds
                    );

                await GhiLog(
                    maTaiKhoan,
                    "KetThucCuocGoi",
                    $"Kết thúc cuộc gọi {maCuocGoi}",
                    "ThanhCong"
                );

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    ThongBao =
                        "Đã kết thúc cuộc gọi!",

                    MaCuocGoi =
                        cuocGoi.MaCuocGoi,

                    TrangThai =
                        cuocGoi.TrangThai,

                    ThoiLuong =
                        cuocGoi.ThoiLuong
                });
            }
            catch
            {
                return StatusCode(500, new
                {
                    ThongBao =
                        "Lỗi khi kết thúc cuộc gọi!"
                });
            }
        }

        // =========================================================
        // 7. GHI LOG
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
        }
    }
}