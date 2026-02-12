using Grovetracks.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace Grovetracks.DataAccess;

public class AppDbContext : DbContext
{
    public DbSet<QuickdrawDoodle> QuickdrawDoodles => Set<QuickdrawDoodle>();
    public DbSet<QuickdrawSimpleDoodle> QuickdrawSimpleDoodles => Set<QuickdrawSimpleDoodle>();
    public DbSet<DoodleEngagement> DoodleEngagements => Set<DoodleEngagement>();
    public DbSet<SeedComposition> SeedCompositions => Set<SeedComposition>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<QuickdrawDoodle>(entity =>
        {
            entity.ToTable("quickdraw_doodles");
            entity.HasKey(e => e.KeyId);

            entity.Property(e => e.KeyId).HasColumnName("key_id");
            entity.Property(e => e.Word).HasColumnName("word").HasMaxLength(100);
            entity.Property(e => e.CountryCode).HasColumnName("country_code").HasMaxLength(10);
            entity.Property(e => e.Timestamp).HasColumnName("timestamp");
            entity.Property(e => e.Recognized).HasColumnName("recognized");
            entity.Property(e => e.DrawingReference).HasColumnName("drawing_reference").HasMaxLength(200);

            entity.HasIndex(e => e.Word);
        });

        modelBuilder.Entity<QuickdrawSimpleDoodle>(entity =>
        {
            entity.ToTable("quickdraw_simple_doodles");
            entity.HasKey(e => e.KeyId);

            entity.Property(e => e.KeyId).HasColumnName("key_id");
            entity.Property(e => e.Word).HasColumnName("word").HasMaxLength(100);
            entity.Property(e => e.CountryCode).HasColumnName("country_code").HasMaxLength(10);
            entity.Property(e => e.Timestamp).HasColumnName("timestamp");
            entity.Property(e => e.Recognized).HasColumnName("recognized");
            entity.Property(e => e.Drawing).HasColumnName("drawing").HasColumnType("jsonb");

            entity.HasIndex(e => e.Word);
        });

        modelBuilder.Entity<DoodleEngagement>(entity =>
        {
            entity.ToTable("doodle_engagements");
            entity.HasKey(e => e.KeyId);

            entity.Property(e => e.KeyId).HasColumnName("key_id");
            entity.Property(e => e.Score).HasColumnName("score");
            entity.Property(e => e.EngagedAt).HasColumnName("engaged_at");
        });

        modelBuilder.Entity<SeedComposition>(entity =>
        {
            entity.ToTable("seed_compositions");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Word).HasColumnName("word").HasMaxLength(100);
            entity.Property(e => e.SourceKeyId).HasColumnName("source_key_id");
            entity.Property(e => e.QualityScore).HasColumnName("quality_score");
            entity.Property(e => e.StrokeCount).HasColumnName("stroke_count");
            entity.Property(e => e.TotalPointCount).HasColumnName("total_point_count");
            entity.Property(e => e.CompositionJson).HasColumnName("composition_json").HasColumnType("jsonb");
            entity.Property(e => e.CuratedAt).HasColumnName("curated_at");
            entity.Property(e => e.SourceType).HasColumnName("source_type").HasMaxLength(50).HasDefaultValue("curated");
            entity.Property(e => e.GenerationMethod).HasColumnName("generation_method").HasMaxLength(100);
            entity.Property(e => e.SourceCompositionIds).HasColumnName("source_composition_ids");

            entity.HasIndex(e => e.Word);
            entity.HasIndex(e => e.QualityScore);
            entity.HasIndex(e => e.SourceType);
        });

        base.OnModelCreating(modelBuilder);
    }
}
