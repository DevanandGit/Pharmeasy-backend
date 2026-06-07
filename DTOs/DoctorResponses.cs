namespace PharmeasyAPI.DTOs;

public class DoctorResponseDto
{
    public int DoctorProfileId { get; set; }
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
    public string Image { get; set; } = string.Empty;
    public string Qualification { get; set; } = string.Empty;
    public string Specialisation { get; set; } = string.Empty;
    public int Experience { get; set; }
    public decimal ConsultationFee { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<DoctorWeekdayAvailabilityDto> WeeklyAvailability { get; set; } = new();
}

public class DoctorSlotDto
{
    public string Start { get; set; } = string.Empty;
    public string End { get; set; } = string.Empty;
}

public class DoctorWeekdayAvailabilityDto
{
    public string Weekday { get; set; } = string.Empty;
    public List<DoctorSlotDto> AvailableSlots { get; set; } = new();
}

public class DoctorCustomerSummaryDto
{
    public int DoctorProfileId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public string Qualification { get; set; } = string.Empty;
    public string Specialisation { get; set; } = string.Empty;
    public int Experience { get; set; }
    public decimal ConsultationFee { get; set; }
}

public class DoctorCustomerDetailDto : DoctorCustomerSummaryDto
{
    public List<DoctorWeekdayAvailabilityDto> WeeklyAvailability { get; set; } = new();
}
