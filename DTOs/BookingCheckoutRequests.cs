using System.ComponentModel.DataAnnotations;
using PharmeasyAPI.Models;

namespace PharmeasyAPI.DTOs;

public class BookingCheckoutRequest
{
    [Required]
    public int DoctorProfileId { get; set; }

    /// <summary>Time slot in HH:mm-HH:mm format, e.g. "09:00-09:30".</summary>
    [Required]
    public string TimeSlot { get; set; } = string.Empty;

    /// <summary>Date of the appointment in ISO 8601 format, e.g. "2024-12-25T00:00:00.000Z".</summary>
    [Required]
    public DateTime AppointmentDate { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string PatientName { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^\+?[0-9\s\-]{7,15}$", ErrorMessage = "Invalid phone number.")]
    public string PatientNumber { get; set; } = string.Empty;

    [Required]
    [Range(1, 150)]
    public int Age { get; set; }

    [Required]
    public Gender Gender { get; set; }

    [Required]
    public ConsultMode ModeOfConsult { get; set; }

    public string PrescriptionUpload { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;
}
