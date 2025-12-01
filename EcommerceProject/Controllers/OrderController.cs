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

        //  CREATE ORDER WITH PENDING STATUS
        [HttpPost("CreateOrder")]
        public IActionResult CreateOrder([FromBody] OrderRequest order)
        {
            if (order == null || order.CartItems == null || !order.CartItems.Any())
                return BadRequest(new { success = false, message = "Invalid order data." });

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlTransaction transaction = conn.BeginTransaction())
                {
                    int orderId = 0;
                    try
                    {
                        // Map order_status and payment_status
                        string orderStatus = order.Payment.Method.ToLower() == "cod" ? "processing" : "pending";
                        string paymentStatus = order.Payment.Method.ToLower() == "cod" ? "pending_cod" : "pending";

                        // 1️⃣ Insert into Orders
                        try
                        {
                            using (SqlCommand cmd = new SqlCommand(@"
                        INSERT INTO Orders (customer_id, shop_id, total_amount, order_status, payment_status, created_at)
                        OUTPUT INSERTED.order_id
                        VALUES (@CustomerId, @ShopId, @TotalAmount, @OrderStatus, @PaymentStatus, GETDATE())", conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@CustomerId", (object)order.Customer?.CustomerId ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@ShopId", order.ShopId);
                                cmd.Parameters.AddWithValue("@TotalAmount", order.CartItems.Sum(i => i.Price * i.Quantity));
                                cmd.Parameters.AddWithValue("@OrderStatus", orderStatus);
                                cmd.Parameters.AddWithValue("@PaymentStatus", paymentStatus);

                                orderId = (int)cmd.ExecuteScalar();
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Error in Orders table: {ex.Message}", ex);
                        }

                        // 2️⃣ Insert OrderDetails
                        foreach (var item in order.CartItems)
                        {
                            try
                            {
                                using (SqlCommand cmd = new SqlCommand(@"
                            INSERT INTO OrderDetails (order_id, product_id, product_name, quantity, price, total)
                            VALUES (@OrderId, @ProductId, @ProductName, @Quantity, @Price, @Total)", conn, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@OrderId", orderId);
                                    cmd.Parameters.AddWithValue("@ProductId", item.ProductId);
                                    cmd.Parameters.AddWithValue("@ProductName", item.Name);
                                    cmd.Parameters.AddWithValue("@Quantity", item.Quantity);
                                    cmd.Parameters.AddWithValue("@Price", item.Price);
                                    cmd.Parameters.AddWithValue("@Total", item.Price * item.Quantity);

                                    cmd.ExecuteNonQuery();
                                }
                            }
                            catch (Exception ex)
                            {
                                throw new Exception($"Error in OrderDetails table for ProductId {item.ProductId}: {ex.Message}", ex);
                            }
                        }

                        // 3️⃣ Insert ShippingDetails
                        try
                        {
                            using (SqlCommand cmd = new SqlCommand(@"
                        INSERT INTO ShippingDetails
                        (order_id, full_name, phone, address, city, province, postal_code, courier_name, tracking_number, shipping_status)
                        VALUES
                        (@OrderId, @FullName, @Phone, @Address, @City, @Province, @PostalCode, NULL, NULL, @ShippingStatus)", conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@OrderId", orderId);
                                cmd.Parameters.AddWithValue("@FullName", order.Customer.FullName);
                                cmd.Parameters.AddWithValue("@Phone", order.Customer.Phone);
                                cmd.Parameters.AddWithValue("@Address", order.Shipping.Address);
                                cmd.Parameters.AddWithValue("@City", order.Shipping.City);
                                cmd.Parameters.AddWithValue("@Province", order.Shipping.Province);
                                cmd.Parameters.AddWithValue("@PostalCode", order.Shipping.PostalCode);
                                cmd.Parameters.AddWithValue("@ShippingStatus", "pending");

                                cmd.ExecuteNonQuery();
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Error in ShippingDetails table: {ex.Message}", ex);
                        }

                        // 4️⃣ Insert Payments
                        try
                        {
                            using (SqlCommand cmd = new SqlCommand(@"
                        INSERT INTO Payments (order_id, payment_method, amount, payment_status, created_at)
                        VALUES (@OrderId, @Method, @Amount, @PaymentStatus, GETDATE())", conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@OrderId", orderId);
                                cmd.Parameters.AddWithValue("@Method", order.Payment.Method);
                                cmd.Parameters.AddWithValue("@Amount", order.CartItems.Sum(i => i.Price * i.Quantity));
                                cmd.Parameters.AddWithValue("@PaymentStatus", paymentStatus);

                                cmd.ExecuteNonQuery();
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Error in Payments table: {ex.Message}", ex);
                        }

                        transaction.Commit();

                        return Ok(new
                        {
                            success = true,
                            message = "Order created successfully",
                            orderId,
                            orderStatus,
                            paymentStatus
                        });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return StatusCode(500, new
                        {
                            success = false,
                            message = "Error creating order",
                            error = ex.Message,
                            inner = ex.InnerException?.Message
                        });
                    }
                }
            }
        }




        //  GET ORDER DETAILS
        [HttpGet("GetOrder/{orderId}")]
        public IActionResult GetOrder(int orderId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    var order = new OrderResponse();

                    // Get Order Basic Info
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT o.order_id, o.shop_id, o.total_amount, o.order_status, o.payment_status, o.created_at,
                               s.full_name, s.phone, s.address, s.city, s.province, s.postal_code, s.shipping_status,
                               p.payment_method, p.amount as payment_amount, p.payment_status as payment_status_detail
                        FROM Orders o
                        LEFT JOIN ShippingDetails s ON o.order_id = s.order_id
                        LEFT JOIN Payments p ON o.order_id = p.order_id
                        WHERE o.order_id = @OrderId", conn))
                    {
                        cmd.Parameters.AddWithValue("@OrderId", orderId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                order.OrderId = reader.GetInt32("order_id");
                                order.ShopId = reader.GetInt32("shop_id");
                                order.TotalAmount = reader.GetDecimal("total_amount");
                                order.OrderStatus = reader.GetString("order_status");
                                order.PaymentStatus = reader.GetString("payment_status");
                                order.CreatedAt = reader.GetDateTime("created_at");

                                order.Customer = new CustomerDto
                                {
                                    FullName = reader.GetString("full_name"),
                                    Phone = reader.GetString("phone")
                                };

                                order.Shipping = new ShippingDto
                                {
                                    Address = reader.GetString("address"),
                                    City = reader.GetString("city"),
                                    Province = reader.GetString("province"),
                                    PostalCode = reader.GetString("postal_code")
                                };

                                order.Payment = new PaymentResponse
                                {
                                    Method = reader.GetString("payment_method"),
                                    Amount = reader.GetDecimal("payment_amount"),
                                    Status = reader.GetString("payment_status_detail")
                                };
                            }
                            else
                            {
                                return NotFound(new { success = false, message = "Order not found" });
                            }
                        }
                    }

                    // Get Order Items
                    order.Items = new List<OrderItemResponse>();
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT product_id, product_name, quantity, price 
                        FROM OrderDetails 
                        WHERE order_id = @OrderId", conn))
                    {
                        cmd.Parameters.AddWithValue("@OrderId", orderId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                order.Items.Add(new OrderItemResponse
                                {
                                    ProductId = reader.GetInt32("product_id"),
                                    Name = reader.GetString("product_name"),
                                    Quantity = reader.GetInt32("quantity"),
                                    Price = reader.GetDecimal("price")
                                });
                            }
                        }
                    }

                    return Ok(new { success = true, order = order });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error retrieving order", error = ex.Message });
            }
        }

        // ✅ UPDATE ORDER STATUS (After Payment Success)
        [HttpPatch("UpdateStatus/{orderId}")]
        public IActionResult UpdateOrderStatus(int orderId, [FromBody] UpdateOrderStatusRequest request)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    SqlTransaction transaction = conn.BeginTransaction();

                    try
                    {
                        // Update Order Status
                        using (SqlCommand cmd = new SqlCommand(@"
                            UPDATE Orders 
                            SET order_status = @OrderStatus, 
                                payment_status = @PaymentStatus,
                                updated_at = GETDATE()
                            WHERE order_id = @OrderId", conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@OrderId", orderId);
                            cmd.Parameters.AddWithValue("@OrderStatus", request.OrderStatus);
                            cmd.Parameters.AddWithValue("@PaymentStatus", request.PaymentStatus);
                            int affected = cmd.ExecuteNonQuery();

                            if (affected == 0)
                            {
                                transaction.Rollback();
                                return NotFound(new { success = false, message = "Order not found" });
                            }
                        }

                        // Update Payment Status
                        using (SqlCommand cmd = new SqlCommand(@"
                            UPDATE Payments 
                            SET payment_status = @PaymentStatus,
                                payment_date = GETDATE(),
                                transaction_id = @TransactionId
                            WHERE order_id = @OrderId", conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@OrderId", orderId);
                            cmd.Parameters.AddWithValue("@PaymentStatus", request.PaymentStatus);
                            cmd.Parameters.AddWithValue("@TransactionId", request.TransactionId ?? (object)DBNull.Value);
                            cmd.ExecuteNonQuery();
                        }

                        // Update Shipping Status if order is confirmed
                        if (request.OrderStatus == "confirmed")
                        {
                            using (SqlCommand cmd = new SqlCommand(@"
                                UPDATE ShippingDetails 
                                SET shipping_status = 'pending'
                                WHERE order_id = @OrderId", conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@OrderId", orderId);
                                cmd.ExecuteNonQuery();
                            }

                            // Update Stock when order is confirmed
                            using (SqlCommand cmd = new SqlCommand(@"
                                UPDATE Stock 
                                SET quantity = quantity - od.quantity
                                FROM OrderDetails od
                                WHERE od.order_id = @OrderId 
                                AND Stock.product_id = od.product_id 
                                AND Stock.shop_id = (SELECT shop_id FROM Orders WHERE order_id = @OrderId)", conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@OrderId", orderId);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                        return Ok(new { success = true, message = "Order status updated successfully" });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error updating order status", error = ex.Message });
            }
        }

        // ✅ CANCEL ORDER
        [HttpPost("CancelOrder/{orderId}")]
        public IActionResult CancelOrder(int orderId, [FromBody] CancelRequest request)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (SqlCommand cmd = new SqlCommand(@"
                        UPDATE Orders 
                        SET order_status = 'cancelled', 
                            payment_status = 'refunded',
                            updated_at = GETDATE()
                        WHERE order_id = @OrderId AND order_status IN ('pending_payment', 'confirmed')", conn))
                    {
                        cmd.Parameters.AddWithValue("@OrderId", orderId);
                        int affected = cmd.ExecuteNonQuery();

                        if (affected == 0)
                            return BadRequest(new { success = false, message = "Order cannot be cancelled" });

                        return Ok(new { success = true, message = "Order cancelled successfully" });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error cancelling order", error = ex.Message });
            }
        }
    }
}
