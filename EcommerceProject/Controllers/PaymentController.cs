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
        [HttpPost("initiate")]
        public IActionResult InitiatePayment([FromBody] PaymentRequest request)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            decimal amount;
            string method;

            using (var cmd = new SqlCommand(@"
        SELECT amount, payment_method 
        FROM Payments 
        WHERE order_id = @OrderId AND payment_status = 'initiated'", conn))
            {
                cmd.Parameters.AddWithValue("@OrderId", request.OrderId);

                using var reader = cmd.ExecuteReader();
                if (!reader.Read())
                    return BadRequest("Invalid payment state");

                amount = reader.GetDecimal(0);
                method = reader.GetString(1);
            }

            string paymentUrl = GeneratePaymentUrl(
                request.OrderId,
                amount,
                method,
                request.ReturnUrl
            );

            return Ok(new { paymentUrl });
        }


        // ✅ PROCESS PAYMENT CALLBACK (After Gateway Redirect)
        [HttpPost("callback")]
        public IActionResult PaymentCallback(PaymentCallbackRequest request)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var tran = conn.BeginTransaction();

            try
            {
                string paymentStatus = request.Success ? "paid" : "failed";
                string orderStatus = request.Success ? "confirmed" : "pending_payment";

                using (var cmd = new SqlCommand(@"
            UPDATE Payments
            SET payment_status = @Status, transaction_id = @Txn
            WHERE order_id = @OrderId", conn, tran))
                {
                    cmd.Parameters.AddWithValue("@OrderId", request.OrderId);
                    cmd.Parameters.AddWithValue("@Status", paymentStatus);
                    cmd.Parameters.AddWithValue("@Txn", request.TransactionId ?? (object)DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = new SqlCommand(@"
            UPDATE Orders
            SET order_status = @OrderStatus, payment_status = @PaymentStatus
            WHERE order_id = @OrderId", conn, tran))
                {
                    cmd.Parameters.AddWithValue("@OrderId", request.OrderId);
                    cmd.Parameters.AddWithValue("@OrderStatus", orderStatus);
                    cmd.Parameters.AddWithValue("@PaymentStatus", paymentStatus);
                    cmd.ExecuteNonQuery();
                }

                tran.Commit();
                return Ok();
            }
            catch
            {
                tran.Rollback();
                throw;
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
            // Convert method to lowercase for consistent comparison
            method = method.ToLower();

            // Generate payment URLs based on the selected method
            return method switch
            {
                // JazzCash Sandbox URL
                "jazzcash" => $"https://sandbox.jazzcash.com.pk/ApplicationAPI/API/2.0/Purchase/DoMWalletTransaction" +
                              $"?orderId={orderId}&amount={amount}&returnUrl={returnUrl}",

                // Easypaisa Sandbox/Test URL
                "easypaisa" => $"https://easypay.easypaisa.com.pk/easypay/Index.jsf" +
                               $"?orderRefNum={orderId}&amount={amount}&postBackURL={returnUrl}",

                // Visa Card Sandbox (test cards)
                "card" => $"https://sandbox.visa.com/test-payment?orderId={orderId}&amount={amount}&returnUrl={returnUrl}",

                // Unsupported payment method
                _ => throw new ArgumentException("Unsupported payment method")
            };
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
}