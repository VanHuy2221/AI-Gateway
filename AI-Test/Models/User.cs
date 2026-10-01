using Microsoft.VisualBasic;

namespace AI_Test.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<Conversation> Conversations { get; set; } = new();
    }
}