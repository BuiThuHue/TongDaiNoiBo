using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TongDaiNoiBo.Data;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// KẾT NỐI MYSQL
// ==========================================

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Không tìm thấy chuỗi kết nối DefaultConnection."
    );

builder.Services.AddDbContext<TongDaiDbContext>(options =>
    options.UseMySQL(connectionString)
);

// ==========================================
// JWT AUTHENTICATION
// ==========================================

builder.Services.AddAuthentication(
    JwtBearerDefaults.AuthenticationScheme
)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        // Không kiểm tra Issuer
        ValidateIssuer = false,

        // Không kiểm tra Audience
        ValidateAudience = false,

        // Kiểm tra thời hạn Token
        ValidateLifetime = true,

        // Kiểm tra chữ ký Token
        ValidateIssuerSigningKey = true,

        // Khóa bí mật dùng để kiểm tra JWT
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(
                "TongDaiNoiBo_Secure_Key_2026_VeryStrong!"
            )
        )
    };
});

// ==========================================
// AUTHORIZATION
// ==========================================

builder.Services.AddAuthorization();

// ==========================================
// CONTROLLERS
// ==========================================

builder.Services.AddControllers();

// ==========================================
// SWAGGER
// ==========================================

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

// ==========================================
// BUILD APP
// ==========================================

var app = builder.Build();

// ==========================================
// SWAGGER
// ==========================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI();
}

// ==========================================
// HTTPS
// ==========================================

app.UseHttpsRedirection();

// ==========================================
// AUTHENTICATION
// ==========================================

app.UseAuthentication();

// ==========================================
// AUTHORIZATION
// ==========================================

app.UseAuthorization();

// ==========================================
// MAP CONTROLLERS
// ==========================================

app.MapControllers();

// ==========================================
// CHẠY ỨNG DỤNG
// ==========================================

app.Run();