using System.ComponentModel.DataAnnotations;

namespace PharmeasyAPI.DTOs;

public class CompleteProfileRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Phone]
    public string Phone { get; set; } = string.Empty;
}
