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
                    p.image_url,
                    p.price,
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
                                // API ka base URL create karna
                                var baseUrl = $"{Request.Scheme}://{Request.Host}/";

                                stockList.Add(new
                                {
                                    StockId = Convert.ToInt32(reader["stock_id"]),
                                    ProductId = Convert.ToInt32(reader["product_id"]),
                                    ProductName = reader["product_name"].ToString(),
                                    Price = Convert.ToInt32(reader["price"]),
                                    ShopId = Convert.ToInt32(reader["shop_id"]),
                                    ShopName = reader["shop_name"].ToString(),
                                    Quantity = Convert.ToInt32(reader["quantity"]),
                                    CreatedAt = Convert.ToDateTime(reader["created_at"]),
                                    LastUpdated = Convert.ToDateTime(reader["last_updated"]),
                                    IsDeleted = Convert.ToBoolean(reader["is_deleted"]),
                                    ImageUrl = $"{baseUrl}{reader["image_url"]}" // full absolute path
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
        //[HttpPost("AddQuantity")]
        //public async Task<IActionResult> AddQuantity([FromBody] StockRequest stock)
        //{
        //    if (stock == null || stock.Quantity <= 0)
        //        return BadRequest("Invalid stock quantity.");

        //    try
        //    {
        //        await using (var conn = new SqlConnection(_connectionString))
        //        {
        //            var query = @"
        //                UPDATE Stock
        //                SET quantity = quantity + @quantity, last_updated = GETDATE()
        //                WHERE stock_id = @stockId AND is_deleted = 0";

        //            await using (var cmd = new SqlCommand(query, conn))
        //            {
        //                cmd.Parameters.AddWithValue("@quantity", stock.Quantity);
        //                cmd.Parameters.AddWithValue("@stockId", stock.StockId);

        //                await conn.OpenAsync();
        //                var rowsAffected = await cmd.ExecuteNonQueryAsync();

        //                if (rowsAffected == 0)
        //                    return NotFound("Stock record not found.");
        //            }
        //        }

        //        return Ok(new { message = "Quantity added successfully." });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, $"Error adding quantity: {ex.Message}");
        //    }
        //}



        // POST: api/Stock/ReduceQuantity
        //[HttpPost("ReduceQuantity")]
        //public async Task<IActionResult> ReduceQuantity([FromBody] StockRequest stock)
        //{
        //    if (stock == null || stock.Quantity <= 0)
        //        return BadRequest("Invalid stock quantity.");

        //    try
        //    {
        //        await using (var conn = new SqlConnection(_connectionString))
        //        {
        //            var query = @"
        //                UPDATE Stock
        //                SET quantity = quantity - @quantity, last_updated = GETDATE()
        //                WHERE stock_id = @stockId AND quantity >= @quantity AND is_deleted = 0";

        //            await using (var cmd = new SqlCommand(query, conn))
        //            {
        //                cmd.Parameters.AddWithValue("@quantity", stock.Quantity);
        //                cmd.Parameters.AddWithValue("@stockId", stock.StockId);

        //                await conn.OpenAsync();
        //                var rowsAffected = await cmd.ExecuteNonQueryAsync();

        //                if (rowsAffected == 0)
        //                    return BadRequest("Not enough stock or stock record not found.");
        //            }
        //        }

        //        return Ok(new { message = "Quantity reduced successfully." });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, $"Error reducing quantity: {ex.Message}");
        //    }
        //}





        [HttpPost("AddQuantity")]
        public async Task<IActionResult> AddQuantity([FromBody] StockRequest stock)
        {
            if (stock == null || stock.Quantity <= 0)
                return BadRequest("Invalid stock quantity.");

            try
            {
                await using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    // Get current stock details
                    var getStockQuery = "SELECT quantity, product_id, shop_id FROM Stock WHERE stock_id = @stockId AND is_deleted = 0";
                    int prevQty = 0, productId = 0, shopId = 0;

                    await using (var getCmd = new SqlCommand(getStockQuery, conn))
                    {
                        getCmd.Parameters.AddWithValue("@stockId", stock.StockId);
                        using var reader = await getCmd.ExecuteReaderAsync();
                        if (await reader.ReadAsync())
                        {
                            prevQty = Convert.ToInt32(reader["quantity"]);
                            productId = Convert.ToInt32(reader["product_id"]);
                            shopId = Convert.ToInt32(reader["shop_id"]);
                        }
                        else
                        {
                            return NotFound("Stock record not found.");
                        }
                    }

                    int newQty = prevQty + stock.Quantity;

                    // Update Stock
                    var updateQuery = @"UPDATE Stock SET quantity = @newQty, last_updated = GETDATE()
                                WHERE stock_id = @stockId AND is_deleted = 0";
                    await using (var updateCmd = new SqlCommand(updateQuery, conn))
                    {
                        updateCmd.Parameters.AddWithValue("@newQty", newQty);
                        updateCmd.Parameters.AddWithValue("@stockId", stock.StockId);
                        await updateCmd.ExecuteNonQueryAsync();
                    }

                    // Insert History
                    var historyQuery = @"INSERT INTO StockHistory (stock_id, product_id, shop_id, change_type, quantity_changed, previous_quantity, new_quantity, changed_by)
                                 VALUES (@stockId, @productId, @shopId, 'ADD', @changedQty, @prevQty, @newQty, @changedBy)";
                    await using (var histCmd = new SqlCommand(historyQuery, conn))
                    {
                        histCmd.Parameters.AddWithValue("@stockId", stock.StockId);
                        histCmd.Parameters.AddWithValue("@productId", productId);
                        histCmd.Parameters.AddWithValue("@shopId", shopId);
                        histCmd.Parameters.AddWithValue("@changedQty", stock.Quantity);
                        histCmd.Parameters.AddWithValue("@prevQty", prevQty);
                        histCmd.Parameters.AddWithValue("@newQty", newQty);
                        histCmd.Parameters.AddWithValue("@changedBy", "Admin"); // TODO: user context se lo
                        await histCmd.ExecuteNonQueryAsync();
                    }
                }

                return Ok(new { message = "Quantity added successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error adding quantity: {ex.Message}");
            }
        }




        [HttpPost("ReduceQuantity")]
        public async Task<IActionResult> ReduceQuantity([FromBody] StockRequest stock)
        {
            if (stock == null || stock.Quantity <= 0)
                return BadRequest("Invalid stock quantity.");

            try
            {
                await using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    // Get current stock details
                    var getStockQuery = "SELECT quantity, product_id, shop_id FROM Stock WHERE stock_id = @stockId AND is_deleted = 0";
                    int prevQty = 0, productId = 0, shopId = 0;

                    await using (var getCmd = new SqlCommand(getStockQuery, conn))
                    {
                        getCmd.Parameters.AddWithValue("@stockId", stock.StockId);
                        using var reader = await getCmd.ExecuteReaderAsync();
                        if (await reader.ReadAsync())
                        {
                            prevQty = Convert.ToInt32(reader["quantity"]);
                            productId = Convert.ToInt32(reader["product_id"]);
                            shopId = Convert.ToInt32(reader["shop_id"]);
                        }
                        else
                        {
                            return NotFound("Stock record not found.");
                        }
                    }

                    if (prevQty < stock.Quantity)
                        return BadRequest("Not enough stock available.");

                    int newQty = prevQty - stock.Quantity;

                    // Update Stock
                    var updateQuery = @"UPDATE Stock SET quantity = @newQty, last_updated = GETDATE()
                                WHERE stock_id = @stockId AND is_deleted = 0";
                    await using (var updateCmd = new SqlCommand(updateQuery, conn))
                    {
                        updateCmd.Parameters.AddWithValue("@newQty", newQty);
                        updateCmd.Parameters.AddWithValue("@stockId", stock.StockId);
                        await updateCmd.ExecuteNonQueryAsync();
                    }

                    // Insert History
                    var historyQuery = @"INSERT INTO StockHistory (stock_id, product_id, shop_id, change_type, quantity_changed, previous_quantity, new_quantity, changed_by)
                                 VALUES (@stockId, @productId, @shopId, 'REDUCE', @changedQty, @prevQty, @newQty, @changedBy)";
                    await using (var histCmd = new SqlCommand(historyQuery, conn))
                    {
                        histCmd.Parameters.AddWithValue("@stockId", stock.StockId);
                        histCmd.Parameters.AddWithValue("@productId", productId);
                        histCmd.Parameters.AddWithValue("@shopId", shopId);
                        histCmd.Parameters.AddWithValue("@changedQty", stock.Quantity);
                        histCmd.Parameters.AddWithValue("@prevQty", prevQty);
                        histCmd.Parameters.AddWithValue("@newQty", newQty);
                        histCmd.Parameters.AddWithValue("@changedBy", "Admin"); // TODO: user context se lo
                        await histCmd.ExecuteNonQueryAsync();
                    }
                }

                return Ok(new { message = "Quantity reduced successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error reducing quantity: {ex.Message}");
            }
        }



        [HttpGet("GetStockHistory/{stockId}")]
        public async Task<IActionResult> GetStockHistory(int stockId)
        {
            try
            {
                var historyList = new List<object>();

                await using (var conn = new SqlConnection(_connectionString))
                {
                    var query = @"
                SELECT h.history_id, h.stock_id, h.change_type, h.quantity_changed, 
                       h.previous_quantity, h.new_quantity, h.changed_by, h.changed_at,
                       p.product_name, sh.shop_name
                FROM StockHistory h
                JOIN Products p ON h.product_id = p.product_id
                JOIN Shops sh ON h.shop_id = sh.shop_id
                WHERE h.stock_id = @stockId
                ORDER BY h.changed_at DESC";

                    await using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@stockId", stockId);
                        await conn.OpenAsync();

                        await using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                historyList.Add(new
                                {
                                    HistoryId = reader["history_id"],
                                    StockId = reader["stock_id"],
                                    ChangeType = reader["change_type"].ToString(),
                                    QuantityChanged = reader["quantity_changed"],
                                    PreviousQuantity = reader["previous_quantity"],
                                    NewQuantity = reader["new_quantity"],
                                    ChangedBy = reader["changed_by"].ToString(),
                                    ChangedAt = reader["changed_at"],
                                    ProductName = reader["product_name"].ToString(),
                                    ShopName = reader["shop_name"].ToString()
                                });
                            }
                        }
                    }
                }

                if (!historyList.Any())
                    return NotFound("No history found for this stock.");

                return Ok(historyList);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving stock history: {ex.Message}");
            }
        }



    }
}


    