namespace EcommerceProject.Models
{
    public class ProductRequest
    {
        public string ProductName { get; set; }
        public string Description { get; set; }
        public decimal? Price { get; set; }
        public string ImageUrl { get; set; }
        public int? StockQuantity { get; set; }
        public int ShopId { get; set; }
    }
}
