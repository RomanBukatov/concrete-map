namespace ConcreteMap.Domain.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty; // Храним только хеш!

        // Премодерация: если false — вход запрещен
        public bool IsApproved { get; set; } = false;

        public string Role { get; set; } = "User"; // User, Admin
    }
}