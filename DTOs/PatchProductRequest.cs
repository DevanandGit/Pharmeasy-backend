using System.ComponentModel.DataAnnotations;

namespace PharmeasyAPI.DTOs;

public class PatchProductRequest
{
    public string? Name { get; set; }
    public decimal? Price { get; set; }
    public decimal? DiscountedPrice { get; set; }
    public string? Summary { get; set; }
    public string? Description { get; set; }
    public string? Uses { get; set; }
    public string? SideEffects { get; set; }
    public string? DirectionForUse { get; set; }
    public string? QuickTips { get; set; }
    public string? StorageDisposal { get; set; }
    public string? Dosage { get; set; }
    public string? ModeOfAction { get; set; }
    public string? Image { get; set; }
    public int? CategoryId { get; set; }
}
