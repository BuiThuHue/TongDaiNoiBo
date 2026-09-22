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
        // 1. LẤY MÃ TÀI KHOẢN TỪ JWT
        // =========================================================

        private bool LayMaTaiKhoan(out int maTaiKhoan)
        {
            maTaiKhoan = 0;

            string? maTaiKhoanString =
                User.FindFirst(
                    ClaimTypes.NameIdentifier
                )?.Value;

            return int.TryParse(
                maTaiKhoanString,
                out maTaiKhoan
            );
        }


        // =========================================================
        // 2. GHI LOG
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


        // =========================================================
        // 3. TÌM TÀI KHOẢN NGƯỜI NHẬN
        //
        // Có thể nhận:
        //
        // - Mã tài khoản
        // HOẶC
        // - Số máy: 101, 102, 103...
        // =========================================================

        private async Task<TaiKhoan?> TimTaiKhoanNguoiNhan(
            int giaTriNguoiNhan)
        {
            // -----------------------------------------------------
            // ƯU TIÊN TÌM THEO SỐ MÁY
            // -----------------------------------------------------

            string soMayCanTim =
                giaTriNguoiNhan.ToString();

            var soMay =
                await _context.SoMayNoiBo
                    .FirstOrDefaultAsync(
                        x => x.SoMay == soMayCanTim
                    );

            if (soMay != null)
            {
                return await _context.TaiKhoan
                    .FirstOrDefaultAsync(
                        x =>
                            x.MaTaiKhoan ==
                            soMay.MaTaiKhoan
                    );
            }


            // -----------------------------------------------------
            // NẾU KHÔNG PHẢI SỐ MÁY
            // THÌ TÌM THEO MÃ TÀI KHOẢN
            // -----------------------------------------------------

            return await _context.TaiKhoan
                .FirstOrDefaultAsync(
                    x =>
                        x.MaTaiKhoan ==
                        giaTriNguoiNhan
                );
        }


        // =========================================================
        // 4. TẠO CUỘC GỌI
        //
        // POST:
        // /api/CuocGoi/TaoCuocGoi
        //
        // CƠ CHẾ MỚI:
        //
        // Private Key KHÔNG gửi lên Server.
        //
        // Máy nhân viên:
        //      Private Key
        //          ↓
        //      ký dữ liệu
        //          ↓
        //      ChuKySo
        //
        // Server:
        //      ChuKySo
        //          +
        //      PublicKeyRSA
        //          ↓
        //      Verify
        // =========================================================

        [Authorize(Roles = "NhanVien")]
        [HttpPost("TaoCuocGoi")]
        public async Task<IActionResult> TaoCuocGoi(
            [FromBody] TaoCuocGoiDTO dto)
        {
            try
            {
                // -------------------------------------------------
                // KIỂM TRA DTO
                // -------------------------------------------------

                if (dto == null)
                {
                    return BadRequest(new
                    {
                        ThongBao =
                            "Dữ liệu cuộc gọi không hợp lệ!"
                    });
                }


                if (dto.MaNguoiNhan <= 0)
                {
                    return BadRequest(new
                    {
                        ThongBao =
                            "Mã người nhận hoặc số máy không hợp lệ!"
                    });
                }


                if (dto.ThoiGianTicks <= 0)
                {
                    return BadRequest(new
                    {
                        ThongBao =
                            "Thời gian ký số không hợp lệ!"
                    });
                }


                if (string.IsNullOrWhiteSpace(
                    dto.Nonce))
                {
                    return BadRequest(new
                    {
                        ThongBao =
                            "Nonce không được để trống!"
                    });
                }


                if (string.IsNullOrWhiteSpace(
                    dto.ChuKySo))
                {
                    return BadRequest(new
                    {
                        ThongBao =
                            "Chưa có chữ ký số của người gọi!"
                    });
                }


                // =================================================
                // LẤY NGƯỜI GỌI TỪ JWT
                // =================================================

                if (!LayMaTaiKhoan(
                    out int maNguoiGoi))
                {
                    return Unauthorized(new
                    {
                        ThongBao =
                            "Không xác định được tài khoản đăng nhập!"
                    });
                }


                // =================================================
                // TÌM NGƯỜI GỌI
                // =================================================

                var nguoiGoi =
                    await _context.TaiKhoan
                        .FirstOrDefaultAsync(
                            x =>
                                x.MaTaiKhoan ==
                                maNguoiGoi
                        );


                if (nguoiGoi == null)
                {
                    return Unauthorized(new
                    {
                        ThongBao =
                            "Tài khoản người gọi không tồn tại!"
                    });
                }


                if (nguoiGoi.VaiTro !=
                    "NhanVien")
                {
                    return Forbid();
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


                // =================================================
                // KIỂM TRA PUBLIC KEY CỦA NGƯỜI GỌI
                // =================================================

                if (string.IsNullOrWhiteSpace(
                    nguoiGoi.PublicKeyRSA))
                {
                    return BadRequest(new
                    {
                        ThongBao =
                            "Bạn chưa thiết lập chữ ký số!"
                    });
                }


                // =================================================
                // TÌM NGƯỜI NHẬN
                // =================================================

                var nguoiNhan =
                    await TimTaiKhoanNguoiNhan(
                        dto.MaNguoiNhan
                    );


                if (nguoiNhan == null)
                {
                    return NotFound(new
                    {
                        ThongBao =
                            $"Không tìm thấy tài khoản hoặc số máy {dto.MaNguoiNhan}!"
                    });
                }


                // -------------------------------------------------
                // KHÔNG GỌI CHÍNH MÌNH
                // -------------------------------------------------

                if (maNguoiGoi ==
                    nguoiNhan.MaTaiKhoan)
                {
                    return BadRequest(new
                    {
                        ThongBao =
                            "Không thể gọi cho chính mình!"
                    });
                }


                // -------------------------------------------------
                // CHỈ GỌI NHÂN VIÊN
                // -------------------------------------------------

                if (nguoiNhan.VaiTro !=
                    "NhanVien")
                {
                    return BadRequest(new
                    {
                        ThongBao =
                            "Chỉ có thể gọi cho tài khoản nhân viên!"
                    });
                }


                // -------------------------------------------------
                // KIỂM TRA TRẠNG THÁI NGƯỜI NHẬN
                // -------------------------------------------------

                if (nguoiNhan.TrangThai !=
                    "HoatDong")
                {
                    return BadRequest(new
                    {
                        ThongBao =
                            "Tài khoản người nhận đang bị khóa!"
                    });
                }


                // =================================================
                // KIỂM TRA THỜI GIAN CHỮ KÝ
                //
                // Yêu cầu ký không được cũ quá 2 phút.
                // =================================================

                DateTime thoiGianKy;

                try
                {
                    thoiGianKy =
                        new DateTime(
                            dto.ThoiGianTicks,
                            DateTimeKind.Utc
                        );
                }
                catch
                {
                    return BadRequest(new
                    {
                        ThongBao =
                            "Thời gian chữ ký không hợp lệ!"
                    });
                }


                TimeSpan doLech =
                    DateTime.UtcNow -
                    thoiGianKy;


                if (Math.Abs(
                    doLech.TotalMinutes) > 2)
                {
                    await GhiLog(
                        maNguoiGoi,
                        "TaoCuocGoi",
                        "Từ chối yêu cầu do chữ ký đã hết thời hạn.",
                        "ThatBai"
                    );


                    return BadRequest(new
                    {
                        ThongBao =
                            "Chữ ký số đã hết thời hạn. Vui lòng ký lại!"
                    });
                }


                // =================================================
                // KIỂM TRA NONCE
                //
                // Chống Replay Attack
                // =================================================

                bool nonceDaTonTai =
                    await _context.CuocGoi
                        .AnyAsync(
                            x =>
                                x.Nonce ==
                                dto.Nonce
                        );


                if (nonceDaTonTai)
                {
                    await GhiLog(
                        maNguoiGoi,
                        "TaoCuocGoi",
                        "Phát hiện Nonce đã được sử dụng.",
                        "ThatBai"
                    );


                    return BadRequest(new
                    {
                        ThongBao =
                            "Yêu cầu cuộc gọi đã được sử dụng hoặc bị gửi lại!"
                    });
                }


                // =================================================
                // DỰNG LẠI DỮ LIỆU MÀ NHÂN VIÊN ĐÃ KÝ
                //
                // Cấu trúc:
                //
                // MaNguoiGoi
                // |
                // MaNguoiNhan thật
                // |
                // Ticks
                // |
                // Nonce
                //
                // =================================================

                string duLieuXacThuc =
                    $"{maNguoiGoi}|" +
                    $"{nguoiNhan.MaTaiKhoan}|" +
                    $"{dto.ThoiGianTicks}|" +
                    $"{dto.Nonce}";


                // =================================================
                // SERVER VERIFY CHỮ KÝ
                //
                // Không sử dụng Private Key.
                //
                // Chỉ sử dụng:
                //
                // - dữ liệu
                // - chữ ký
                // - Public Key
                // =================================================

                bool chuKyHopLe =
                    _rsaService.KiemTraChuKy(
                        duLieuXacThuc,
                        dto.ChuKySo,
                        nguoiGoi.PublicKeyRSA
                    );


                if (!chuKyHopLe)
                {
                    await GhiLog(
                        maNguoiGoi,
                        "TaoCuocGoi",
                        "Chữ ký số của người gọi không hợp lệ.",
                        "ThatBai"
                    );


                    return BadRequest(new
                    {
                        ThongBao =
                            "Chữ ký số không hợp lệ. Cuộc gọi bị từ chối!"
                    });
                }


                // =================================================
                // CHỮ KÝ HỢP LỆ
                // → TẠO CUỘC GỌI
                // =================================================

                var cuocGoi =
                    new CuocGoi
                    {
                        MaNguoiGoi =
                            maNguoiGoi,

                        MaNguoiNhan =
                            nguoiNhan.MaTaiKhoan,

                        ThoiGianBatDau =
                            DateTime.UtcNow,

                        ThoiGianKetThuc =
                            null,

                        ThoiLuong =
                            null,

                        TrangThai =
                            "DangGoi",

                        Nonce =
                            dto.Nonce,

                        DuLieuXacThuc =
                            duLieuXacThuc,

                        ChuKyNguoiGoi =
                            dto.ChuKySo,

                        ChuKyNguoiNhan =
                            null,

                        TrangThaiXacThuc =
                            "NguoiGoiDaXacThuc"
                    };


                _context.CuocGoi.Add(
                    cuocGoi
                );


                await _context
                    .SaveChangesAsync();


                // =================================================
                // GHI LOG
                // =================================================

                await GhiLog(
                    maNguoiGoi,
                    "TaoCuocGoi",
                    $"Chữ ký số hợp lệ. Tạo cuộc gọi đến tài khoản {nguoiNhan.MaTaiKhoan}.",
                    "ThanhCong"
                );


                // =================================================
                // TRẢ KẾT QUẢ
                // =================================================

                return Ok(new
                {
                    ThongBao =
                        "Chữ ký số hợp lệ. Đã tạo cuộc gọi!",

                    MaCuocGoi =
                        cuocGoi.MaCuocGoi,

                    MaNguoiGoi =
                        cuocGoi.MaNguoiGoi,

                    MaNguoiNhan =
                        cuocGoi.MaNguoiNhan,

                    SoMayNguoiNhan =
                        dto.MaNguoiNhan,

                    TenNguoiNhan =
                        nguoiNhan.HoTen,

                    TrangThai =
                        cuocGoi.TrangThai,

                    TrangThaiXacThuc =
                        cuocGoi.TrangThaiXacThuc
                });
            }

            catch (FormatException ex)
            {
                return BadRequest(new
                {
                    ThongBao =
                        "Chữ ký số không đúng định dạng Base64!",

                    ChiTietLoi =
                        ex.Message
                });
            }

            catch (CryptographicException ex)
            {
                return BadRequest(new
                {
                    ThongBao =
                        "Không thể kiểm tra chữ ký số RSA!",

                    ChiTietLoi =
                        ex.Message
                });
            }

            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    ThongBao =
                        "Lỗi khi tạo cuộc gọi!",

                    ChiTietLoi =
                        ex.Message,

                    InnerException =
                        ex.InnerException?.Message
                });
            }
        }


        // =========================================================
        // 5. NHẬN CUỘC GỌI
        //
        // PUT:
        // /api/CuocGoi/{maCuocGoi}/NhanCuocGoi
        //
        // CƠ CHẾ MỚI CHO B:
        //
        // Private Key KHÔNG gửi lên Server.
        //
        // Trình duyệt B:
        //      Private Key B
        //          ↓
        //      ký dữ liệu xác nhận
        //          ↓
        //      ChuKySo
        //
        // Server:
        //      ChuKySo + PublicKeyRSA của B
        //          ↓
        //      Verify
        // =========================================================

        [Authorize(Roles = "NhanVien")]
        [HttpPut("{maCuocGoi}/NhanCuocGoi")]
        public async Task<IActionResult> NhanCuocGoi(
            int maCuocGoi,
            [FromBody] NhanCuocGoiDTO dto)
        {
            try
            {
                // -------------------------------------------------
                // KIỂM TRA DTO
                // -------------------------------------------------

                if (dto == null)
                {
                    return BadRequest(new
                    {
                        ThongBao =
                            "Dữ liệu xác nhận cuộc gọi không hợp lệ!"
                    });
                }

                if (dto.ThoiGianTicks <= 0)
                {
                    return BadRequest(new
                    {
                        ThongBao =
                            "Thời gian ký số của người nhận không hợp lệ!"
                    });
                }

                if (string.IsNullOrWhiteSpace(dto.Nonce))
                {
                    return BadRequest(new
                    {
                        ThongBao =
                            "Nonce của người nhận không được để trống!"
                    });
                }

                if (string.IsNullOrWhiteSpace(dto.ChuKySo))
                {
                    return BadRequest(new
                    {
                        ThongBao =
                            "Chưa có chữ ký số của người nhận!"
                    });
                }

                // -------------------------------------------------
                // LẤY B TỪ JWT
                // -------------------------------------------------

                if (!LayMaTaiKhoan(out int maTaiKhoan))
                {
                    return Unauthorized(new
                    {
                        ThongBao =
                            "Không xác định được tài khoản!"
                    });
                }

                // -------------------------------------------------
                // TÌM CUỘC GỌI
                // -------------------------------------------------

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

                // -------------------------------------------------
                // CHỈ ĐÚNG NGƯỜI NHẬN B MỚI ĐƯỢC NHẬN
                // -------------------------------------------------

                if (cuocGoi.MaNguoiNhan != maTaiKhoan)
                {
                    return Forbid();
                }

                // -------------------------------------------------
                // CUỘC GỌI PHẢI ĐANG CHỜ B
                // -------------------------------------------------

                if (cuocGoi.TrangThai != "DangGoi")
                {
                    return BadRequest(new
                    {
                        ThongBao =
                            "Cuộc gọi không còn ở trạng thái đang gọi!"
                    });
                }

                if (cuocGoi.TrangThaiXacThuc !=
                    "NguoiGoiDaXacThuc")
                {
                    return BadRequest(new
                    {
                        ThongBao =
                            "Người gọi chưa được xác thực hoặc cuộc gọi không còn chờ xác thực B!"
                    });
                }

                // -------------------------------------------------
                // TÌM TÀI KHOẢN B
                // -------------------------------------------------

                var nguoiNhan =
                    await _context.TaiKhoan
                        .FirstOrDefaultAsync(
                            x =>
                                x.MaTaiKhoan ==
                                maTaiKhoan
                        );

                if (nguoiNhan == null)
                {
                    return Unauthorized(new
                    {
                        ThongBao =
                            "Tài khoản người nhận không tồn tại!"
                    });
                }

                if (nguoiNhan.VaiTro != "NhanVien")
                {
                    return Forbid();
                }

                if (nguoiNhan.TrangThai != "HoatDong")
                {
                    return BadRequest(new
                    {
                        ThongBao =
                            "Tài khoản người nhận đang bị khóa!"
                    });
                }

                // -------------------------------------------------
                // B PHẢI CÓ PUBLIC KEY
                // -------------------------------------------------

                if (string.IsNullOrWhiteSpace(
                    nguoiNhan.PublicKeyRSA))
                {
                    return BadRequest(new
                    {
                        ThongBao =
                            "Người nhận chưa thiết lập chữ ký số!"
                    });
                }

                // -------------------------------------------------
                // KIỂM TRA THỜI GIAN CHỮ KÝ B
                //
                // Chữ ký chỉ có hiệu lực trong ±2 phút.
                // -------------------------------------------------

                DateTime thoiGianKy;

                try
                {
                    thoiGianKy =
                        new DateTime(
                            dto.ThoiGianTicks,
                            DateTimeKind.Utc
                        );
                }
                catch
                {
                    return BadRequest(new
                    {
                        ThongBao =
                            "Thời gian chữ ký của người nhận không hợp lệ!"
                    });
                }

                TimeSpan doLech =
                    DateTime.UtcNow -
                    thoiGianKy;

                if (Math.Abs(doLech.TotalMinutes) > 2)
                {
                    await GhiLog(
                        maTaiKhoan,
                        "NhanCuocGoi",
                        $"Từ chối xác nhận cuộc gọi {maCuocGoi} vì chữ ký B đã hết thời hạn.",
                        "ThatBai"
                    );

                    return BadRequest(new
                    {
                        ThongBao =
                            "Chữ ký của người nhận đã hết thời hạn. Vui lòng ký lại!"
                    });
                }

                // -------------------------------------------------
                // KIỂM TRA NONCE B
                //
                // Không cho B dùng lại chính Nonce của A.
                // MaCuocGoi + thời gian + Nonce B cũng được đưa vào
                // dữ liệu ký để ràng buộc chữ ký với đúng cuộc gọi.
                // -------------------------------------------------

                if (dto.Nonce == cuocGoi.Nonce)
                {
                    await GhiLog(
                        maTaiKhoan,
                        "NhanCuocGoi",
                        $"Nonce B trùng Nonce A của cuộc gọi {maCuocGoi}.",
                        "ThatBai"
                    );

                    return BadRequest(new
                    {
                        ThongBao =
                            "Nonce xác nhận của người nhận không hợp lệ!"
                    });
                }

                // -------------------------------------------------
                // DỰNG LẠI ĐÚNG DỮ LIỆU B ĐÃ KÝ
                //
                // Cấu trúc:
                //
                // MaCuocGoi|MaNguoiGoi|MaNguoiNhan|Ticks|NonceB
                // -------------------------------------------------

                string duLieuXacThucB =
                    $"{maCuocGoi}|" +
                    $"{cuocGoi.MaNguoiGoi}|" +
                    $"{maTaiKhoan}|" +
                    $"{dto.ThoiGianTicks}|" +
                    $"{dto.Nonce}";

                // -------------------------------------------------
                // SERVER VERIFY CHỮ KÝ B BẰNG PUBLIC KEY B
                //
                // KHÔNG sử dụng Private Key.
                // -------------------------------------------------

                bool chuKyHopLe =
                    _rsaService.KiemTraChuKy(
                        duLieuXacThucB,
                        dto.ChuKySo,
                        nguoiNhan.PublicKeyRSA
                    );

                if (!chuKyHopLe)
                {
                    await GhiLog(
                        maTaiKhoan,
                        "NhanCuocGoi",
                        $"Chữ ký số của người nhận không hợp lệ cho cuộc gọi {maCuocGoi}.",
                        "ThatBai"
                    );

                    return BadRequest(new
                    {
                        ThongBao =
                            "Chữ ký số của người nhận không hợp lệ!"
                    });
                }

                // -------------------------------------------------
                // B ĐÃ XÁC THỰC THÀNH CÔNG
                // -------------------------------------------------

                cuocGoi.ChuKyNguoiNhan =
                    dto.ChuKySo;

                cuocGoi.TrangThaiXacThuc =
                    "DaXacThucCaHai";

                // -------------------------------------------------
                // A + B ĐỀU HỢP LỆ → KẾT NỐI CUỘC GỌI
                // -------------------------------------------------

                cuocGoi.TrangThai =
                    "DangNoi";

                cuocGoi.ThoiGianBatDau =
                    DateTime.UtcNow;

                await _context
                    .SaveChangesAsync();

                // -------------------------------------------------
                // GHI LOG
                // -------------------------------------------------

                await GhiLog(
                    maTaiKhoan,
                    "NhanCuocGoi",
                    $"Chữ ký số B hợp lệ. Cuộc gọi {maCuocGoi} đã xác thực cả A và B.",
                    "ThanhCong"
                );

                return Ok(new
                {
                    ThongBao =
                        "Chữ ký người nhận hợp lệ. A và B đã được xác thực, cuộc gọi đã kết nối!",

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

                    ChuKyNguoiNhan =
                        cuocGoi.ChuKyNguoiNhan
                });
            }
            catch (FormatException ex)
            {
                return BadRequest(new
                {
                    ThongBao =
                        "Chữ ký số của người nhận không đúng định dạng Base64!",

                    ChiTietLoi =
                        ex.Message
                });
            }
            catch (CryptographicException ex)
            {
                return BadRequest(new
                {
                    ThongBao =
                        "Không thể kiểm tra chữ ký số RSA của người nhận!",

                    ChiTietLoi =
                        ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    ThongBao =
                        "Lỗi khi nhận cuộc gọi!",

                    ChiTietLoi =
                        ex.Message,

                    InnerException =
                        ex.InnerException?.Message
                });
            }
        }


        // =========================================================
        // 6. LẤY CUỘC GỌI ĐANG CHỜ CỦA TÔI
        //
        // GET:
        // /api/CuocGoi/CuocGoiDangCho
        // =========================================================

        [Authorize(Roles = "NhanVien")]
        [HttpGet("CuocGoiDangCho")]
        public async Task<IActionResult> LayCuocGoiDangCho()
        {
            try
            {
                if (!LayMaTaiKhoan(out int maTaiKhoan))
                {
                    return Unauthorized(new
                    {
                        ThongBao =
                            "Không xác định được tài khoản đăng nhập!"
                    });
                }

                var cuocGoi =
                    await _context.CuocGoi
                        .Include(x => x.NguoiGoi)
                        .Where(x =>
                            x.MaNguoiNhan == maTaiKhoan
                            &&
                            x.TrangThai == "DangGoi"
                            &&
                            x.TrangThaiXacThuc ==
                                "NguoiGoiDaXacThuc"
                        )
                        .OrderByDescending(x => x.MaCuocGoi)
                        .FirstOrDefaultAsync();

                if (cuocGoi == null)
                {
                    return Ok(new
                    {
                        CoCuocGoi = false,
                        ThongBao =
                            "Không có cuộc gọi đang chờ."
                    });
                }

                var soMayNguoiGoi =
                    await _context.SoMayNoiBo
                        .FirstOrDefaultAsync(x =>
                            x.MaTaiKhoan ==
                            cuocGoi.MaNguoiGoi
                        );

                return Ok(new
                {
                    CoCuocGoi = true,
                    MaCuocGoi =
                        cuocGoi.MaCuocGoi,
                    MaNguoiGoi =
                        cuocGoi.MaNguoiGoi,
                    HoTenNguoiGoi =
                        cuocGoi.NguoiGoi != null
                            ? cuocGoi.NguoiGoi.HoTen
                            : null,
                    TenDangNhapNguoiGoi =
                        cuocGoi.NguoiGoi != null
                            ? cuocGoi.NguoiGoi.TenDangNhap
                            : null,
                    SoMayNguoiGoi =
                        soMayNguoiGoi != null
                            ? soMayNguoiGoi.SoMay
                            : null,
                    ThoiGianBatDau =
                        cuocGoi.ThoiGianBatDau,
                    TrangThai =
                        cuocGoi.TrangThai,
                    TrangThaiXacThuc =
                        cuocGoi.TrangThaiXacThuc
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    ThongBao =
                        "Lỗi khi kiểm tra cuộc gọi đang chờ!",
                    ChiTietLoi =
                        ex.Message,
                    InnerException =
                        ex.InnerException?.Message
                });
            }
        }


        // =========================================================
        // 6. TỪ CHỐI CUỘC GỌI
        //
        // PUT:
        // /api/CuocGoi/{maCuocGoi}/TuChoi
        // =========================================================

        [Authorize(Roles = "NhanVien")]
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


                // Chỉ B được từ chối

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


                cuocGoi.TrangThai =
                    "TuChoi";


                cuocGoi.ThoiGianKetThuc =
                    DateTime.UtcNow;


                cuocGoi.ThoiLuong =
                    0;


                await _context
                    .SaveChangesAsync();


                await GhiLog(
                    maTaiKhoan,
                    "TuChoiCuocGoi",
                    $"Từ chối cuộc gọi {maCuocGoi}",
                    "ThanhCong"
                );


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

            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    ThongBao =
                        "Lỗi khi từ chối cuộc gọi!",

                    ChiTietLoi =
                        ex.Message,

                    InnerException =
                        ex.InnerException?.Message
                });
            }
        }


        // =========================================================
        // 7. KẾT THÚC CUỘC GỌI
        //
        // PUT:
        // /api/CuocGoi/{maCuocGoi}/KetThuc
        // =========================================================

        [Authorize(Roles = "NhanVien")]
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


                // A hoặc B mới được kết thúc

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


                DateTime thoiGianKetThuc =
                    DateTime.UtcNow;


                cuocGoi.ThoiGianKetThuc =
                    thoiGianKetThuc;


                cuocGoi.TrangThai =
                    "DaKetThuc";


                TimeSpan thoiLuong =
                    thoiGianKetThuc -
                    cuocGoi.ThoiGianBatDau;


                cuocGoi.ThoiLuong =
                    Math.Max(
                        0,
                        (int)thoiLuong.TotalSeconds
                    );


                await _context
                    .SaveChangesAsync();


                await GhiLog(
                    maTaiKhoan,
                    "KetThucCuocGoi",
                    $"Kết thúc cuộc gọi {maCuocGoi}",
                    "ThanhCong"
                );


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

            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    ThongBao =
                        "Lỗi khi kết thúc cuộc gọi!",

                    ChiTietLoi =
                        ex.Message,

                    InnerException =
                        ex.InnerException?.Message
                });
            }
        }


        // =========================================================
        // 8. LỊCH SỬ CUỘC GỌI CỦA TÔI
        //
        // GET:
        // /api/CuocGoi/LichSuCuaToi
        // =========================================================

        [Authorize(Roles = "NhanVien")]
        [HttpGet("LichSuCuaToi")]
        public async Task<IActionResult> LayLichSuCuaToi()
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

                        .Include(
                            x => x.NguoiGoi
                        )

                        .Include(
                            x => x.NguoiNhan
                        )

                        .Where(
                            x =>
                                x.MaNguoiGoi ==
                                maTaiKhoan
                                ||
                                x.MaNguoiNhan ==
                                maTaiKhoan
                        )

                        .OrderByDescending(
                            x => x.MaCuocGoi
                        )

                        .Select(
                            x => new
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
                            }
                        )

                        .ToListAsync();


                return Ok(danhSach);
            }

            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    ThongBao =
                        "Lỗi khi lấy lịch sử cuộc gọi!",

                    ChiTietLoi =
                        ex.Message,

                    InnerException =
                        ex.InnerException?.Message
                });
            }
        }


        // =========================================================
        // 9. CHI TIẾT CUỘC GỌI
        //
        // GET:
        // /api/CuocGoi/{maCuocGoi}
        // =========================================================

        [Authorize(Roles = "NhanVien")]
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

                        .Include(
                            x => x.NguoiGoi
                        )

                        .Include(
                            x => x.NguoiNhan
                        )

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
                            "Cuộc gọi không tồn tại hoặc bạn không có quyền xem cuộc gọi này!"
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

            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    ThongBao =
                        "Lỗi khi lấy chi tiết cuộc gọi!",

                    ChiTietLoi =
                        ex.Message,

                    InnerException =
                        ex.InnerException?.Message
                });
            }
        }
    }
}