using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Freedom.DataAccessLayer
{
    public class FreedomContext : DbContext
    {
        public FreedomContext(string connectionString) : base(connectionString)
        {
            
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OHLC>().ToTable("OHLC");
            modelBuilder.Entity<Trade>().ToTable("Trades");
        }
    }
}
