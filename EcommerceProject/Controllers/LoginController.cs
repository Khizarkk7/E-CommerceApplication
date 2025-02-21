using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using EcommerceProject.Models;

namespace EcommerceProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public LoginController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = configuration.GetConnectionString("DevDB");
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest("Email and Password are required.");
            }

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                using (SqlCommand cmd = new SqlCommand("UserLogin", conn))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Email", request.Email);
                    cmd.Parameters.AddWithValue("@Password", request.Password);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            int userId = reader.GetInt32(0);
                            string username = reader.GetString(1);
                            string role = reader.GetString(2);
                            int shopDeletedFlag = reader.GetInt32(3); // Fetch deleted flag

                            if (shopDeletedFlag == 1)
                            {
                                return Unauthorized("This shop is deactivated. Contact support.");
                            }

                             string token = GenerateJwtToken(userId, username, role);
                            return Ok(new { Token = token, Username = username, Role = role });
                        }
                    }
                }
            }

            return Unauthorized("Invalid email or password.");
        }

        private string GenerateJwtToken(int userId, string username, string role)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim("UserId", userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

