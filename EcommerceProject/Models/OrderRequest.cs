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
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }


}
