using Microsoft.EntityFrameworkCore;
using System;
using EcommerceProject.Models;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using EcommerceProject.Services;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.StaticFiles;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Database connection setup
    builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DevDB")));

// JWT Key (Secure it in environment variables in production)
var key = Encoding.UTF8.GetBytes("MySuperSecretKey");

// JWT Authentication Setup
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            RoleClaimType = ClaimTypes.Role
        };
    });

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddTransient<IEmailService, EmailService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins("http://localhost:4200")  //  Allow Angular App
              .AllowAnyMethod()                      //  Allow GET, POST, PUT, DELETE
              .AllowAnyHeader()                      //  Allow Headers
              .AllowCredentials();                   //  Allow Cookies/Auth
    });
});

//Role-Based Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SystemAdminPolicy", policy => policy.RequireRole("systemAdmin"));
    options.AddPolicy("ShopAdminPolicy", policy => policy.RequireRole("shopAdmin"));
    options.AddPolicy("BrandManagerPolicy", policy => policy.RequireRole("brandManager"));
    options.AddPolicy("ProductManagerPolicy", policy => policy.RequireRole("productManager"));
    options.AddPolicy("StockManagerPolicy", policy => policy.RequireRole("stockManager"));
    options.AddPolicy("ShopEmployeePolicy", policy => policy.RequireRole("shopEmployee"));
});



// swagger  for testing Apis
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseCors("AllowAngularApp");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads")),
    RequestPath = "/uploads"
});


app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
