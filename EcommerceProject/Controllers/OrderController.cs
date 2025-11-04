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
        public IActionResult PlaceOrder([FromBody] OrderRequest order)
        {
            if (order == null || order.CartItems == null || !order.CartItems.Any())
                return BadRequest("Invalid order data.");

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                SqlTransaction transaction = conn.BeginTransaction();

                try
                {
                    //  Insert Order
                    int orderId;
                    using (SqlCommand cmd = new SqlCommand(@"
                        INSERT INTO Orders (customer_id, shop_id, total_amount, order_status, payment_status, created_at)
                        OUTPUT INSERTED.order_id
                        VALUES (NULL, @ShopId, @TotalAmount, 'pending', 'unpaid', GETDATE())", conn, transaction))
                    {
                        cmd.Parameters.AddWithValue("@ShopId", order.ShopId);
                        cmd.Parameters.AddWithValue("@TotalAmount", order.CartItems.Sum(i => i.Price * i.Quantity));
                        orderId = (int)cmd.ExecuteScalar();
                    }

                    //  Insert Order Details
                    foreach (var item in order.CartItems)
                    {
                        using (SqlCommand cmd = new SqlCommand(@"
                            INSERT INTO OrderDetails (order_id, product_id, quantity, price)
                            VALUES (@OrderId, @ProductId, @Quantity, @Price)", conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@OrderId", orderId);
                            cmd.Parameters.AddWithValue("@ProductId", item.ProductId);
                            cmd.Parameters.AddWithValue("@Quantity", item.Quantity);
                            cmd.Parameters.AddWithValue("@Price", item.Price);
                            cmd.ExecuteNonQuery();
                        }

                        //  Update Stock
                        using (SqlCommand cmd = new SqlCommand(@"
                            UPDATE Stock
                            SET quantity = quantity - @Quantity
                            WHERE product_id = @ProductId AND shop_id = @ShopId", conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@Quantity", item.Quantity);
                            cmd.Parameters.AddWithValue("@ProductId", item.ProductId);
                            cmd.Parameters.AddWithValue("@ShopId", order.ShopId);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    //  Insert Shipping
                    using (SqlCommand cmd = new SqlCommand(@"
                        INSERT INTO ShippingDetails (order_id, full_name, phone, address, city, province, postal_code)
                        VALUES (@OrderId, @FullName, @Phone, @Address, @City, @Province, @PostalCode)", conn, transaction))
                    {
                        cmd.Parameters.AddWithValue("@OrderId", orderId);
                        cmd.Parameters.AddWithValue("@FullName", order.Customer.FullName);
                        cmd.Parameters.AddWithValue("@Phone", order.Customer.Phone);
                        cmd.Parameters.AddWithValue("@Address", order.Shipping.Address);
                        cmd.Parameters.AddWithValue("@City", order.Shipping.City);
                        cmd.Parameters.AddWithValue("@Province", order.Shipping.Province);
                        cmd.Parameters.AddWithValue("@PostalCode", order.Shipping.PostalCode);
                        cmd.ExecuteNonQuery();
                    }

                    //  Insert Payment
                    using (SqlCommand cmd = new SqlCommand(@"
                        INSERT INTO Payments (order_id, payment_method, amount, payment_status)
                        VALUES (@OrderId, @Method, @Amount, 'pending')", conn, transaction))
                    {
                        cmd.Parameters.AddWithValue("@OrderId", orderId);
                        cmd.Parameters.AddWithValue("@Method", order.Payment.Method);
                        cmd.Parameters.AddWithValue("@Amount", order.CartItems.Sum(i => i.Price * i.Quantity));
                        cmd.ExecuteNonQuery();
                    }

                    //  Commit Transaction
                    transaction.Commit();

                    return Ok(new
                    {
                        message = "Order placed successfully",
                        orderId = orderId,
                        paymentStatus = "pending"
                    });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return StatusCode(500, new { message = "Error placing order", error = ex.Message });
                }
            }
        }


    }

}

