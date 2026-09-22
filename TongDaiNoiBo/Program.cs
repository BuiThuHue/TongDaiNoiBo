using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;
using TongDaiNoiBo.Data;
using TongDaiNoiBo.Security;

var builder = WebApplication.CreateBuilder(args);

// =========================================================
// 1. KẾT NỐI DATABASE MYSQL
// =========================================================

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Không tìm thấy chuỗi kết nối DefaultConnection!"
    );

builder.Services.AddDbContext<TongDaiDbContext>(options =>
{
    options.UseMySQL(connectionString);
});


// =========================================================
// 2. JWT AUTHENTICATION
// =========================================================

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,

                ValidateLifetime = true,

                ValidateIssuerSigningKey = true,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            "TongDaiNoiBo_Secure_Key_2026_VeryStrong!"
                        )
                    )
            };
    });


// =========================================================
// 3. AUTHORIZATION
// =========================================================

builder.Services.AddAuthorization();


// =========================================================
// 4. RSA SERVICE
// =========================================================

builder.Services.AddScoped<RsaService>();


// =========================================================
// 5. CONTROLLER
// =========================================================

builder.Services.AddControllers();


// =========================================================
// 6. SWAGGER
// =========================================================

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    // -----------------------------------------------------
    // Khai báo JWT Bearer
    // -----------------------------------------------------

    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",

            Type = SecuritySchemeType.Http,

            Scheme = "bearer",

            BearerFormat = "JWT",

            In = ParameterLocation.Header,

            Description =
                "Nhập JWT Token theo dạng: Bearer {token}"
        }
    );


    // -----------------------------------------------------
    // Cho Swagger sử dụng JWT
    // -----------------------------------------------------

    options.AddSecurityRequirement(
        document =>
            new OpenApiSecurityRequirement
            {
                [
                    new OpenApiSecuritySchemeReference(
                        "Bearer",
                        document
                    )
                ] = []
            }
    );
});


// =========================================================
// 7. BUILD APPLICATION
// =========================================================

var app = builder.Build();


// =========================================================
// 8. SWAGGER
// =========================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI();
}


// =========================================================
// 9. FILE HTML / CSS / JS
// =========================================================

app.UseDefaultFiles();

app.UseStaticFiles();


// =========================================================
// 10. HTTPS
// =========================================================

app.UseHttpsRedirection();


// =========================================================
// 11. AUTHENTICATION
// =========================================================

app.UseAuthentication();


// =========================================================
// 12. AUTHORIZATION
// =========================================================

app.UseAuthorization();


// =========================================================
// 13. CONTROLLERS
// =========================================================

app.MapControllers();


// =========================================================
// 14. CHẠY
// =========================================================

app.Run();