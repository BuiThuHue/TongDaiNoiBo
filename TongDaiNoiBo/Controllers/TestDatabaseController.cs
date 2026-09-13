using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TongDaiNoiBo.Data;

namespace TongDaiNoiBo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestDatabaseController : ControllerBase
    {
        private readonly TongDaiDbContext _context;

        public TestDatabaseController(TongDaiDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> KiemTraKetNoi()
        {
            try
            {
                bool ketNoi = await _context.Database.CanConnectAsync();

                if (ketNoi)
                {
                    return Ok(new
                    {
                        ThongBao = "Kết nối MySQL thành công!"
                    });
                }

                return BadRequest(new
                {
                    ThongBao = "Không thể kết nối MySQL!"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    ThongBao = "Có lỗi khi kết nối database!",
                    ChiTiet = ex.Message
                });
            }
        }
    }
}