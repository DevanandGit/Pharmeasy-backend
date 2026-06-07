namespace PharmeasyAPI.Models;

public class DoctorProfile
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Image { get; set; } = string.Empty;
    public string Qualification { get; set; } = string.Empty;
    public string Specialisation { get; set; } = string.Empty;
    public int Experience { get; set; }
    public decimal ConsultationFee { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public ICollection<DoctorUnAvailability> Unavailabilities { get; set; } = new List<DoctorUnAvailability>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
