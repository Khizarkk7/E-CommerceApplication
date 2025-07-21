using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;
using EcommerceProject.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace YourProjectNamespace.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShopController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly string _connectionString;
        private readonly ILogger<ShopController> _logger;


        public ShopController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _connectionString = configuration.GetConnectionString("DevDB");
        }



        // POST: api/Shop/CreateShop
        [Authorize(Roles = "systemAdmin")]
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
        public async Task<IActionResult> EditShop([FromForm] ShopRequest shop)
        {
            if (shop == null || shop.ShopId <= 0)
            {
                return BadRequest(new { success = false, message = "Valid shop details are required" });
            }

            string newLogoPath = null;
            string oldLogoPath = null;
            string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Handle logo upload if new file provided
                        if (shop.Logo != null && shop.Logo.Length > 0)
                        {
                            try
                            {
                                if (!Directory.Exists(uploadsFolder))
                                {
                                    Directory.CreateDirectory(uploadsFolder);
                                }

                                var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(shop.Logo.FileName)}";
                                newLogoPath = Path.Combine("uploads", uniqueFileName);
                                var fullPath = Path.Combine(uploadsFolder, uniqueFileName);

                                using (var fileStream = new FileStream(fullPath, FileMode.Create))
                                {
                                    await shop.Logo.CopyToAsync(fileStream);
                                }
                            }
                            catch (Exception ex)
                            {
                                return StatusCode(500, new
                                {
                                    success = false,
                                    message = "Failed to upload logo file",
                                    error = ex.Message
                                });
                            }
                        }

                        // Execute stored procedure
                        using (var command = new SqlCommand("EditShop", connection, transaction))
                        {
                            command.CommandType = CommandType.StoredProcedure;

                            command.Parameters.AddWithValue("@shop_id", shop.ShopId);
                            command.Parameters.AddWithValue("@shop_name", shop.ShopName);
                            command.Parameters.AddWithValue("@description",
                                string.IsNullOrWhiteSpace(shop.Description) ? (object)DBNull.Value : shop.Description);
                            command.Parameters.AddWithValue("@contact_info",
                                string.IsNullOrWhiteSpace(shop.ContactInfo) ? (object)DBNull.Value : shop.ContactInfo);
                            command.Parameters.AddWithValue("@logo",
                                string.IsNullOrWhiteSpace(newLogoPath) ? (object)DBNull.Value : newLogoPath);
                            command.Parameters.AddWithValue("@creator_id", shop.CreatorId);

                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                if (reader.Read())
                                {
                                    oldLogoPath = reader.IsDBNull(0) ? null : reader.GetString(0);
                                }
                            }
                        }

                        // Clean up old logo if new one was uploaded
                        if (!string.IsNullOrEmpty(newLogoPath) && !string.IsNullOrEmpty(oldLogoPath))
                        {
                            try
                            {
                                var oldFullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", oldLogoPath);
                                if (System.IO.File.Exists(oldFullPath))
                                {
                                    System.IO.File.Delete(oldFullPath);
                                }
                            }
                            catch (IOException ex)
                            {
                                // Log the error but don't fail the operation
                                _logger.LogWarning(ex, "Failed to delete old logo file: {OldLogoPath}", oldLogoPath);
                            }
                        }

                        await transaction.CommitAsync();
                        return Ok(new { success = true, message = "Shop updated successfully" });
                    }
                    catch (SqlException ex) when (ex.Number == 50000)
                    {
                        await transaction.RollbackAsync();
                        CleanupFile(newLogoPath);
                        return BadRequest(new { success = false, message = ex.Message });
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        CleanupFile(newLogoPath);
                        return StatusCode(500, new
                        {
                            success = false,
                            message = "An error occurred while updating the shop",
                            error = ex.Message
                        });
                    }
                }
            }
        }

        private void CleanupFile(string filePath)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                try
                {
                    var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", filePath);
                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }
                }
                catch (IOException ex)
                {
                    _logger.LogWarning(ex, "Failed to cleanup file: {FilePath}", filePath);
                }
            }
        }




        
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
