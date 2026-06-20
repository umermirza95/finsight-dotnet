using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Finsight.Models
{
    public class FSRefreshToken
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Token { get; set; } = string.Empty;

        public DateTime Expires { get; set; }

        public bool IsExpired => DateTime.UtcNow >= Expires;

        public DateTime Created { get; set; } = DateTime.UtcNow;

        public DateTime? Revoked { get; set; }

        public bool IsActive => Revoked == null && !IsExpired;

        [Required]
        public string FSUserId { get; set; } = string.Empty;

        [ForeignKey("FSUserId")]
        public virtual FSUser? User { get; set; }
    }
}
