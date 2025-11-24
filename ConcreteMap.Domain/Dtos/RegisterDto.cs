using System.ComponentModel.DataAnnotations;

namespace ConcreteMap.Domain.Dtos;

public class RegisterDto
{
    [Required]
    public string Username { get; set; }
    [Required]
    [MinLength(5)]
    public string Password { get; set; }
}