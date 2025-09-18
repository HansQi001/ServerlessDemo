using AgileObjects.AgileMapper;
using ServerlessDemo.FunApp.Models.DTOs;
using ServerlessDemo.FunApp.Models.Entities;

namespace ServerlessDemo.FunApp.Models.MappingConfigs
{
    internal interface IMappingConfig
    {
        void Configure(IMapper mapper);
    }

    internal class ProductMappingConfig : IMappingConfig
    {
        public void Configure(IMapper mapper)
        {
            mapper.WhenMapping
                .From<Product>()
                .To<ProductSummaryDTO>()
                .Map(p => p.Source.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"))
                .To(dto => dto.CreatedAt);

            mapper.WhenMapping
                .From<Product>()
                .To<ProductSummaryDTO>()
                .Map(p => p.Source.LastModifiedAt.HasValue
                    ? p.Source.LastModifiedAt.Value.ToString("yyyy-MM-dd HH:mm:ss")
                    : string.Empty)
                .To(dto => dto.LastModifiedAt);
        }
    }
}
