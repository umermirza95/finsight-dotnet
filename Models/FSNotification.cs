using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Finsight.Enums;

namespace Finsight.Models
{
    public class FSNotification
    {
        [Key]
        public required Guid Id { get; set; }

        [ForeignKey(nameof(FSUser))]
        public required string FSUserId { get; set; }

        public required string Title { get; set; }

        public required string Message { get; set; }

        public bool IsRead { get; set; }

        public required DateTime CreatedAt { get; set; }

        public required FSNotificationType Type { get; set; }
    }
}
