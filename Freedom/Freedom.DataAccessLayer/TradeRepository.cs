using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace Freedom.DataAccessLayer
{
    public class TradeRepository : IRepository<Trade>
    {
        private readonly DbSet<Trade> Trades;

        public TradeRepository(FreedomContext context)
        {
            Trades = context.Set<Trade>();
        }

        public Trade Get(int id)
        {
            return Trades.FirstOrDefault(x => x.Id == id);
        }

        public List<Trade> List(List<int> ids)
        {
            throw new NotImplementedException();
        }

        public void Add(Trade entity)
        {
            throw new NotImplementedException();
        }
    }
}
