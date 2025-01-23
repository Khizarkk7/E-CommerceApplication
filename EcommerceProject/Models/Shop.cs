using System;
using System.Collections.Generic;

namespace EcommerceProject.Models;

public partial class Shop
{
    public int ShopId { get; set; }

    public string ShopName { get; set; } = null!;

    public string? Description { get; set; }

    public string? Logo { get; set; }

    public string? ContactInfo { get; set; }

    public int? CreatorId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public bool? DeletedFlag { get; set; }

    public virtual User? Creator { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
