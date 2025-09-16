namespace EcommerceProject.Models
{
    public class ProductRequest
    {
        public string? ProductName { get; set; }
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public IFormFile? ImageUrl { get; set; }
        public int? StockQuantity { get; set; }
        public int ShopId { get; set; }
    }




    public class StockRequest
    {
        public int StockId { get; set; }
        public int ProductId { get; set; }
        public int ShopId { get; set; }
        public int Quantity { get; set; }
        public DateTime LastUpdated { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsDeleted { get; set; }
    }
}
