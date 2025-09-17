using System.ComponentModel.DataAnnotations;

namespace MakrisPortfolio.Server.Data.Entities
{
    public class ResourceItem
    {
        public int Id { get; set; }

        [Required] public string Title { get; set; } = string.Empty;
        [Required] public string Url { get; set; } = string.Empty;

        public bool IsPremium { get; set; }
    }

    public enum PremiumRequestStatus { Pending = 0, Approved = 1, Denied = 2 }

    public class PremiumRequest
    {
        public int Id { get; set; }
        public string UserId { get; set; } = default!;
        public string Email { get; set; } = default!;
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        public PremiumRequestStatus Status { get; set; } = PremiumRequestStatus.Pending;
        public string? ReviewedByUserId { get; set; }
        public DateTime? ReviewedUtc { get; set; }
        public string? Notes { get; set; }
    }
}