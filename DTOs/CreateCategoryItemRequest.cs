using System.ComponentModel.DataAnnotations;

namespace PharmeasyAPI.DTOs;

public class CreateCategoryItemRequest
{
    [Required]
    public string Image { get; set; } = string.Empty;

    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public decimal Price { get; set; }
}

public class CreateHealthcareDataRequest
{
    [Required]
    public string Image { get; set; } = string.Empty;

    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Dis { get; set; } = string.Empty;
}
