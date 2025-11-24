using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace EcommerceProject.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly string _connectionString;

        public PaymentController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DevDB");
        }

        // ✅ INITIATE PAYMENT
        [HttpPost("Initiate")]
        public IActionResult InitiatePayment([FromBody] PaymentRequest request)
        {
            if (request == null)
                return BadRequest(new { success = false, message = "Invalid payment data." });

            try
            {
                // Verify order exists and is pending payment
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    string orderStatus, paymentMethod;
                    decimal amount;

                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT o.order_status, p.payment_method, p.amount
                        FROM Orders o
                        JOIN Payments p ON o.order_id = p.order_id
                        WHERE o.order_id = @OrderId", conn))
                    {
                        cmd.Parameters.AddWithValue("@OrderId", request.OrderId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                orderStatus = reader.GetString("order_status");
                                paymentMethod = reader.GetString("payment_method");
                                amount = reader.GetDecimal("amount");
                            }
                            else
                            {
                                return NotFound(new { success = false, message = "Order not found" });
                            }
                        }
                    }

                    // Validate order status
                    if (orderStatus != "pending_payment")
                    {
                        return BadRequest(new { success = false, message = "Order is not in pending payment status" });
                    }

                    // Generate payment gateway URL based on method
                    string paymentUrl = GeneratePaymentUrl(request.OrderId, amount, paymentMethod, request.ReturnUrl);

                    return Ok(new
                    {
                        success = true,
                        paymentUrl = paymentUrl,
                        orderId = request.OrderId,
                        amount = amount,
                        paymentMethod = paymentMethod
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error initiating payment", error = ex.Message });
            }
        }

        // ✅ PROCESS PAYMENT CALLBACK (After Gateway Redirect)
        [HttpPost("Callback")]
        public IActionResult PaymentCallback([FromBody] PaymentCallbackRequest request)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    SqlTransaction transaction = conn.BeginTransaction();

                    try
                    {
                        // Update payment status
                        using (SqlCommand cmd = new SqlCommand(@"
                            UPDATE Payments 
                            SET payment_status = @PaymentStatus,
                                transaction_id = @TransactionId,
                                payment_date = GETDATE()
                            WHERE order_id = @OrderId", conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@OrderId", request.OrderId);
                            cmd.Parameters.AddWithValue("@PaymentStatus", request.Success ? "paid" : "failed");
                            cmd.Parameters.AddWithValue("@TransactionId", request.TransactionId ?? (object)DBNull.Value);
                            cmd.ExecuteNonQuery();
                        }

                        // Update order status if payment successful
                        if (request.Success)
                        {
                            using (SqlCommand cmd = new SqlCommand(@"
                                UPDATE Orders 
                                SET order_status = 'confirmed',
                                    payment_status = 'paid',
                                    updated_at = GETDATE()
                                WHERE order_id = @OrderId", conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@OrderId", request.OrderId);
                                cmd.ExecuteNonQuery();
                            }

                            // Update shipping status
                            using (SqlCommand cmd = new SqlCommand(@"
                                UPDATE ShippingDetails 
                                SET shipping_status = 'pending'
                                WHERE order_id = @OrderId", conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@OrderId", request.OrderId);
                                cmd.ExecuteNonQuery();
                            }

                            // Update stock
                            using (SqlCommand cmd = new SqlCommand(@"
                                UPDATE Stock 
                                SET quantity = quantity - od.quantity
                                FROM OrderDetails od
                                WHERE od.order_id = @OrderId 
                                AND Stock.product_id = od.product_id 
                                AND Stock.shop_id = (SELECT shop_id FROM Orders WHERE order_id = @OrderId)", conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@OrderId", request.OrderId);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();

                        return Ok(new
                        {
                            success = true,
                            message = request.Success ? "Payment processed successfully" : "Payment failed",
                            orderId = request.OrderId,
                            orderStatus = request.Success ? "confirmed" : "pending_payment"
                        });
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
                return StatusCode(500, new { success = false, message = "Error processing payment callback", error = ex.Message });
            }
        }

        // CHECK PAYMENT STATUS
        [HttpGet("Status/{orderId}")]
        public IActionResult GetPaymentStatus(int orderId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT p.payment_status, p.transaction_id, p.payment_date, o.order_status
                        FROM Payments p
                        JOIN Orders o ON p.order_id = o.order_id
                        WHERE p.order_id = @OrderId", conn))
                    {
                        cmd.Parameters.AddWithValue("@OrderId", orderId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                //  Handle nullable DateTime properly
                                DateTime? paymentDate = null;
                                if (!reader.IsDBNull(reader.GetOrdinal("payment_date")))
                                {
                                    paymentDate = reader.GetDateTime(reader.GetOrdinal("payment_date"));
                                }

                                string transactionId = null;
                                if (!reader.IsDBNull(reader.GetOrdinal("transaction_id")))
                                {
                                    transactionId = reader.GetString(reader.GetOrdinal("transaction_id"));
                                }

                                return Ok(new
                                {
                                    success = true,
                                    paymentStatus = reader.GetString("payment_status"),
                                    transactionId = transactionId,
                                    paymentDate = paymentDate,
                                    orderStatus = reader.GetString("order_status")
                                });
                            }
                            else
                            {
                                return NotFound(new { success = false, message = "Payment not found" });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error retrieving payment status", error = ex.Message });
            }
        }



        private string GeneratePaymentUrl(int orderId, decimal amount, string method, string returnUrl)
        {
            // Generate payment gateway URLs
            return method.ToLower() switch
            {
                "jazzcash" => $"https://sandbox.jazzcash.com.pk/ApplicationAPI/API/2.0/Purchase/DoMWalletTransaction?orderId={orderId}&amount={amount}&returnUrl={returnUrl}",
                "easypaisa" => $"https://easypay.easypaisa.com.pk/easypay/Index.jsf?orderRefNum={orderId}&amount={amount}&postBackURL={returnUrl}",
                "card" => $"/payment/card?orderId={orderId}&amount={amount}",
                _ => throw new ArgumentException("Unsupported payment method")
            };
        }
    }

    public class PaymentRequest
    {
        public int OrderId { get; set; }
        public string ReturnUrl { get; set; }
    }

    public class PaymentCallbackRequest
    {
        public int OrderId { get; set; }
        public bool Success { get; set; }
        public string TransactionId { get; set; }
        public string Message { get; set; }
    }
}