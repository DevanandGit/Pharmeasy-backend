using System.ComponentModel.DataAnnotations;

namespace PharmeasyAPI.DTOs;

public class BookingCheckoutRequest
{
    [Required]
    public int DoctorProfileId { get; set; }

    [Required]
    public string TimeSlot { get; set; } = string.Empty;

    [Required]
    public DateTime AppointmentDate { get; set; }

    [Required]
    public string PatientName { get; set; } = string.Empty;

    [Required]
    public string PatientNumber { get; set; } = string.Empty;

    [Required]
    public int Age { get; set; }

    [Required]
    public string Gender { get; set; } = string.Empty;

    public string PrescriptionUpload { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    [Required]
    public string ModeOfConsult { get; set; } = string.Empty;
}
