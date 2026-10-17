using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using TruthEye.Data;
using TruthEye.Interfaces;

namespace TruthEye.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly AppDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(AppDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public IQueryable<T> GetQueryable(params Expression<Func<T, object>>[] includes)
        {
            var query = ApplySoftDeleteFilter(_dbSet);

            if (includes != null)
            {
                foreach (var include in includes)
                {
                    query = query.Include(include);
                }
            }

            return query;
        }

        public async Task<T> GetByIdAsync(int id, params Expression<Func<T, object>>[] includes)
        {
            var query = ApplySoftDeleteFilter(_dbSet);

            if (includes != null)
            {
                foreach (var include in includes)
                {
                    query = query.Include(include);
                }
            }

            return await query.FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id);
        }

        public async Task<IEnumerable<T>> FindAsync(
            Expression<Func<T, bool>> predicate,
            params Expression<Func<T, object>>[] includes)
        {
            var query = ApplySoftDeleteFilter(_dbSet);

            if (includes != null)
            {
                foreach (var include in includes)
                {
                    query = query.Include(include);
                }
            }

            return await query.Where(predicate).ToListAsync();
        }

        public async Task<T> GetByIdAsync(int id)
        {
            var entity = await _dbSet.FindAsync(id);
            return IsSoftDeleteEntity(entity) && IsDeleted(entity) ? null : entity;
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await ApplySoftDeleteFilter(_dbSet).ToListAsync();
        }

        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            var query = ApplySoftDeleteFilter(_dbSet);
            return await query.Where(predicate).ToListAsync();
        }

        public async Task<T> AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
            return entity;
        }

        public async Task UpdateAsync(T entity)
        {
            _dbSet.Update(entity);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(T entity)
        {
            if (IsSoftDeleteEntity(entity))
            {
                SetSoftDeleteProperties(entity, true, DateTime.UtcNow, null);
                _dbSet.Update(entity);
            }
            else
            {
                _dbSet.Remove(entity);
            }
            await Task.CompletedTask;
        }

        public async Task<int> CountAsync(Expression<Func<T, bool>> predicate = null)
        {
            var query = ApplySoftDeleteFilter(_dbSet);

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            return await query.CountAsync();
        }

        public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
        {
            var query = ApplySoftDeleteFilter(_dbSet);
            return await query.AnyAsync(predicate);
        }

        public async Task HardDeleteAsync(T entity)
        {
            _dbSet.Remove(entity);
            await Task.CompletedTask;
        }

        public async Task RestoreAsync(T entity)
        {
            if (IsSoftDeleteEntity(entity))
            {
                SetSoftDeleteProperties(entity, false, null, null);
                _dbSet.Update(entity);
            }
            await Task.CompletedTask;
        }

        public async Task<IEnumerable<T>> GetDeletedAsync()
        {
            if (typeof(ISoftDelete).IsAssignableFrom(typeof(T)))
            {
                return await _dbSet
                    .Where(GetDeletedExpression())
                    .ToListAsync();
            }
            return new List<T>();
        }
        public async Task<IEnumerable<T>> GetAllAsync(params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = ApplySoftDeleteFilter(_dbSet);

            if (includes != null)
            {
                foreach (var include in includes)
                {
                    query = query.Include(include);
                }
            }

            return await query.ToListAsync();
        }


        #region Helper Methods

        private IQueryable<T> ApplySoftDeleteFilter(IQueryable<T> query)
        {
            if (typeof(ISoftDelete).IsAssignableFrom(typeof(T)))
            {
                return query.Where(GetNotDeletedExpression());
            }
            return query;
        }

        private static Expression<Func<T, bool>> GetNotDeletedExpression()
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var isDeletedProperty = Expression.Property(parameter, "IsDeleted");
            var falseConstant = Expression.Constant(false);
            var equalExpression = Expression.Equal(isDeletedProperty, falseConstant);

            return Expression.Lambda<Func<T, bool>>(equalExpression, parameter);
        }

        private static Expression<Func<T, bool>> GetDeletedExpression()
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var isDeletedProperty = Expression.Property(parameter, "IsDeleted");
            var trueConstant = Expression.Constant(true);
            var equalExpression = Expression.Equal(isDeletedProperty, trueConstant);

            return Expression.Lambda<Func<T, bool>>(equalExpression, parameter);
        }

        private static bool IsSoftDeleteEntity(T entity)
        {
            return entity is ISoftDelete;
        }

        private static bool IsDeleted(T entity)
        {
            return entity is ISoftDelete softDeleteEntity && softDeleteEntity.IsDeleted;
        }

        private static void SetSoftDeleteProperties(T entity, bool isDeleted, DateTime? deletedAt, int? deletedBy)
        {
            if (entity is ISoftDelete softDeleteEntity)
            {
                softDeleteEntity.IsDeleted = isDeleted;
                softDeleteEntity.DeletedAt = deletedAt;
                softDeleteEntity.DeletedBy = deletedBy;
            }
        }
       
        #endregion
    }
}