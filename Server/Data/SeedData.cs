using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MakrisPortfolio.Server.Data.Entities;

namespace MakrisPortfolio.Server.Data

{
    public static class SeedData
    {
        public static async Task RunAsync(IServiceProvider sp, IConfiguration cfg)
        {
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.EnsureCreatedAsync();

            var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            foreach (var r in new[] { "Admin", "Premium" })
                if (!await roleMgr.RoleExistsAsync(r))
                    await roleMgr.CreateAsync(new IdentityRole(r));

            var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

            async Task EnsureUser(string email, string pwd, bool admin)
            {
                var u = await userMgr.Users.FirstOrDefaultAsync(x => x.Email == email);
                if (u == null)
                {
                    u = new AppUser { Email = email, UserName = email, EmailConfirmed = true, DisplayName = email.Split('@')[0] };
                    await userMgr.CreateAsync(u, pwd);
                }
                if (admin && !(await userMgr.IsInRoleAsync(u, "Admin")))
                    await userMgr.AddToRoleAsync(u, "Admin");
            }

            await EnsureUser("admin@makris.dev", "P@ssw0rd!", true);
            await EnsureUser("demo@user.dev", "P@ssw0rd!", false);
            
            
            
            if (!await db.Resources.AnyAsync())
            {
                db.Resources.Add(new ResourceItem { Title = "Public CV", Url = "/files/cv-sample.pdf", IsPremium = false });
                db.Resources.Add(new ResourceItem { Title = "Interview Playbook", Url = "/files/interview-playbook.pdf", IsPremium = true });
                await db.SaveChangesAsync();
            }
            


            if (!db.Resources.Any())
            {
                db.Resources.AddRange(
                    new ResourceItem { Title = "Public: Resume Checklist", Url = "https://example.com/resume-checklist.pdf", IsPremium = false },
                    new ResourceItem { Title = "Public: Career One-Pager",   Url = "https://example.com/career-onepager.pdf",    IsPremium = false },
                    new ResourceItem { Title = "Premium: Interview Loop Playbook (PDF)", Url = "https://example.com/premium-interview-playbook.pdf", IsPremium = true },
                    new ResourceItem { Title = "Premium: 30/60/90 Template (Doc)",       Url = "https://example.com/premium-30-60-90.docx",         IsPremium = true }
                );
                await db.SaveChangesAsync();
            }
        }
    }
}
