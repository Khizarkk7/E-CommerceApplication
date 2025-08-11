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


// Detect OS
var isWindows = OperatingSystem.IsWindows();

// Load OS-specific config
if (isWindows)
{
    builder.Configuration.AddJsonFile("appsettings.Windows.json", optional: false);
}
else
{
    builder.Configuration.AddJsonFile("appsettings.Mac.json", optional: false);
}

// Add services to the container.
builder.Services.AddControllers();


// Database connection setup
//builder.Services.AddDbContext<AppDbContext>(options =>
//options.UseSqlServer(builder.Configuration.GetConnectionString("DevDB")));


// Connection string choose based on OS
var connectionString = builder.Configuration.GetConnectionString("DevDB");

if (isWindows)
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(connectionString));
}
else
{
    //builder.Services.AddDbContext<AppDbContext>(options =>
      //  options.UseNpgsql(connectionString));
}





var jwtKey = builder.Configuration.GetSection("Jwt")["Key"];
var issuer = builder.Configuration.GetSection("Jwt")["Issuer"];
var audience = builder.Configuration.GetSection("Jwt")["Audience"];

var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = issuer,
            ValidAudience = audience,
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
