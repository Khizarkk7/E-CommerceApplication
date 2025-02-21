using EcommerceProject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace EcommerceProject.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderDetailsController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly AppDbContext _context;
        public OrderDetailsController(AppDbContext context, IConfiguration configuration) {
            _context = context;
            _connectionString = configuration.GetConnectionString("DevDB");
        }

        [HttpPost("AddOrderDetails")]
        public async Task<IActionResult> AddOrderDetails([FromBody] OrderDetailRequest orderDetail)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand("AddOrderDetail", connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@order_id", orderDetail.OrderId);
                        cmd.Parameters.AddWithValue("@product_id", orderDetail.ProductId);
                        cmd.Parameters.AddWithValue("@quantity", orderDetail.Quantity);
                        cmd.Parameters.AddWithValue("@price", orderDetail.Price);

                        int rowsAffected = await cmd.ExecuteNonQueryAsync();
                        if (rowsAffected > 0)
                            return Ok(new { message = "Order detail added successfully!" });

                        return BadRequest(new { message = "Failed to add order detail!" });
                    }
                }
            }

            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }

        }

        [HttpPut("UpdateOrderDetail")]
        public async Task<IActionResult> UpdateOrderDetail([FromBody] OrderDetailRequest orderDetail)
        {
            if (orderDetail == null || orderDetail.OrderDetailId <= 0 || orderDetail.Quantity <= 0 || orderDetail.Price <= 0)
            {
                return BadRequest("Invalid input data.");
            }
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync(); // Ensure the connection is open asynchronously

                    using (SqlCommand cmd = new SqlCommand("UpdateOrderDetail", connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Add parameters for stored procedure
                        cmd.Parameters.Add(new SqlParameter("@order_detail_id", SqlDbType.Int) { Value = orderDetail.OrderDetailId });
                        cmd.Parameters.Add(new SqlParameter("@quantity", SqlDbType.Int) { Value = orderDetail.Quantity });
                        cmd.Parameters.Add(new SqlParameter("@price", SqlDbType.Decimal) { Value = orderDetail.Price });

                        // Execute the stored procedure asynchronously
                        await cmd.ExecuteNonQueryAsync();

                        return Ok("Order detail updated successfully.");
                    }
                }
            }
            catch (SqlException ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpDelete("DeleteOrderDetail/{orderDetailId}")]
        public async Task<IActionResult> DeleteOrderDetail(int orderDetailId)
        {
            if (orderDetailId <= 0)
            {
                return BadRequest("Invalid order detail ID.");
            }

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand("DeleteOrderDetail", connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Add parameter for order_detail_id
                        cmd.Parameters.Add(new SqlParameter("@order_detail_id", SqlDbType.Int) { Value = orderDetailId });

                        // Execute the stored procedure asynchronously
                        await cmd.ExecuteNonQueryAsync();

                        return Ok("Order detail deleted successfully.");
                    }
                }
            }
            catch (SqlException ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("GetOrderDetails/{orderId}")]
        public async Task<IActionResult> GetOrderDetails(int orderId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Execute stored procedure to get order details for the given orderId
                    using (SqlCommand cmd = new SqlCommand("GetOrderDetailsByOrderId", connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add(new SqlParameter("@order_id", SqlDbType.Int) { Value = orderId });

                        var reader = await cmd.ExecuteReaderAsync();

                        var orderDetails = new List<OrderDetailRequest>();

                        while (await reader.ReadAsync())
                        {
                            var orderDetail = new OrderDetailRequest
                            {
                                OrderDetailId = reader.GetInt32(0),
                                OrderId = reader.GetInt32(1),
                                ProductId = reader.GetInt32(2),
                                ProductName = reader.GetString(3),
                                Quantity = reader.GetInt32(4),
                                Price = reader.GetDecimal(5)
                            };
                            orderDetails.Add(orderDetail);
                        }

                        if (orderDetails.Count == 0)
                        {
                            return NotFound("No order details found for this order.");
                        }

                        return Ok(orderDetails);
                    }
                }
            }
            catch (SqlException ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }



    }
}
