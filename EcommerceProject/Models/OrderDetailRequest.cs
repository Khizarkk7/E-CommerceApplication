namespace EcommerceProject.Models
{
    public class OrderDetailRequest
    {
        public int OrderDetailId { get; set; }

        public int OrderId { get; set; } // this will not updated,comes from tables

        public int ProductId { get; set; }  // this will not updated,comes from tables

        public int Quantity { get; set; }

        public decimal Price { get; set; }

        
    }
}
