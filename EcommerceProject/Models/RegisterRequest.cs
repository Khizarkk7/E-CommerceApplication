namespace EcommerceProject.Models
{
    public class RegisterRequest
    {
         public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public int? RoleId { get; set; } // From dropdown
        public int? ShopId { get; set; } // From dropdown
    }
}
