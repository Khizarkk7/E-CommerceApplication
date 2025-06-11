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

        //[HttpPost("Login")]
        //public async Task<IActionResult> Login([FromBody] LoginRequest request)
        //{
        //    if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
        //    {
        //        return BadRequest("Email and Password are required.");
        //    }

        //    using (SqlConnection conn = new SqlConnection(_connectionString))
        //    {
        //        await conn.OpenAsync();

        //        using (SqlCommand cmd = new SqlCommand("UserLogin", conn))
        //        {
        //            cmd.CommandType = System.Data.CommandType.StoredProcedure;
        //            cmd.Parameters.AddWithValue("@Email", request.Email);
        //            cmd.Parameters.AddWithValue("@Password", request.Password);

        //            using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
        //            {
        //                if (await reader.ReadAsync())
        //                {
        //                    int userId = reader.GetInt32(0);
        //                    string username = reader.GetString(1);
        //                    string role = reader.GetString(2);
        //                    bool shopDeletedFlag = reader.GetBoolean(reader.GetOrdinal("deleted_flag"));

        //                    if (shopDeletedFlag) // BIT values are true/false
        //                    {
        //                        return Unauthorized("This shop is deactivated. Contact support.");
        //                    }

        //                    string token = GenerateJwtToken(userId, username, role);
        //                    return Ok(new { Token = token, Username = username, Role = role });
        //                }

        //            }
        //        }
        //    }

        //    return Unauthorized("Invalid email or password.");
        //}

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Email and Password are required.");
            }

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                using (SqlCommand cmd = new SqlCommand("UserLogin", conn))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Email", request.Email.Trim());
                    cmd.Parameters.AddWithValue("@Password", request.Password.Trim());

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            int userId = reader.GetInt32(0);
                            string username = reader.GetString(1);
                            string role = reader.GetString(2);

                            object shopIdObj = reader["shop_id"];
                            int? shopId = shopIdObj != DBNull.Value ? Convert.ToInt32(shopIdObj) : (int?)null;
                            string shopName = reader["shop_name"] != DBNull.Value ? reader["shop_name"].ToString() : null;

                            bool shopDeletedFlag = reader.GetBoolean(reader.GetOrdinal("deleted_flag"));

                            if (role == "shopAdmin" && shopDeletedFlag)
                            {
                                return Unauthorized("This shop is deactivated. Contact support.");
                            }

                            string token = GenerateJwtToken(userId, username, role);

                            return Ok(new
                            {
                                token,
                                username,
                                role,
                                shopId,
                                shopName

                            });
                        }
                    }
                }
            }

            return Unauthorized("Invalid email or password.");
        }



        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest userRequest)
        {
            if (string.IsNullOrEmpty(userRequest.Username) ||
                string.IsNullOrEmpty(userRequest.Email) ||
                string.IsNullOrEmpty(userRequest.Password) ||
                userRequest.RoleId == null || userRequest.RoleId <= 0)
            {
                return BadRequest("All fields are required.");
            }

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                // Optional Step: Validate roleId exists
                string roleQuery = "SELECT COUNT(*) FROM Roles WHERE role_id = @RoleId";
                SqlCommand roleCmd = new SqlCommand(roleQuery, conn);
                roleCmd.Parameters.AddWithValue("@RoleId", userRequest.RoleId);
                int roleExists = (int)await roleCmd.ExecuteScalarAsync();

                if (roleExists == 0)
                    return BadRequest("Invalid role selected.");

                // Optional Step: Validate shopId exists if provided
                int? shopId = null;
                if (userRequest.ShopId != null && userRequest.ShopId > 0)
                {
                    string shopQuery = "SELECT COUNT(*) FROM Shops WHERE shop_id = @ShopId AND deleted_flag = 0";
                    SqlCommand shopCmd = new SqlCommand(shopQuery, conn);
                    shopCmd.Parameters.AddWithValue("@ShopId", userRequest.ShopId);
                    int shopExists = (int)await shopCmd.ExecuteScalarAsync();

                    if (shopExists == 0)
                        return BadRequest("Invalid shop selected.");

                    shopId = userRequest.ShopId;
                }

                // Insert user
                string insertQuery = @"INSERT INTO Users (username, email, password, role_id, shop_id, created_at, is_active)
                               VALUES (@Username, @Email, @Password, @RoleId, @ShopId, GETDATE(), 0)";
                SqlCommand insertCmd = new SqlCommand(insertQuery, conn);
                insertCmd.Parameters.AddWithValue("@Username", userRequest.Username);
                insertCmd.Parameters.AddWithValue("@Email", userRequest.Email);
                insertCmd.Parameters.AddWithValue("@Password", userRequest.Password);
                insertCmd.Parameters.AddWithValue("@RoleId", userRequest.RoleId);
                insertCmd.Parameters.AddWithValue("@ShopId", (object?)shopId ?? DBNull.Value);

                await insertCmd.ExecuteNonQueryAsync();
                return Ok(new { succeeded = true, message = "User registered successfully." });


            }
        }


        private string GenerateJwtToken(int userId, string username, string role) //Jwt method
        {
            string jwtKey = _configuration["Jwt:Key"];

            if (string.IsNullOrEmpty(jwtKey))
            {
                throw new ArgumentNullException("Jwt:Key is missing in appsettings.json.");
            }

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
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

