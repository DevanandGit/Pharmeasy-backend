using System.ComponentModel.DataAnnotations;

namespace PharmeasyAPI.DTOs;

public class AddToCartRequest
{
    [Required]
    public int ProductId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; } = 1;
}

public class RemoveFromCartRequest
{
    [Required]
    public int ProductId { get; set; }

    // If null, remove the item entirely; otherwise reduce by this quantity
    public int? Quantity { get; set; }
}
