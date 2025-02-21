using System.ComponentModel.DataAnnotations;

namespace EcommerceProject.Models
{
    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
