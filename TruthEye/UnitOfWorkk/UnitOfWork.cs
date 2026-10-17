using Microsoft.EntityFrameworkCore.Storage;
using TruthEye.Data;
using TruthEye.Repositories;
using TruthEye.Interfaces;

namespace TruthEye.UnitOfWorkk
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private readonly Dictionary<Type, object> _repositories;
        private IDbContextTransaction? _transaction;

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
            _repositories = new Dictionary<Type, object>();
        }

        public IRepository<T> Repository<T>() where T : class
        {
            if (_repositories.ContainsKey(typeof(T)))
            {
                return (IRepository<T>)_repositories[typeof(T)];
            }

            var repository = new Repository<T>(_context);
            _repositories.Add(typeof(T), repository);
            return repository;
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction == null)
                throw new InvalidOperationException("No active transaction. Call BeginTransactionAsync first.");

            try
            {
                await SaveChangesAsync();
                await _transaction.CommitAsync();
            }
            catch
            {
                await RollbackTransactionAsync();
                throw;
            }
            finally
            {
                if (_transaction != null)
                {
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction == null) return;

            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }

        public void Dispose()
        {
            // The AppDbContext lifetime is managed by the DI container (Scoped).
            _transaction?.Dispose();
            _transaction = null;
        }
    }
}