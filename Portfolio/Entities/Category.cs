namespace Portfolio.Entities
{
    public class Category
    {
        public int CategoryId { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public List<Project> Projects { get; set; } = [];

    }
}
