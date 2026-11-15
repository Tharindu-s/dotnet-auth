using auth.webapi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace auth.webapi.Data
{
    public class ApplicationDbContext : IdentityDbContext<AppUser>
    {
        public ApplicationDbContext(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {

        }

        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<ApplicationClient> ApplicationClient { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Identity makes NormalizedUserName unique across the whole database.
            // Usernames only need to be unique within an app, so the same email can register in several apps.
            builder.Entity<AppUser>(user =>
            {
                var userNameIndex = user.Metadata.GetIndexes()
                    .Single(i => i.GetDatabaseName() == "UserNameIndex");
                user.Metadata.RemoveIndex(userNameIndex);

                user.HasIndex(u => new { u.ApplicationClientId, u.NormalizedUserName })
                    .IsUnique()
                    .HasDatabaseName("UserNameIndex");
            });

            builder.Entity<RefreshToken>(token =>
            {
                token.HasIndex(t => t.TokenHash).IsUnique();
                token.HasIndex(t => t.FamilyId);
            });

            builder.Entity<ApplicationClient>()
                .Property(a => a.Audience)
                .HasMaxLength(200);

            List<IdentityRole> roles = new List<IdentityRole>{
                new IdentityRole{
                    Id = "1",
                    Name = "Admin",
                    NormalizedName = "ADMIN"
                },
                new IdentityRole{
                    Id = "2",
                    Name = "User",
                    NormalizedName = "USER"
                }
            };
            builder.Entity<IdentityRole>().HasData(roles);
        }

    }
}
