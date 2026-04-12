using Microsoft.EntityFrameworkCore;
using Pickadate.Domain.AntiAbuse;
using Pickadate.Domain.Auth;
using Pickadate.Domain.Invitations;
using Pickadate.Domain.Users;

namespace Pickadate.Infrastructure.Persistence;

public class PickadateDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<CounterProposal> CounterProposals => Set<CounterProposal>();
    public DbSet<VerificationCode> VerificationCodes => Set<VerificationCode>();
    public DbSet<DeclineRecord> DeclineRecords => Set<DeclineRecord>();

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
            b.Property(i => i.Vibe).HasConversion<int>();
            b.Property(i => i.CustomVibe).HasMaxLength(64);
            b.Property(i => i.Status).HasConversion<int>();
            b.Property(i => i.Message).HasMaxLength(140);
            b.Property(i => i.MediaUrl).HasMaxLength(512);
            b.Property(i => i.DeclineReason).HasMaxLength(80);
            b.HasIndex(i => i.InitiatorId);

            b.OwnsOne(i => i.Place, p =>
            {
                p.Property(x => x.GooglePlaceId).HasColumnName("place_google_id").HasMaxLength(256);
                p.Property(x => x.Name).HasColumnName("place_name").IsRequired().HasMaxLength(256);
                p.Property(x => x.FormattedAddress).HasColumnName("place_formatted_address").IsRequired().HasMaxLength(512);
                p.Property(x => x.Lat).HasColumnName("place_lat");
                p.Property(x => x.Lng).HasColumnName("place_lng");
            });

            b.Ignore(i => i.DomainEvents);
        });

        modelBuilder.Entity<CounterProposal>(b =>
        {
            b.ToTable("counter_proposals");
            b.HasKey(c => c.Id);
            b.Property(c => c.Kind).HasConversion<int>();
            b.HasIndex(c => new { c.InvitationId, c.Round });

            b.OwnsOne(c => c.NewPlace, p =>
            {
                p.Property(x => x.GooglePlaceId).HasColumnName("new_place_google_id").HasMaxLength(256);
                p.Property(x => x.Name).HasColumnName("new_place_name").HasMaxLength(256);
                p.Property(x => x.FormattedAddress).HasColumnName("new_place_formatted_address").HasMaxLength(512);
                p.Property(x => x.Lat).HasColumnName("new_place_lat");
                p.Property(x => x.Lng).HasColumnName("new_place_lng");
            });

            b.Ignore(c => c.DomainEvents);
        });

        modelBuilder.Entity<VerificationCode>(b =>
        {
            b.ToTable("verification_codes");
            b.HasKey(v => v.Id);
            b.Property(v => v.Email).IsRequired().HasMaxLength(320);
            b.Property(v => v.Code).IsRequired().HasMaxLength(6);
            b.HasIndex(v => new { v.Email, v.ExpiresAt });
            b.Ignore(v => v.DomainEvents);
        });

        modelBuilder.Entity<DeclineRecord>(b =>
        {
            b.ToTable("decline_records");
            b.HasKey(d => d.Id);
            b.Property(d => d.Ip).IsRequired().HasMaxLength(64);
            b.HasIndex(d => new { d.Ip, d.CreatedAt });
        });
    }
}
