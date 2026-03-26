using System.Data.Entity;

namespace Coepd.Web.Models
{
    public class CoepdDbContext : DbContext
    {
        public CoepdDbContext() : base("CoepdDb") { }

        public DbSet<Lead> Leads { get; set; }
        public DbSet<Staff> Staff { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Lead>().Property(x => x.InterestedDomain).HasColumnName("interested_domain");
            modelBuilder.Entity<Lead>().Property(x => x.CreatedAt).HasColumnName("created_at");
            modelBuilder.Entity<Lead>().Property(x => x.Source).HasColumnName("source");
            modelBuilder.Entity<Lead>().Property(x => x.Whatsapp).HasColumnName("whatsapp");
            modelBuilder.Entity<Staff>().Property(x => x.PasswordHash).HasColumnName("password_hash");
            modelBuilder.Entity<Staff>().Property(x => x.CreatedAt).HasColumnName("created_at");
            base.OnModelCreating(modelBuilder);
        }
    }
}
