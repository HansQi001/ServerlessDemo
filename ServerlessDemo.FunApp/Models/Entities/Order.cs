using System.ComponentModel.DataAnnotations;

namespace ServerlessDemo.FunApp.Models.Entities
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        public string OrderNumber { get; set; } = string.Empty;

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? LastUpdatedDate { get; set; }
    }
}
