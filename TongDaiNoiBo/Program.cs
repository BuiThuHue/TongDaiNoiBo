using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;
using TongDaiNoiBo.Data;
using TongDaiNoiBo.Security;
using TongDaiNoiBo.Hubs;

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

        // =================================================
        // CHO PHÉP SIGNALR NHẬN JWT TỪ QUERY STRING
        // =================================================
        //
        // JavaScript SignalR Client sẽ kết nối:
        //
        // /callHub?access_token=JWT...
        //
        // SignalR/WebSocket cần đoạn này để [Authorize]
        // trong CallHub nhận được JWT.
        // =================================================

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken =
                    context.Request.Query["access_token"];

                var path =
                    context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/callHub"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
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
// 6. SIGNALR
// =========================================================
//
// SignalR dùng để trao đổi tín hiệu WebRTC:
// - Offer
// - Answer
// - ICE Candidate
// - Thông báo kết thúc cuộc gọi
//
// Âm thanh KHÔNG truyền qua SignalR.
// Âm thanh sau này sẽ truyền bằng WebRTC.
// =========================================================

builder.Services.AddSignalR();


// =========================================================
// 7. SWAGGER
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
// 8. BUILD APPLICATION
// =========================================================

var app = builder.Build();


// =========================================================
// 9. SWAGGER
// =========================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI();
}


// =========================================================
// 10. FILE HTML / CSS / JS
// =========================================================

app.UseDefaultFiles();

app.UseStaticFiles();


// =========================================================
// 11. HTTPS
// =========================================================

app.UseHttpsRedirection();


// =========================================================
// 12. AUTHENTICATION
// =========================================================

app.UseAuthentication();


// =========================================================
// 13. AUTHORIZATION
// =========================================================

app.UseAuthorization();


// =========================================================
// 14. CONTROLLERS
// =========================================================

app.MapControllers();


// =========================================================
// 15. SIGNALR HUB
// =========================================================
//
// JavaScript sẽ kết nối đến:
//
// /callHub
//
// Ví dụ:
// const connection = new signalR.HubConnectionBuilder()
//     .withUrl("/callHub", ...)
//     .build();
//
// =========================================================

app.MapHub<CallHub>("/callHub");


// =========================================================
// 16. CHẠY
// =========================================================

app.Run();