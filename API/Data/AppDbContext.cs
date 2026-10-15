using Microsoft.EntityFrameworkCore;
using Models;

namespace API.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Note> Notes => Set<Note>();
        public DbSet<Attachment> Attachments => Set<Attachment>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string connectionString = "Server=localhost;Port=3306;Database=database;User=root;Password=jelszo;";

            optionsBuilder.UseMySQL(connectionString);
        }
    }
}
