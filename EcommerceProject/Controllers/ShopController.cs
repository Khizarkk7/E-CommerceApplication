using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;
using EcommerceProject.Models;
using Microsoft.EntityFrameworkCore;

namespace YourProjectNamespace.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShopController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly string _connectionString;


        public ShopController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _connectionString = configuration.GetConnectionString("DevDB");
        }

        // POST: api/Shop/CreateShop
        [HttpPost("CreateShop")]
        public async Task<IActionResult> CreateShop([FromForm] ShopRequest shop)
        {
            if (shop == null)
                return BadRequest("Shop details are required.");

            string logoFilePath = null;

            // Upload the logo file
            if (shop.Logo != null && shop.Logo.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(shop.Logo.FileName)}";
                logoFilePath = Path.Combine("uploads", uniqueFileName); // Relative path
                var fullFilePath = Path.Combine(uploadsFolder, uniqueFileName); // Absolute path

                using (var fileStream = new FileStream(fullFilePath, FileMode.Create))
                {
                    await shop.Logo.CopyToAsync(fileStream);
                }
            }

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    using (SqlCommand command = new SqlCommand("CreateShop", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        command.Parameters.AddWithValue("@shop_name", shop.ShopName);
                        command.Parameters.AddWithValue("@description", shop.Description ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@logo", logoFilePath ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@contact_info", shop.ContactInfo ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@creator_id", shop.CreatorId);

                        await command.ExecuteNonQueryAsync();
                    }
                }
                catch (SqlException ex)
                {
                    if (ex.Number == 50000) // Custom error thrown by RAISERROR
                        return BadRequest(new { message = ex.Message });

                    return StatusCode(500, new { message = "An error occurred.", error = ex.Message });
                }
            }

            return Ok(new { message = "Shop created successfully." });
        }



        [HttpPut("EditShop")]
        public async Task<IActionResult> EditShop([FromBody] ShopRequest shop)
        {
            if (shop == null || shop.ShopId <= 0)
                return BadRequest("Valid shop details are required.");

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    using (SqlCommand command = new SqlCommand("EditShop", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        // Add parameters
                        command.Parameters.AddWithValue("@shop_id", shop.ShopId);
                        command.Parameters.AddWithValue("@shop_name", shop.ShopName);
                        command.Parameters.AddWithValue("@description", shop.Description ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@contact_info", shop.ContactInfo ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@creator_id", shop.CreatorId);

                        // Execute the command
                        await command.ExecuteNonQueryAsync();
                    }
                }
                catch (SqlException ex)
                {
                    if (ex.Number == 50000) // Custom error raised by RAISERROR in the stored procedure
                    {
                        return BadRequest(new { message = ex.Message });
                    }
                    else
                    {
                        return StatusCode(500, new { message = "An error occurred while updating the shop.", error = ex.Message });
                    }
                }
            }

            return Ok(new { message = "Shop updated successfully." });
        }

        // Soft delete shop and associated users
        [HttpPut("SoftDeleteShop/{shopId}")]
        public async Task<IActionResult> SoftDeleteShop(int shopId, [FromQuery] int adminId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand("SoftDeleteShop", conn))
                    {
                        cmd.CommandType = System.Data.CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@shop_id", shopId);
                        cmd.Parameters.AddWithValue("@admin_id", adminId);

                        // Execute the stored procedure
                        await cmd.ExecuteNonQueryAsync();

                        return Ok("Shop and associated users marked as inactive successfully.");
                    }
                }
            }
            catch (SqlException ex)
            {
                return StatusCode(500, $"SQL error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpGet("GetShopDetails/{shopId}")]
        public async Task<IActionResult> GetShopDetails(int shopId)
        {
            try
            {
                var shop = await _context.Shops
                    .Where(s => s.ShopId == shopId && s.DeletedFlag == false)
                    .Select(s => new
                    {
                        s.ShopId,
                        s.ShopName,
                        s.Description,
                        s.ContactInfo,
                        s.Logo,
                        s.CreatedAt
                    }).FirstOrDefaultAsync();

                if (shop == null)
                    return NotFound("Shop not found");

                return Ok(shop);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetShops()
        {
            var shops = await _context.Shops
                .Where(s => s.DeletedFlag == false) // or s.DeletedFlag == 0
                .Select(s => new { s.ShopId, s.ShopName })
                .ToListAsync();

            return Ok(shops);
        }

        // GET: api/Shop/GetAllShops
        [HttpGet("GetAllShops")]
        public async Task<IActionResult> GetAllShops()
        {
            try
            {
                var shops = await (from shop in _context.Shops
                                   join user in _context.Users
                                   on shop.CreatorId equals user.UserId
                                   where shop.DeletedFlag == false
                                   select new
                                   {
                                       shop.ShopId,
                                       shop.ShopName,
                                       shop.Description,
                                       shop.ContactInfo,
                                       shop.Logo,
                                       shop.CreatedAt,
                                       CreatorName = user.Username // fetched via join
                                   }).ToListAsync();

                if (shops == null || shops.Count == 0)
                {
                    return NotFound("No active shops found.");
                }

                return Ok(shops);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving shops.", error = ex.Message });
            }
        }





    }



}
