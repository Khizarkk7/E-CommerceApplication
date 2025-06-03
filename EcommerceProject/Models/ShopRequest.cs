namespace EcommerceProject.Models
{
    public class ShopRequest
    {
        public int ShopId { get; set; }
        public string ShopName { get; set; }
        public string Description { get; set; }
        public string ContactInfo { get; set; }
        public int CreatorId { get; set; }
        public IFormFile Logo { get; set; }
        public DateTime CreatedAt { get; set; }
    }   
}
