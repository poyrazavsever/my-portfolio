namespace Portfolio.Entities
{
    public class Project
    {

        public int ProjectId { get; set; }

        public string? Title { get; set; }

        public string? Description { get; set; }

        public string? GithubUrl { get; set; }

        public string? CoverImageUrl { get; set; }

        public int CategoryId { get; set; }

        public Category Category { get; set; } = null!;

    }
}
