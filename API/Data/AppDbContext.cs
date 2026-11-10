using Microsoft.EntityFrameworkCore;
using Models;

namespace API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Note> Notes => Set<Note>();
        public DbSet<Attachment> Attachments => Set<Attachment>();
        public DbSet<Folder> Folders => Set<Folder>();
    }
}
