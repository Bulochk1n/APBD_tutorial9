using Tutorial9.Model.DTOs;

namespace Tutorial9.Services;

public interface IWarehouseService
{
    Task<int> AddProductToWarehouse(ProductDTO productDTO);


}