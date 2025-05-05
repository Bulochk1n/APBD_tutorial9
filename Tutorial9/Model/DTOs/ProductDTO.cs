using System.ComponentModel.DataAnnotations;

namespace Tutorial9.Model.DTOs;

public class ProductDTO
{
    [Required]
    public int IdProduct { get; set; }
    [Required]
    public int IdWarehouse { get; set; }
    [Required]
    public int Amount { get; set; }
    [Required]
    public DateTime CreatedAt { get; set; }
}