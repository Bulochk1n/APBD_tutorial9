using System.Data.Common;
using Microsoft.Data.SqlClient;
using Tutorial9.Model.DTOs;
using Microsoft.Extensions.Configuration;

namespace Tutorial9.Services;

public class WarehouseService : IWarehouseService
{
    
    private readonly IConfiguration _configuration;

    public WarehouseService(IConfiguration configuration)
    {
        _configuration = configuration;
    }


    public async Task<int> AddProductToWarehouse(ProductDTO productDTO)
    {
        if (productDTO == null)
        {
            throw new Exception("Request body is empty");
        }

        if (productDTO.Amount <= 0)
        {
            throw new Exception("Amount of product must be greater than 0");
        }
        await using SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("WarehouseDB"));
        await conn.OpenAsync();
        DbTransaction transaction = await conn.BeginTransactionAsync();
        try
        {

            await using (SqlCommand cmd = new SqlCommand("SELECT 1 FROM PRODUCT WHERE IdProduct = @IdProduct", conn, transaction as SqlTransaction))
            {
                cmd.Parameters.AddWithValue("@IdProduct", productDTO.IdProduct);
                var exists = await cmd.ExecuteScalarAsync();
                if (exists == null)
                {
                    throw new Exception("Product doesn't exist");
                }


            }

            await using (SqlCommand cmd = new SqlCommand("SELECT 1 FROM WAREHOUSE WHERE IdWarehouse = @IdWarehouse", conn, transaction as SqlTransaction))
            {
                cmd.Parameters.AddWithValue("@IdWarehouse", productDTO.IdWarehouse);
                var exists = await cmd.ExecuteScalarAsync();
                if (exists == null)
                {
                    throw new Exception("Warehouse doesn't exist");
                }
            }

            int orderId;
            using (var cmd = new SqlCommand(
                       @"SELECT 1 FROM [ORDER] WHERE IdProduct = @IdProduct
                                                        and Amount = @Amount
                                                        and CreatedAt < @CreatedAt",
                       conn, transaction as SqlTransaction))
            {
                cmd.Parameters.AddWithValue("@IdProduct", productDTO.IdProduct);
                cmd.Parameters.AddWithValue("@Amount", productDTO.Amount);
                cmd.Parameters.AddWithValue("@CreatedAt", productDTO.CreatedAt);
                var exists = await cmd.ExecuteScalarAsync();
                if (exists == null)
                {
                    throw new Exception("Order doesn't exist");
                }
            }

            await using (SqlCommand cmd = new SqlCommand(
                       @"SELECT TOP 1 IdOrder FROM [ORDER] WHERE IdProduct = @IdProduct
                                                        and Amount = @Amount
                                                        and CreatedAt < @CreatedAt
                                                        and IdOrder not in (SELECT IdOrder FROM Product_Warehouse)",
                       conn, transaction as SqlTransaction))
            {
                cmd.Parameters.AddWithValue("@IdProduct", productDTO.IdProduct);
                cmd.Parameters.AddWithValue("@Amount", productDTO.Amount);
                cmd.Parameters.AddWithValue("@CreatedAt", productDTO.CreatedAt);
                var IdOrder = await cmd.ExecuteScalarAsync();
                if (IdOrder == null)
                {
                    throw new Exception("This order has been already completed");
                }

                orderId = (int)IdOrder;
            }

            await using (var cmd = new SqlCommand("UPDATE [ORDER] SET FulfilledAt = @now WHERE IdOrder = @orderId", conn, transaction as SqlTransaction))
            {
                cmd.Parameters.AddWithValue("@orderId", orderId);
                cmd.Parameters.AddWithValue("@now", DateTime.Now);
                await cmd.ExecuteNonQueryAsync();
            }

            // ... внутри вашего try-блока, после UPDATE FulfilledAt
            int newId;
            await using (var cmd = new SqlCommand(@"INSERT INTO Product_Warehouse
                                                       (IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt)
                                                    VALUES
                                                       (@idWarehouse, @idProduct, @orderId, @amount,
                                                        (SELECT Price FROM Product WHERE IdProduct = @idProduct) * @amount,
                                                        @now);
                                                    SELECT CAST(SCOPE_IDENTITY() AS int);
                                                   ", conn, transaction as SqlTransaction))
            {
                cmd.Parameters.AddWithValue("@idWarehouse", productDTO.IdWarehouse);
                cmd.Parameters.AddWithValue("@idProduct", productDTO.IdProduct);
                cmd.Parameters.AddWithValue("@orderId", orderId);
                cmd.Parameters.AddWithValue("@amount", productDTO.Amount);
                cmd.Parameters.AddWithValue("@now", DateTime.UtcNow);

                var result = await cmd.ExecuteScalarAsync();
                newId = (int)result;
            }


            await transaction.CommitAsync();
            return newId;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}