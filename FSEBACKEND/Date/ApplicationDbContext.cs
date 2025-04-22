using Microsoft.EntityFrameworkCore;
using FSEBACKEND.Models;

namespace FSEBACKEND.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Book> Books { get; set; }
        public DbSet<Request> Requests { get; set; }
        public DbSet<Lend> Lends { get; set; }
        public DbSet<Challan> Challans { get; set; }
        public DbSet<Review> Reviews { get; set; }
    }
}
