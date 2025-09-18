using System.ComponentModel.DataAnnotations;

namespace ServerlessDemo.FunApp.Models.Entities
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        public int Stock { get; set; }

        public string Status { get; set; } = "Active";

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastModifiedAt { get; set; }
    }
}
