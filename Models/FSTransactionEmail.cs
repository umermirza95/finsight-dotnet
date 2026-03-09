using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Finsight.Models
{
    public class FSTransactionEmail
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string UserId { get; set; }= string.Empty;

        public string Subject { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty; 
        public string Html { get; set; } = string.Empty;

        [Column(TypeName = "text[]")]
        public List<string> From { get; set; } = [];

        [Column(TypeName = "text[]")]
        public List<string> To { get; set; } = [];

        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    }
}