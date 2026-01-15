using Microsoft.EntityFrameworkCore;
using ConcreteMap.Infrastructure.Data;
using ConcreteMap.Infrastructure.Services;
using Scalar.AspNetCore;
using OfficeOpenXml;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Amazon.S3;

Console.OutputEncoding = System.Text.Encoding.UTF8;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();

// Установка лицензии EPPlus глобально
ExcelPackage.License.SetNonCommercialPersonal("Roman");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptionsAction: sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,         // Попробовать 5 раз
                maxRetryDelay: TimeSpan.FromSeconds(30), // Макс. задержка
                errorCodesToAdd: null);   // Список ошибок (оставим по умолчанию)
        }));

builder.Services.AddScoped<ExcelImportService>();
builder.Services.AddScoped<ExcelExportService>();

// Регистрация AuthService
builder.Services.AddScoped<AuthService>();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Настройка JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Key"]))
        };
    });

var s3Config = new AmazonS3Config
{
    ServiceURL = builder.Configuration["S3Settings:ServiceUrl"],
    ForcePathStyle = true
};

builder.Services.AddSingleton<IAmazonS3>(sp =>
    new AmazonS3Client(
        builder.Configuration["S3Settings:AccessKey"],
        builder.Configuration["S3Settings:SecretKey"],
        s3Config
    ));

builder.Services.AddScoped<S3Service>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    // Применяем миграции автоматически (удобно для докера)
    dbContext.Database.Migrate();
    // Создаем админа
    await DbSeeder.SeedUsers(dbContext);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

// ВАЖНО: Порядок имеет значение!
app.UseAuthentication(); // <-- Добавить это
app.UseAuthorization();  // Это уже было, оставить

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

app.Run();