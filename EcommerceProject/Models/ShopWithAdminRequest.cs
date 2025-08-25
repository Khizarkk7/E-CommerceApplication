namespace EcommerceProject.Models
{
    public class ShopWithAdminRequest
    {
        // Shop details
        public string ShopName { get; set; }
        public string ContactInfo { get; set; }
        public string Description { get; set; }
        public IFormFile Logo { get; set; }
        public int CreatorId { get; set; }

        // Shop Admin details
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
