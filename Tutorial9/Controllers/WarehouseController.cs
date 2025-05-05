using System.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Tutorial9.Model.DTOs;
using Tutorial9.Services;

namespace Tutorial9.Controllers;

[Route("api/[controller]")]
[ApiController]
public class WarehouseController : ControllerBase
{
    private readonly IWarehouseService _warehouseService;
    private readonly string _connectionString;


    public WarehouseController(IConfiguration configuration, IWarehouseService warehouseService)
    {
        _connectionString = configuration.GetConnectionString("WarehouseDB");
        _warehouseService = warehouseService;
    }


    public async Task<IActionResult> AddProductToWarehouse([FromBody] ProductDTO productDTO)
    {

        if (productDTO == null || productDTO.Amount <= 0)
        {
            return BadRequest("Amount of product must be greater than 0");
        }

        try
        {
            var newId = await _warehouseService.AddProductToWarehouse(productDTO);
            return Ok(new {Id = newId});
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
        
        
    }

    [HttpPost("procedure")]
    public async Task<IActionResult> AddProductToWarehouse_Procedure([FromBody] ProductDTO productDTO)
    {
        if (productDTO == null || productDTO.Amount <= 0)
        {
            return BadRequest("Amount of product must be greater than 0");
        }
        
        await using SqlConnection conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        await using SqlCommand command = new SqlCommand("AddProductToWarehouse")
        {
            CommandType = CommandType.StoredProcedure
        };
        
        //@IdProduct INT, @IdWarehouse INT, @Amount INT, @CreatedAt DATETIME

        command.Parameters.AddWithValue("@IdProduct", productDTO.IdProduct);
        command.Parameters.AddWithValue("@IdWarehouse", productDTO.IdWarehouse);
        command.Parameters.AddWithValue("@Amount", productDTO.Amount);
        command.Parameters.AddWithValue("@CreatedAt", productDTO.CreatedAt);

        try
        {
            var reader = await command.ExecuteReaderAsync();
            if (!reader.HasRows)
            {
                return StatusCode(500, "Stored procedure did not successfully completed.");
            }

            await reader.ReadAsync();
            var newId = reader.GetInt32(reader.GetOrdinal("NewId"));
            return Ok(new { Id = newId });
        }
        catch (SqlException e) when (e.Number == 50000)
        {
            return BadRequest(e.Message);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
        


    }
    
    
}