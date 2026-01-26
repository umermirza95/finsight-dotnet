using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Finsight.Models
{
    public class FSFile
    {
        [Key]
        public required Guid Id { get; set; }

        public required string FileName { get; set; }

        public required string FilePath { get; set; }

        public required DateTime UploadedAt { get; set; }

        [ForeignKey(nameof(FSUser))]
        public required string FSUserId { get; set; } 

        [ForeignKey(nameof(Transaction))]
        public Guid? FSTransactionId { get; set; }

        public FSTransaction? Transaction { get; set; }
    }
}