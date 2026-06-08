using System.ComponentModel.DataAnnotations;

namespace PharmeasyAPI.DTOs;

public class CreateCategoryRequest
{
    [Required]
    public string CategoryName { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
}

public class UpdateCategoryRequest
{
    [Required]
    public string CategoryName { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
}
