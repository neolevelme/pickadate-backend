using Microsoft.EntityFrameworkCore;
using Pickadate.Domain.Invitations;
using Pickadate.Domain.Users;

namespace Pickadate.Infrastructure.Persistence;

public class PickadateDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Invitation> Invitations => Set<Invitation>();

    public PickadateDbContext(DbContextOptions<PickadateDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(b =>
        {
            b.ToTable("users");
            b.HasKey(u => u.Id);
            b.Property(u => u.Email).IsRequired().HasMaxLength(320);
            b.HasIndex(u => u.Email).IsUnique();
            b.Property(u => u.Name).HasMaxLength(128);
            b.Property(u => u.Country).HasMaxLength(64);
            b.Property(u => u.VibePreference).HasMaxLength(64);
            b.Property(u => u.ProfileImageUrl).HasMaxLength(512);
            b.Ignore(u => u.DomainEvents);
        });

        modelBuilder.Entity<Invitation>(b =>
        {
            b.ToTable("invitations");
            b.HasKey(i => i.Id);
            b.Property(i => i.Slug).IsRequired().HasMaxLength(16);
            b.HasIndex(i => i.Slug).IsUnique();
            b.Property(i => i.CustomVibe).HasMaxLength(64);
            b.Property(i => i.PlaceName).IsRequired().HasMaxLength(256);
            b.Property(i => i.PlaceGoogleId).IsRequired().HasMaxLength(256);
            b.Property(i => i.PlaceFormattedAddress).IsRequired().HasMaxLength(512);
            b.Property(i => i.Message).HasMaxLength(140);
            b.Property(i => i.MediaUrl).HasMaxLength(512);
            b.Ignore(i => i.DomainEvents);
        });
    }
}
