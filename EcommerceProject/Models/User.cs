using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcommerceProject.Models;

public partial class User
{
    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public int RoleId { get; set; }

    public int? ShopId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Role Role { get; set; } = null!;

    [Column("is_active")]
    public bool? IsActive { get; set; }

    public virtual ICollection<Shop> Shops { get; set; } = new List<Shop>();
}
