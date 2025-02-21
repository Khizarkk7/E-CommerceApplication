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
    public class OrderController : ControllerBase
    {

        private readonly AppDbContext _context;
        private readonly string _connectionString;

        public OrderController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _connectionString = configuration.GetConnectionString("DevDB");
        }

        [HttpPost("PlaceOrder")]
        public async Task<IActionResult> CreateOrder([FromBody] OrderRequest order)
        {
            if (order == null)
            {
                return BadRequest("invalid order data ");
            }
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var cmd = new SqlCommand("PlaceOrder", connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@customer_name", order.CustomerName);
                        cmd.Parameters.AddWithValue("@customer_email", (object)order.CustomerEmail ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@shop_id", order.ShopId);
                        cmd.Parameters.AddWithValue("@total_amount", order.TotalAmount);

                        SqlParameter outputIdParam = new SqlParameter("@order_id", SqlDbType.Int)
                        {
                            Direction = ParameterDirection.Output
                        };
                        cmd.Parameters.Add(outputIdParam);

                        await cmd.ExecuteNonQueryAsync();

                        int newOrderId = (int)outputIdParam.Value;
                        return Ok(new { OrderId = newOrderId, Message = "Order placed successfully" });
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = "Error placing order", Error = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetOrdersByShop([FromQuery] int shopId)
        {
            List<OrderRequest> orders = new List<OrderRequest>();

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("GetOrdersByShop", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@shop_id", shopId);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                orders.Add(new OrderRequest
                                {
                                    OrderId = reader.GetInt32(0),
                                    CustomerName = reader.GetString(1),
                                    CustomerEmail = reader.IsDBNull(2) ? null : reader.GetString(2),
                                    OrderStatus = reader.GetString(3),
                                    TotalAmount = reader.GetDecimal(4),
                                    ShopId = reader.GetInt32(5),
                                    CreatedAt = reader.GetDateTime(6)
                                });
                            }
                        }
                    }
                }

                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving orders.", error = ex.Message });
            }
        }
        //issues
        [HttpPut("update-order-status/{orderId}")]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, [FromBody] string newStatus)
        {
            if (string.IsNullOrEmpty(newStatus))
            {
                return BadRequest("Order status is required.");
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("UpdateOrderStatus", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@order_id", orderId);
                        cmd.Parameters.AddWithValue("@new_status", newStatus);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return Ok(new { message = "Order status updated successfully!" });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new { message = "Failed to update order status.", error = ex.Message });
            }
        }


    }

}

