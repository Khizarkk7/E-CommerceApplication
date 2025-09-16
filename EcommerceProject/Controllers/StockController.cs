using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;
using EcommerceProject.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace EcommerceProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StockController : ControllerBase
    {
       // private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public StockController(AppDbContext context, IConfiguration configuration)
        {
          // _context = context;
            _configuration = configuration;
            _connectionString = configuration.GetConnectionString("DevDB");
        }
        
        // GET: api/Stock/GetStockByShop/5
        [HttpGet("GetStockByShop/{shopId}")]
        public async Task<IActionResult> GetStockByShop(int shopId)
        {
            try
            {
                var stockList = new List<object>();

                await using (var conn = new SqlConnection(_connectionString))
                {
                    var query = @"
                SELECT 
                    s.stock_id, 
                    s.product_id, 
                    p.product_name, 
                    s.shop_id, 
                    sh.shop_name, 
                    s.quantity, 
                    s.last_updated, 
                    s.created_at, 
                    s.is_deleted
                FROM Stock s
                JOIN Products p ON s.product_id = p.product_id
                JOIN Shops sh ON s.shop_id = sh.shop_id
                WHERE s.shop_id = @shopId AND s.is_deleted = 0";

                    await using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@shopId", shopId);
                        await conn.OpenAsync();

                        await using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                stockList.Add(new
                                {
                                    StockId = Convert.ToInt32(reader["stock_id"]),
                                    ProductId = Convert.ToInt32(reader["product_id"]),
                                    ProductName = reader["product_name"].ToString(),
                                    ShopId = Convert.ToInt32(reader["shop_id"]),
                                    ShopName = reader["shop_name"].ToString(),
                                    Quantity = Convert.ToInt32(reader["quantity"]),
                                    CreatedAt = Convert.ToDateTime(reader["created_at"]),
                                    LastUpdated = Convert.ToDateTime(reader["last_updated"]),
                                    IsDeleted = Convert.ToBoolean(reader["is_deleted"])
                                });
                            }
                        }
                    }
                }

                if (!stockList.Any())
                    return NotFound("No stock found for this shop.");

                return Ok(stockList);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving stock: {ex.Message}");
            }
        }


        // POST: api/Stock/AddQuantity
        [HttpPost("AddQuantity")]
        public async Task<IActionResult> AddQuantity([FromBody] StockRequest stock)
        {
            if (stock == null || stock.Quantity <= 0)
                return BadRequest("Invalid stock quantity.");

            try
            {
                await using (var conn = new SqlConnection(_connectionString))
                {
                    var query = @"
                        UPDATE Stock
                        SET quantity = quantity + @quantity, last_updated = GETDATE()
                        WHERE stock_id = @stockId AND is_deleted = 0";

                    await using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@quantity", stock.Quantity);
                        cmd.Parameters.AddWithValue("@stockId", stock.StockId);

                        await conn.OpenAsync();
                        var rowsAffected = await cmd.ExecuteNonQueryAsync();

                        if (rowsAffected == 0)
                            return NotFound("Stock record not found.");
                    }
                }

                return Ok(new { message = "Quantity added successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error adding quantity: {ex.Message}");
            }
        }

        // POST: api/Stock/ReduceQuantity
        [HttpPost("ReduceQuantity")]
        public async Task<IActionResult> ReduceQuantity([FromBody] StockRequest stock)
        {
            if (stock == null || stock.Quantity <= 0)
                return BadRequest("Invalid stock quantity.");

            try
            {
                await using (var conn = new SqlConnection(_connectionString))
                {
                    var query = @"
                        UPDATE Stock
                        SET quantity = quantity - @quantity, last_updated = GETDATE()
                        WHERE stock_id = @stockId AND quantity >= @quantity AND is_deleted = 0";

                    await using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@quantity", stock.Quantity);
                        cmd.Parameters.AddWithValue("@stockId", stock.StockId);

                        await conn.OpenAsync();
                        var rowsAffected = await cmd.ExecuteNonQueryAsync();

                        if (rowsAffected == 0)
                            return BadRequest("Not enough stock or stock record not found.");
                    }
                }

                return Ok(new { message = "Quantity reduced successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error reducing quantity: {ex.Message}");
            }
        }


    }
}


    