using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace IELTSWord
{
    public class KVContext : DbContext
    {
        public KVContext(string name)
        {
            Name = name;
        }
        public DbSet<KV> Values { get; set; }
        public string Name { get; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source={Name}");
        }
    }

    public class KV
    {
        public string Id { get; set; }
        public string Value { get; set; }

    }
}