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

        [HttpPut("EditProduct/{ProductID}")]
        public EditProduct{
            
        }

}




