using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
    public class User
    {
        public int Id { get; set; }

        public string Username { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public byte[] PasswordHash { get; set; } = Array.Empty<byte>();

        public byte[] PasswordSalt { get; set; } = Array.Empty<byte>();

        // ✅ Bu sadece giriş/üye olurken kullanılır, veritabanına kaydedilmez
        [NotMapped]
        public string Password { get; set; } = string.Empty;
    }
}
