using System.ComponentModel.DataAnnotations;
using PharmeasyAPI.Models;

namespace PharmeasyAPI.DTOs;

public class CreateDoctorUnavailabilityRequest
{
    [Required]
    public Weekday Weekday { get; set; }

    [Required]
    public string StartTime { get; set; } = string.Empty;

    [Required]
    public string EndTime { get; set; } = string.Empty;
}

public class CreateDoctorUnavailabilityAdminRequest : CreateDoctorUnavailabilityRequest
{
    [Required]
    public int DoctorProfileId { get; set; }
}

public class PatchDoctorUnavailabilityRequest
{
    public Weekday? Weekday { get; set; }
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
}
