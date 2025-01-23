using System;
using System.Collections.Generic;

namespace EcommerceProject.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public string CustomerName { get; set; } = null!;

    public string? CustomerEmail { get; set; }

    public string? OrderStatus { get; set; }

    public decimal TotalAmount { get; set; }

    public int ShopId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual Shop Shop { get; set; } = null!;
}
