namespace ConcreteMap.Domain.Dtos;

public class EmergencyResetDto
{
    public string Username { get; set; }
    public string EmergencyKey { get; set; }
    public string NewPassword { get; set; }
}