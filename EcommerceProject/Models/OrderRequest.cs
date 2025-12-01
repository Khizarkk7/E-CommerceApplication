namespace EcommerceProject.Models
{
    //public class OrderRequest
    //{
    //    public int OrderId { get; set; }   //by default
    //    public string CustomerName { get; set; }

    //    public string CustomerEmail { get; set; }
    //    public string OrderStatus { get; set; } //by default pending

    //    // The ID of the shop where the order is placed
    //    public int ShopId { get; set; }

    //    public decimal TotalAmount { get; set; }
    //    public DateTime CreatedAt { get; set; }
    //}

    public class OrderRequest
    {
        public int ShopId { get; set; }

        public CustomerDto Customer { get; set; }
        public ShippingDto Shipping { get; set; }
        public PaymentDto Payment { get; set; }
        public List<CartItemDto> CartItems { get; set; }
       
    }

    public class CustomerDto
    {
        public int? CustomerId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
    }

    public class ShippingDto
    {
        public string Address { get; set; }
        public string City { get; set; }
        public string Province { get; set; }
        public string PostalCode { get; set; }
    }

    public class PaymentDto
    {
        public string Method { get; set; } // COD, Easypaisa, JazzCash, etc.
    }

    public class CartItemDto
    {
        public string Name { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
    public class CancelRequest
    {
        public string Reason { get; set; }
    }


    // Add these to your existing Models
    public class UpdateOrderStatusRequest
    {
        public string OrderStatus { get; set; }
        public string PaymentStatus { get; set; }
        public string TransactionId { get; set; }
    }

    public class OrderResponse
    {
        public int OrderId { get; set; }
        public int ShopId { get; set; }
        public decimal TotalAmount { get; set; }
        public string OrderStatus { get; set; }
        public string PaymentStatus { get; set; }
        public DateTime CreatedAt { get; set; }
        public CustomerDto Customer { get; set; }
        public ShippingDto Shipping { get; set; }
        public PaymentResponse Payment { get; set; }
        public List<OrderItemResponse> Items { get; set; }
    }

    public class PaymentResponse
    {
        public string Method { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public string TransactionId { get; set; }
        public DateTime? PaymentDate { get; set; }
    }

    public class OrderItemResponse
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Total => Price * Quantity;
    }

    // Update CartItemDto to include Name
    public class CartItem
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string ImageUrl { get; set; }
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
