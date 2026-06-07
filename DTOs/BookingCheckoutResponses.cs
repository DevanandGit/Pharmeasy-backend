namespace PharmeasyAPI.DTOs;

public class BookingResponseDto
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
    public string CustomerName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class BookingSnapshotDto
{
    public int DoctorProfileId { get; set; }
    public int CustomerProfileId { get; set; }
    public string TimeSlot { get; set; } = string.Empty;
    public DateTime AppointmentDate { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PatientNumber { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string PrescriptionUpload { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ModeOfConsult { get; set; } = string.Empty;
}

public class BookingCheckoutResponse
{
    public int BookingSessionId { get; set; }
    public int DoctorProfileId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string TimeSlot { get; set; } = string.Empty;
    public DateTime AppointmentDate { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PatientNumber { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string PrescriptionUpload { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ModeOfConsult { get; set; } = string.Empty;
    public decimal ConsultationFee { get; set; }
    public string PaymentLinkUrl { get; set; } = string.Empty;
    public string PaymentLinkId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
