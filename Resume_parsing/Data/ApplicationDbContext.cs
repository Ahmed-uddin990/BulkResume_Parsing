// Resume_parsing.Data/ApplicationDbContext.cs
using Microsoft.EntityFrameworkCore;
using Resume_parsing.Models;

namespace Resume_parsing.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Resume_parsing.Models.Jobs> Jobs { get; set; }
        public DbSet<Resume_parsing.Models.JobProgress> JobProgresses { get; set; }
        public DbSet<Resume_parsing.Models.CV_JobResults> JobResults { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Resume_parsing.Models.JobProgress>()
                .HasOne(jp => jp.Job)
                .WithMany(j => j.JobProgresses)
                .HasForeignKey(jp => jp.Job_Id);

            modelBuilder.Entity<Resume_parsing.Models.CV_JobResults>()
                .HasOne(jr => jr.Job)
                .WithMany(j => j.JobResults)
                .HasForeignKey(jr => jr.JobId)
                .HasPrincipalKey(j => j.JobIdPython);

            base.OnModelCreating(modelBuilder);
        }
    }
}