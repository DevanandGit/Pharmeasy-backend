using System.ComponentModel.DataAnnotations;

namespace PharmeasyAPI.DTOs;

public class SendOtpRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
