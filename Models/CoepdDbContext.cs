using System;
using System.Configuration;
using System.Data.Entity;

namespace Coepd.Web.Models
{
    public class CoepdDbContext : DbContext
    {
        public CoepdDbContext() : base(ResolveConnectionString())
        {
            Database.CommandTimeout = 30;
        }

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

        private static string ResolveConnectionString()
        {
            var envConnection = Environment.GetEnvironmentVariable("COEPD_DB_CONNECTION");
            if (!string.IsNullOrWhiteSpace(envConnection))
            {
                return envConnection;
            }

            var configured = ConfigurationManager.ConnectionStrings["CoepdDb"]?.ConnectionString;
            return string.IsNullOrWhiteSpace(configured) ? "CoepdDb" : configured;
        }
    }
}
