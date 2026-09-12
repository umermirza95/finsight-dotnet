using System.ComponentModel.DataAnnotations;

namespace Finsight.DTOs
{
    public class PushSubscriptionDTO
    {
        [Required]
        public string Endpoint { get; set; } = string.Empty;

        [Required]
        public PushSubscriptionKeysDTO Keys { get; set; } = new();
    }

    public class PushSubscriptionKeysDTO
    {
        [Required]
        public string P256dh { get; set; } = string.Empty;

        [Required]
        public string Auth { get; set; } = string.Empty;
    }
}
