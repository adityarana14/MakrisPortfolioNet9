using MakrisPortfolio.Server.Data.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MakrisPortfolio.Server.Data
{
    public class ApplicationDbContext : IdentityDbContext<AppUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) {}
        public DbSet<ResourceItem> Resources => Set<ResourceItem>();
        public DbSet<PremiumRequest> PremiumRequests => Set<PremiumRequest>();
    }
}
