using EcommerceProject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.SqlClient;

namespace EcommerceProject.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {

        private readonly AppDbContext _context;
        private readonly string _connectionString;


        public ProductController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _connectionString = configuration.GetConnectionString("DevDB");
        }


        [HttpPost("AddProduct")]
        public async Task<IActionResult> AddProduct([FromForm] ProductRequest product)
        {
            if (product == null)
                return BadRequest(new { error = "Product data is required." });

            try
            {
                string? relativePath = null;

                // --- Handle Image Upload ---
                if (product.ImageUrl is not null && product.ImageUrl.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "products");

                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(product.ImageUrl.FileName)}";
                    var fullPath = Path.Combine(uploadsFolder, uniqueFileName);

                    await using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        await product.ImageUrl.CopyToAsync(stream);
                    }

                    // Save only relative path in DB (best practice)
                    relativePath = Path.Combine("uploads", "products", uniqueFileName).Replace("\\", "/");
                }

                // --- Database Operation ---
                await using (var connection = new SqlConnection(_connectionString))
                await using (var command = new SqlCommand("AddProduct", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("@product_name", product.ProductName);
                    command.Parameters.AddWithValue("@description", string.IsNullOrEmpty(product.Description) ? (object)DBNull.Value : product.Description);
                    command.Parameters.AddWithValue("@price", product.Price ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@image_url", (object?)relativePath ?? DBNull.Value);
                    command.Parameters.AddWithValue("@stock_quantity", product.StockQuantity ?? 0); // now goes into Stock table
                    command.Parameters.AddWithValue("@shop_id", product.ShopId);

                    await connection.OpenAsync();
                    var rowsAffected = await command.ExecuteNonQueryAsync();

                    if (rowsAffected == 0)
                    {
                        return NotFound(new { error = "Shop not found or product could not be added." });
                    }
                }

                return Ok(new { message = "Product added successfully." });
            }
            catch (SqlException ex)
            {
                // SQL-related error
                return StatusCode(500, new { error = "Database error", details = ex.Message });
            }
            catch (Exception ex)
            {
                // General error
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }



[HttpPut("EditProduct/{productId}")]
public async Task<IActionResult> EditProduct(int productId, [FromForm] ProductRequest product)
{
    if (product == null)
        return BadRequest(new { error = "Invalid request: Product details are required." });

    try
    {
        string? relativePath = null;

        // --- Handle New Image (optional) ---
        if (product.ImageUrl is not null && product.ImageUrl.Length > 0)
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "products");

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(product.ImageUrl.FileName)}";
            var fullPath = Path.Combine(uploadsFolder, uniqueFileName);

            await using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await product.ImageUrl.CopyToAsync(stream);
            }

            // Save only relative path in DB
            relativePath = Path.Combine("uploads", "products", uniqueFileName).Replace("\\", "/");
        }

        // --- Database Update ---
        await using (var conn = new SqlConnection(_connectionString))
        await using (var cmd = new SqlCommand("EditProduct", conn))
        {
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@product_id", productId);
            cmd.Parameters.AddWithValue("@product_name", string.IsNullOrEmpty(product.ProductName) ? (object)DBNull.Value : product.ProductName);
            cmd.Parameters.AddWithValue("@description", string.IsNullOrEmpty(product.Description) ? (object)DBNull.Value : product.Description);
            cmd.Parameters.AddWithValue("@price", product.Price ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@image_url", (object?)relativePath ?? DBNull.Value); // only new path if uploaded
            cmd.Parameters.AddWithValue("@stock_quantity", product.StockQuantity.HasValue ? (object)product.StockQuantity.Value : DBNull.Value);

                    await conn.OpenAsync();
            var rowsAffected = await cmd.ExecuteNonQueryAsync();

            if (rowsAffected == 0)
                return NotFound(new { error = "No product found with the given ID." });
        }

        return Ok(new { message = "Product updated successfully." });
    }
    catch (SqlException ex)
    {
        return StatusCode(500, new { error = "Database error", details = ex.Message });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { error = "Internal server error", details = ex.Message });
    }
}




        [HttpGet("GetProductsByShop/{shopId}")]
        public async Task<IActionResult> GetProductsByShop(int shopId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand("GetProductsByShopId", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@shop_id", shopId);

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            var products = new List<object>();

                            while (await reader.ReadAsync())
                            {
                                products.Add(new
                               {
                                 ProductId = Convert.ToInt32(reader["product_id"]),
                                 ProductName = reader["product_name"].ToString(),
                                 Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader["description"].ToString(),
                                 Price = Convert.ToDecimal(reader["price"]),
                                 ImageUrl = reader.IsDBNull(reader.GetOrdinal("image_url"))? null
    :                            $"{Request.Scheme}://{Request.Host}/uploads/products/{Path.GetFileName(reader["image_url"].ToString())}",
                                 StockQuantity = Convert.ToInt32(reader["stock_quantity"]), // Stock now from Stock table
                                 CreatedAt = Convert.ToDateTime(reader["created_at"])
                               });
                            }
                            if (products.Count == 0)
                            {
                                return NotFound("No products found for this shop.");
                            }

                            return Ok(products);
                        }
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



        [HttpDelete("SoftDeleteProduct/{productId}")]
        public async Task<IActionResult> SoftDeleteProduct(int productId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand("SoftDeleteProduct", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@product_id", productId);

                        int rowsAffected = await cmd.ExecuteNonQueryAsync();

                        if (rowsAffected == 0)
                        {
                            return NotFound("Product not found.");
                        }

                        return Ok("Product soft deleted successfully.");
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



        [HttpGet("shops/{slug}/products/search")]
        public IActionResult SearchProducts(
             string slug,
             string q,
             int page = 1,
             int pageSize = 9)
        {
            if (string.IsNullOrWhiteSpace(slug) || string.IsNullOrWhiteSpace(q))
                return BadRequest(new { success = false, message = "Shop and query are required" });

            q = q.Trim();

            try
            {
                using SqlConnection conn = new SqlConnection(_connectionString);
                conn.Open();

                // 1️⃣ Resolve ShopId from Slug
                int shopId;
                using (SqlCommand shopCmd = new SqlCommand(
                    "SELECT shop_id FROM Shops WHERE slug = @Slug AND deleted_flag = 0", conn))
                {
                    shopCmd.Parameters.AddWithValue("@Slug", slug);
                    var result = shopCmd.ExecuteScalar();

                    if (result == null)
                        return NotFound(new { success = false, message = "Shop not found" });

                    shopId = (int)result;
                }

                // 2️⃣ Search Products
                var products = new List<object>();

                using SqlCommand cmd = new SqlCommand(@"
            SELECT 
                p.product_id,
                p.product_name,
                p.description,
                p.price,
                p.image_url,
                s.quantity,
                COUNT(*) OVER() AS TotalCount
            FROM Products p
            INNER JOIN Stock s ON p.product_id = s.product_id
            WHERE 
                p.shop_id = @ShopId
                AND p.is_deleted = 0
                AND (
                    p.product_name LIKE '%' + @Query + '%'
                    OR p.description LIKE '%' + @Query + '%'
                )
            ORDER BY p.product_id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY", conn);

                cmd.Parameters.AddWithValue("@ShopId", shopId);
                cmd.Parameters.AddWithValue("@Query", q);
                cmd.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
                cmd.Parameters.AddWithValue("@PageSize", pageSize);

                int totalCount = 0;

                using SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    totalCount = reader.GetInt32(reader.GetOrdinal("TotalCount"));

                    string imagePath = reader["image_url"] as string;

                    products.Add(new
                    {
                        ProductId = reader.GetInt32("product_id"),
                        Name = reader.GetString("product_name"),
                        Description = reader["description"] as string,
                        Price = reader.GetDecimal("price"),
                        Stock = reader.GetInt32("quantity"),
                        ImageUrl = string.IsNullOrEmpty(imagePath)
                            ? null
                            : $"{Request.Scheme}://{Request.Host}/uploads/products/{Path.GetFileName(imagePath)}"
                    });
                }

                return Ok(new
                {
                    success = true,
                    page,
                    pageSize,
                    totalCount,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                    data = products
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Search failed",
                    error = ex.Message
                });
            }
        }



    }




}




