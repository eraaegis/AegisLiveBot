using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AegisLiveBot.DAL.Repository.Implementation
{
    public class Repository<TEntity> : IRepository<TEntity> where TEntity : Entity
    {
        protected Context _context;
        protected DbSet<TEntity> _dbset;
        public Repository(Context context)
        {
            _context = context;
            _dbset = context.Set<TEntity>();
        }
        public TEntity GetById(int id)
        {
            return _dbset.Find(id);
        }
        public IEnumerable<TEntity> GetAll()
        {
            return _dbset.ToList();
        }
        public void Insert(TEntity entity)
        {
            _dbset.Add(entity);
        }
        public void Delete(int id)
        {
            _dbset.Remove(_dbset.Find(id));
        }
        public void Delete(TEntity entity)
        {
            _dbset.Remove(entity);
        }
        public void Update(TEntity entity)
        {
            _dbset.Update(entity);
        }
    }
}
