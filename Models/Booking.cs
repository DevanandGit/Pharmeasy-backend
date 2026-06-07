namespace PharmeasyAPI.Models;

public class Booking
{
    public int Id { get; set; }
    public int CustomerProfileId { get; set; }
    public int DoctorProfileId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string TimeSlot { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string PatientNumber { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string PrescriptionUpload { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ModeOfConsult { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public CustomerProfile CustomerProfile { get; set; } = null!;
    public DoctorProfile DoctorProfile { get; set; } = null!;
}
