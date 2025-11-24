using System.ComponentModel.DataAnnotations;

namespace ConcreteMap.Domain.Dtos;

public class ChangePasswordDto
{
    [Required]
    [MinLength(5)]
    public string OldPassword { get; set; }
    [Required]
    [MinLength(5)]
    public string NewPassword { get; set; }
}