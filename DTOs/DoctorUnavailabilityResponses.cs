namespace PharmeasyAPI.DTOs;

public class DoctorUnavailabilityResponseDto
{
    public int Id { get; set; }
    public int DoctorProfileId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string DoctorEmail { get; set; } = string.Empty;
    public string Weekday { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
