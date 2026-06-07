namespace PharmeasyAPI.Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public UserRole Role { get; set; } = UserRole.Customer;
    public bool IsNew { get; set; } = true;
    public string? OtpHash { get; set; }
    public DateTime? OtpExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Product> Products { get; set; } = new List<Product>();
    public DoctorProfile? DoctorProfile { get; set; }
    public AdminProfile? AdminProfile { get; set; }
    public CustomerProfile? CustomerProfile { get; set; }
    public ICollection<Cart> Carts { get; set; } = new List<Cart>();
}
