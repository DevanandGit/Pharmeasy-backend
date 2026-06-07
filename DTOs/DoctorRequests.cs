using System.ComponentModel.DataAnnotations;

namespace PharmeasyAPI.DTOs;

public class CreateDoctorRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string Image { get; set; } = string.Empty;
    public string Qualification { get; set; } = string.Empty;
    public string Specialisation { get; set; } = string.Empty;
    public int Experience { get; set; }
    public decimal ConsultationFee { get; set; }
}

public class PatchDoctorRequest
{
    public string? Email { get; set; }
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? Image { get; set; }
    public string? Qualification { get; set; }
    public string? Specialisation { get; set; }
    public int? Experience { get; set; }
    public decimal? ConsultationFee { get; set; }
    public bool? IsActive { get; set; }
}
