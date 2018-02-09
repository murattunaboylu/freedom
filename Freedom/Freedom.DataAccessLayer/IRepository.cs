using System.Collections.Generic;

namespace Freedom.DataAccessLayer
{
    public interface IRepository<T>
    {
        T Get(int id);
        List<T> List(List<int> ids);
        void Add(T entity);
    }
}