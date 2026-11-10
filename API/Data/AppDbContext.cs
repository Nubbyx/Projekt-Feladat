using Microsoft.EntityFrameworkCore;
using Models;

namespace API.Data
{
    /// <summary>
    /// Az Entity Framework Core adatbázis-kontextusa: ez köti össze a Models projektben lévő
    /// osztályokat az adatbázis tábláival. A kapcsolatok (Folder 1-N Note, Note 1-N Attachment)
    /// a modellek navigációs tulajdonságaiból és a FolderId/NoteId idegen kulcsokból
    /// konvenció alapján derülnek ki, ezért nincs szükség OnModelCreating-re.
    /// A séma módosításához migrációt kell készíteni (Add-Migration), majd alkalmazni (Update-Database).
    /// </summary>
    public class AppDbContext : DbContext
    {
        // A DbContextOptions-t (kapcsolati string, MySQL szolgáltató) a Program.cs adja át a DI-on keresztül.
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Táblák. A "=> Set<T>()" forma azért jó, mert nem kell inicializálni a property-t
        // (nullable figyelmeztetés nélkül), az EF pedig a Set<T>()-ből ugyanazt a táblát adja vissza.
        public DbSet<Note> Notes => Set<Note>();
        public DbSet<Attachment> Attachments => Set<Attachment>();
        public DbSet<Folder> Folders => Set<Folder>();
    }
}
