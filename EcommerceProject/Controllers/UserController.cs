using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceProject.Models;
using System.Data.SqlClient;
using System.Data;


namespace EcommerceProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly string _connectionString;


        public UserController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _connectionString = configuration.GetConnectionString("DevDB");
        }


        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            var users = await _context.Users.ToListAsync();
            return Ok(users);
        }



        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return Ok(user);
        }


        [HttpPost("AddUser")]
        public async Task<IActionResult> AddUser([FromBody] UserRequest user)
        {
            if (user == null)
            {
                return BadRequest("Invalid user data.");
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand("AddUser", conn))
                    {
                        cmd.CommandType = System.Data.CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@username", user.Username);
                        cmd.Parameters.AddWithValue("@email", user.Email);
                        cmd.Parameters.AddWithValue("@password", user.Password);
                        cmd.Parameters.AddWithValue("@role_name", user.Role);
                        cmd.Parameters.AddWithValue("@shop_id", user.ShopId);

                        // Execute the stored procedure
                        await cmd.ExecuteNonQueryAsync();

                        return Ok("User added successfully.");
                    }
                }
            }
            catch (SqlException ex)
            {
                // Handle SQL errors (e.g., unique constraint violation)
                if (ex.Number == 2627) // SQL Server error code for unique constraint violation (duplicate email)
                {
                    return Conflict("This email is already in use. Please use a different email.");
                }

                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // PUT: api/User/UpdateUser/{userId}
        [HttpPut("UpdateUser/{userId}")]
        public IActionResult UpdateUser(int userId, [FromBody] UserRequest user)
        {
            if (user == null || string.IsNullOrEmpty(user.Username) || string.IsNullOrEmpty(user.Password))
            {
                return BadRequest("Username and Password are required.");
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (SqlCommand cmd = new SqlCommand("UpdateUserDetails", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@user_id", userId);
                        cmd.Parameters.AddWithValue("@username", user.Username);
                        cmd.Parameters.AddWithValue("@password", user.Password);

                        cmd.ExecuteNonQuery();
                    }
                }

                return Ok(new { Message = "User details updated successfully." });
            }
            catch (SqlException ex)
            {
                if (ex.Number == 50000) // Error number for RAISERROR in SQL Server
                {
                    return NotFound(new { Message = ex.Message });
                }
                return StatusCode(500, new { Message = "An error occurred while updating user details.", Error = ex.Message });
            }
        }




        [HttpDelete("delete/{userId}")]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    using (SqlCommand command = new SqlCommand("DeleteUser", connection))
                    {
                        command.CommandType = System.Data.CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@user_id", userId);

                        await command.ExecuteNonQueryAsync();
                    }

                    return Ok("User deleted successfully.");
                }
                catch (SqlException ex)
                {
                    // Handle specific error from stored procedure
                    if (ex.Message.Contains("User with the given ID does not exist"))
                    {
                        return NotFound("User not found with the provided ID.");
                    }

                    // Generic error handler
                    return StatusCode(500, "Error occurred while deleting user: " + ex.Message);
                }
                finally
                {
                    await connection.CloseAsync();
                }
            }
        }


    }

}

