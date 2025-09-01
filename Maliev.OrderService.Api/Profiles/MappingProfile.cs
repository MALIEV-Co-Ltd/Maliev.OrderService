using AutoMapper;
using Maliev.OrderService.Api.Models.DTOs;
using Maliev.OrderService.Data.Models;

namespace Maliev.OrderService.Api.Profiles
{
    /// <summary>
    /// AutoMapper profile for mapping between DTOs and entities.
    /// </summary>
    public class MappingProfile : Profile
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MappingProfile"/> class.
        /// </summary>
        public MappingProfile()
        {
            CreateMap<Category, CategoryDto>();
            CreateMap<CreateCategoryRequest, Category>();
            CreateMap<UpdateCategoryRequest, Category>();

            CreateMap<FileFormat, FileFormatDto>();
            CreateMap<CreateFileFormatRequest, FileFormat>();
            CreateMap<UpdateFileFormatRequest, FileFormat>();

            CreateMap<Order, OrderDto>();
            CreateMap<CreateOrderRequest, Order>();
            CreateMap<UpdateOrderRequest, Order>();

            CreateMap<OrderFile, OrderFileDto>();
            CreateMap<CreateOrderFileRequest, OrderFile>();
            CreateMap<UpdateOrderFileRequest, OrderFile>();

            CreateMap<Process, ProcessDto>();
            CreateMap<CreateProcessRequest, Process>();
            CreateMap<UpdateProcessRequest, Process>();
        }
    }
}