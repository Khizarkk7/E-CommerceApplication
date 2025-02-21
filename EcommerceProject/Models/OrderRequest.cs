namespace EcommerceProject.Models
{
    public class OrderRequest
    {
        public int OrderId { get; set; }   //by default
        public string CustomerName { get; set; }
        
        public string CustomerEmail { get; set; }
        public string OrderStatus { get; set; } //by default pending

        // The ID of the shop where the order is placed
        public int ShopId { get; set; }

        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

}
