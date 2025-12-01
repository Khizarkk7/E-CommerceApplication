namespace EcommerceProject.Models
{
    public class OrderDetailRequest
    {
        public int OrderDetailId { get; set; }

        public int OrderId { get; set; }  // Read-only (from DB)

        public int ProductId { get; set; } // Read-only (from DB)

        public string ProductName { get; set; } // NEW

        public int Quantity { get; set; }

        public decimal Price { get; set; }

        public decimal Total => Price * Quantity;  // NEW (auto calculated)
    }
}
