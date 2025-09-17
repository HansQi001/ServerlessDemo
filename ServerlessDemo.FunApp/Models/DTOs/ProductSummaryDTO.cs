namespace ServerlessDemo.FunApp.Models.DTOs
{
    internal class ProductSummaryDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }
}
