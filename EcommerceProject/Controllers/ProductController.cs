using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;
using EcommerceProject.Models;
using Microsoft.EntityFrameworkCore;

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
        public async Task<IActionResult> AddProduct([FromBody] ProductRequest product)
        {
            if (product == null)
            {
                return BadRequest("Product data is required.");
            }

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand("AddProduct", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        command.Parameters.AddWithValue("@product_name", product.ProductName);
                        command.Parameters.AddWithValue("@description", product.Description ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@price", product.Price);
                        command.Parameters.AddWithValue("@image_url", product.ImageUrl ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@stock_quantity", product.StockQuantity);
                        command.Parameters.AddWithValue("@shop_id", product.ShopId);

                        connection.Open();
                        await command.ExecuteNonQueryAsync();
                    }
                }

                return Ok(new { message = "Product added successfully." });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
        //[HttpPost("UploadImage")]
        //public async Task<IActionResult> UploadImage([FromForm] IFormFile file)
        //{
        //    if (file == null || file.Length == 0)
        //        return BadRequest("Image file is required.");

        //    var filePath = Path.Combine("wwwroot/images", file.FileName);

        //    using (var stream = new FileStream(filePath, FileMode.Create))
        //    {
        //        await file.CopyToAsync(stream);
        //    }

        //    return Ok(new { ImageUrl = $"/images/{file.FileName}" });
        //}

        [HttpPut("EditProduct/{productId}")]
        public async Task<IActionResult> EditProduct(int productId, [FromBody] ProductRequest product)
        {
            if (product == null)
            {
                return BadRequest("Invalid request: Product details are required.");
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand("EditProduct", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@product_id", productId);
                        cmd.Parameters.AddWithValue("@product_name", (object)product.ProductName ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@description", (object)product.Description ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@price", product.Price ?? (object)DBNull.Value);  
                        cmd.Parameters.AddWithValue("@image_url", (object)product.ImageUrl ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@stock_quantity", product.StockQuantity ?? (object)DBNull.Value); 

                        int rowsAffected = await cmd.ExecuteNonQueryAsync();

                        if (rowsAffected == 0)
                        {
                            return NotFound("No product found with the given ID.");
                        }

                        return Ok("Product updated successfully.");
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
                                    ProductId = reader["product_id"],
                                    ProductName = reader["product_name"],
                                    Description = reader["description"],
                                    Price = reader["price"],
                                    ImageUrl = reader["image_url"],
                                    StockQuantity = reader["stock_quantity"],
                                    CreatedAt = reader["created_at"]
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




    }

}




