using Newtonsoft.Json;

namespace ServerlessDemo.FunApp.Models.Entities
{
    public class Product
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("price")]
        public decimal Price { get; set; }

        [JsonProperty("stock")]
        public int Stock { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; } = "Active";

        [JsonProperty("createdAt")]
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        [JsonProperty("lastModifiedAt")]
        public DateTimeOffset? LastModifiedAt { get; set; }
    }
}
