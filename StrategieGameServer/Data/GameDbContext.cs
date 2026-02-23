using Microsoft.EntityFrameworkCore;
using StrategieGameServer.Models;

namespace StrategieGameServer.Data
{
    public class GameDbContext : DbContext
    {
        public GameDbContext(DbContextOptions<GameDbContext> options) : base(options)
        {
        }

        public DbSet<Question> Questions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Question>(entity =>
            {
                entity.ToTable("Questions"); // Tabellenname in der DB
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
                entity.Property(e => e.QuestionText).IsRequired().HasMaxLength(500);
                entity.Property(e => e.OptionsJson).HasColumnType("nvarchar(max)");
                entity.Property(e => e.CorrectAnswerJson).HasColumnType("nvarchar(max)");
                entity.Property(e => e.Placeholder).HasMaxLength(200);
                entity.Property(e => e.SentenceTemplate).HasMaxLength(500);
                entity.Property(e => e.Placeholders).HasColumnType("nvarchar(max)");
                entity.Property(e => e.OptionsMapping).HasColumnType("nvarchar(max)");
            });
        }
    }
}