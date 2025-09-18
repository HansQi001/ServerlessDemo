using System.Text.Json.Serialization;

namespace ServerlessDemo.FunApp.Models.DTOs
{
    internal class ProductIdsRequest
    {
        [JsonPropertyName("ids")]
        public string[] Ids { get; set; }

    }
}
