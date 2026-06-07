using System.ComponentModel.DataAnnotations;
using PharmeasyAPI.Models;

namespace PharmeasyAPI.DTOs;

public class CreateDoctorUnavailabilityRequest
{
    [Required]
    public Weekday Weekday { get; set; }

    [Required]
    public string Time { get; set; } = string.Empty;
}

public class CreateDoctorUnavailabilityAdminRequest : CreateDoctorUnavailabilityRequest
{
    [Required]
    public int DoctorProfileId { get; set; }
}

public class PatchDoctorUnavailabilityRequest
{
    public Weekday? Weekday { get; set; }
    public string? Time { get; set; }
}
