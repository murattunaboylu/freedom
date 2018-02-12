using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Freedom.DataAccessLayer
{
    public class FreedomContextFactory : IDbContextFactory<FreedomContext>
    {
        public FreedomContext Create()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["marketdata-local"].ConnectionString;

            return new FreedomContext(connectionString);
        }

        public FreedomContext Create(string connectionString)
        {
            return new FreedomContext(connectionString);
        }
    }
}
