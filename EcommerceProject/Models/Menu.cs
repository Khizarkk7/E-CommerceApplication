namespace EcommerceProject.Models
{
    public class Menu
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Icon { get; set; }
        public string Route { get; set; }
        public int? ParentId { get; set; }
        public int SortOrder { get; set; }
    }
}
