using Microsoft.EntityFrameworkCore;

namespace IFNMU_API_NORM.Models
{
    public class DatabaseContext : DbContext
    {
        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<Week> Weeks { get; set; }
        public DbSet<Day> Days { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Filename=db.db");
        } 
    }
}