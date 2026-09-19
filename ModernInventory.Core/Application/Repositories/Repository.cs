using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ModernInventory.Core.Domain.Entities;
using ModernInventory.Core.Domain.Enums;
using ModernInventory.Core.Infrastructure.Database;

namespace ModernInventory.Core.Application.Repositories
{
    public interface IRepository<T> where T : BaseEntity
    {
        Task<T?> GetByIdAsync(string id, string businessId);
        Task<List<T>> GetAllAsync(string businessId, bool includeDeleted = false);
        Task<List<T>> FindAsync(string businessId, Expression<Func<T, bool>> predicate);
        Task AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task SoftDeleteAsync(string id, string businessId);
    }

    public class Repository<T> : IRepository<T> where T : BaseEntity
    {
        protected readonly AppDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(AppDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public virtual async Task<T?> GetByIdAsync(string id, string businessId)
        {
            return await _dbSet
                .Where(e => e.Id == id && e.BusinessId == businessId && !e.IsDeleted)
                .FirstOrDefaultAsync();
        }

        public virtual async Task<List<T>> GetAllAsync(string businessId, bool includeDeleted = false)
        {
            var query = _dbSet.Where(e => e.BusinessId == businessId);
            if (!includeDeleted)
            {
                query = query.Where(e => !e.IsDeleted);
            }
            return await query.OrderByDescending(e => e.CreatedAt).ToListAsync();
        }

        public virtual async Task<List<T>> FindAsync(string businessId, Expression<Func<T, bool>> predicate)
        {
            return await _dbSet
                .Where(e => e.BusinessId == businessId && !e.IsDeleted)
                .Where(predicate)
                .ToListAsync();
        }

        public virtual async Task AddAsync(T entity)
        {
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.SyncStatus = SyncStatus.PendingCreate;

            await _dbSet.AddAsync(entity);

            // Add to Sync Queue
            await _context.SyncQueue.AddAsync(new SyncQueueItem
            {
                BusinessId = entity.BusinessId,
                EntityType = typeof(T).Name,
                EntityId = entity.Id,
                OperationType = SyncOperationType.CREATE,
                PayloadJson = System.Text.Json.JsonSerializer.Serialize(entity),
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }

        public virtual async Task UpdateAsync(T entity)
        {
            entity.UpdatedAt = DateTime.UtcNow;
            entity.SyncStatus = SyncStatus.PendingUpdate;

            _dbSet.Update(entity);

            // Add to Sync Queue
            await _context.SyncQueue.AddAsync(new SyncQueueItem
            {
                BusinessId = entity.BusinessId,
                EntityType = typeof(T).Name,
                EntityId = entity.Id,
                OperationType = SyncOperationType.UPDATE,
                PayloadJson = System.Text.Json.JsonSerializer.Serialize(entity),
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }

        public virtual async Task SoftDeleteAsync(string id, string businessId)
        {
            var entity = await GetByIdAsync(id, businessId);
            if (entity != null)
            {
                entity.IsDeleted = true;
                entity.UpdatedAt = DateTime.UtcNow;
                entity.SyncStatus = SyncStatus.PendingDelete;

                await _context.SyncQueue.AddAsync(new SyncQueueItem
                {
                    BusinessId = businessId,
                    EntityType = typeof(T).Name,
                    EntityId = id,
                    OperationType = SyncOperationType.DELETE,
                    PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { Id = id, IsDeleted = true }),
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
            }
        }
    }
}
