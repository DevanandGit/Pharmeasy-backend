namespace PharmeasyAPI.DTOs;

public class AuthResponse
{
    public UserDto User { get; set; } = null!;
    public string Token { get; set; } = string.Empty;
}

public class UserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsNew { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ProfileBookingDto
{
    public int Id { get; set; }
    public DateTime AppointmentDate { get; set; }
    public string TimeSlot { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string PatientNumber { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string ModeOfConsult { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string PrescriptionUpload { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public int DoctorProfileId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public int CustomerProfileId { get; set; }
    public string PatientProfileName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class ProfileOrderDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CustomerProfileDto
{
    public int CustomerProfileId { get; set; }
    public string ProfileImage { get; set; } = string.Empty;
    public List<ProfileBookingDto> Bookings { get; set; } = new();
    public List<ProfileOrderDto> Orders { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class DoctorProfileSummaryDto
{
    public int DoctorProfileId { get; set; }
    public string? Image { get; set; }
    public string Qualification { get; set; } = string.Empty;
    public string Specialisation { get; set; } = string.Empty;
    public int Experience { get; set; }
    public decimal ConsultationFee { get; set; }
    public bool IsActive { get; set; }
    public List<ProfileBookingDto> Bookings { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ProfileResponse
{
    public UserDto User { get; set; } = null!;
    public CustomerProfileDto? CustomerProfile { get; set; }
    public DoctorProfileSummaryDto? DoctorProfile { get; set; }
}
