using System;
using System.Collections.Generic;
using System.Text;

namespace AegisLiveBot.DAL.Repository
{
    public interface IRepository<TEntity> where TEntity: Entity
    {
        TEntity GetById(int id);
        IEnumerable<TEntity> GetAll();
        void Insert(TEntity entity);
        void Delete(int id);
        void Delete(TEntity entity);
        void Update(TEntity entity);
        void AddOrUpdate(TEntity entity);
    }
}
