using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace Freedom.DataAccessLayer
{
    public class OhlcRepository : IRepository<OHLC>
    {
        private readonly DbSet<OHLC> Ohlcs;

        public OhlcRepository(FreedomContext context)
        {
            Ohlcs = context.Set<OHLC>();
        }

        public OHLC Get(int id)
        {
            return Ohlcs.FirstOrDefault(x => x.Id == id);
        }

        public List<OHLC> List(List<int> ids)
        {
            throw new NotImplementedException();
        }

        public void Add(OHLC entity)
        {
            throw new NotImplementedException();
        }
    }
}
