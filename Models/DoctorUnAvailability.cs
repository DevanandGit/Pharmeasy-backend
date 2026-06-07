namespace PharmeasyAPI.Models;

public class DoctorUnAvailability
{
    public int Id { get; set; }
    public int DoctorProfileId { get; set; }
    public Weekday Weekday { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DoctorProfile DoctorProfile { get; set; } = null!;
}
