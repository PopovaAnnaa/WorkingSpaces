using System.ComponentModel.DataAnnotations;

namespace WorkingSpaces.Models;

public class RegisterViewModel
{
    [Required(ErrorMessage = "The Username field is required.")]
    [StringLength(50, ErrorMessage = "Username length can be maximum 50 characters.")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "The Full Name field is required.")]
    [StringLength(500, ErrorMessage = "FullName length can be maximum 500 characters.")]
    public string FullName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "The Password field is required.")]
    [DataType(DataType.Password)]
    [StringLength(16, MinimumLength = 8, ErrorMessage = "Password length can be 8-16 characters.")]
    [RegularExpression(@"^(?=.*\d)(?=.*[A-Z])(?=.*[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]).*$", ErrorMessage = "The password must contain at least 1 number, 1 uppercase letter and 1 special character.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm Password is required.")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "The passwords don't match")]
    public string PasswordConfirmation { get; set; } = string.Empty;

    [Required(ErrorMessage = "The Phone Number field is required.")]
    [RegularExpression(@"^\+380\d{9}$", ErrorMessage = "Invalid phone number format. Expected: +380XXXXXXXXX (12 digits)")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "The Email field is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address format")]
    public string Email { get; set; } = string.Empty;
}