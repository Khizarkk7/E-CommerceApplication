using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("Stock")]
public class Stock
{
    [Key]
    [Column("stock_id")]
    public int StockId { get; set; }

    [Column("product_id")]
    public int ProductId { get; set; }

    [Column("shop_id")]
    public int ShopId { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("last_updated")]
    public DateTime LastUpdated { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }
}
