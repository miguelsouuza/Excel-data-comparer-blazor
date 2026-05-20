namespace DataComparer.Models
{
    public class ValidationResult
    {
        public List<ValidationIssue> Issues { get; set; } = new();
    }
}
