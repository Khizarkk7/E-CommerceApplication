using EcommerceProject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace EcommerceProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MenuController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly string _connectionString;
        public MenuController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _connectionString = configuration.GetConnectionString("DevDB");
        }


        [HttpGet("GetMenusByRole/{roleId}")]
        public async Task<IActionResult> GetMenusByRole(int roleId)
        {
            if (roleId <= 0)
            {
                return BadRequest("Invalid role ID.");
            }

            try
            {
                var menus = new List<Menu>();

                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand("GetMenusByRole", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@RoleId", roleId);

                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            menus.Add(new Menu
                            {
                                Id = Convert.ToInt32(reader["id"]),
                                Title = reader["title"].ToString(),
                                Icon = reader["icon"].ToString(),
                                Route = reader["route"].ToString(),
                                ParentId = reader["parent_id"] != DBNull.Value ? Convert.ToInt32(reader["parent_id"]) : (int?)null,
                                SortOrder = Convert.ToInt32(reader["sort_order"])
                            });
                        }
                    }
                }

                return Ok(menus);
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

    }
}
