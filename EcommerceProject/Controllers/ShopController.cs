﻿using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;
using EcommerceProject.Models; 

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
        public async Task<IActionResult> CreateShop([FromBody] ShopRequest shop)
        {
            if (shop == null)
                return BadRequest("Shop details are required.");

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    using (SqlCommand command = new SqlCommand("CreateShop", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        // Add parameters
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
                        return StatusCode(500, new { message = "An error occurred while creating the shop.", error = ex.Message });
                    }
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


        [HttpGet("GetShop/{shopId?}")]
        public async Task<IActionResult> GetShop(int? shopId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand("GetShopDetails", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Pass shop_id parameter if provided
                        if (shopId.HasValue)
                        {
                            cmd.Parameters.AddWithValue("@shop_id", shopId.Value);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@shop_id", DBNull.Value); // Fetch all shops
                        }

                        var reader = await cmd.ExecuteReaderAsync();
                        var shops = new List<ShopRequest>();

                        while (await reader.ReadAsync())
                        {
                            shops.Add(new ShopRequest
                            {
                                ShopId = (int)reader["shop_id"],
                                ShopName = reader["shop_name"]?.ToString(),
                                Description = reader["description"]?.ToString(),
                                ContactInfo = reader["contact_info"]?.ToString(),
                                Logo = reader["logo"]?.ToString(),
                                CreatedAt = (DateTime)reader["created_at"]
                            });
                        }
                        if (shops.Count == 0)
                        {
                            return NotFound("No shops found.");
                        }
                        return Ok(shops);
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


    }



}
