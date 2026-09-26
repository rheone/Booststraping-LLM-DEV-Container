// TEMPLATE — async-first repository base. See reference/async-patterns.md before adapting:
// - one ISession instance, never touched by two concurrent operations (no Task.WhenAll on `_session`)
// - cancellation tokens threaded through every method and passed to the NHibernate async overloads
// - transaction boundary conventions live in the SERVICE layer per reference/session-lifecycle.md —
//   this base class deliberately does NOT open transactions itself; callers/services do, so multiple
//   repository calls in one service method can share one atomic transaction.

using System;
using System.Threading;
using System.Threading.Tasks;
using NHibernate;
using NHibernate.Linq;

public abstract class RepositoryBase<TEntity, TId> where TEntity : class
{
    protected readonly ISession Session;

    protected RepositoryBase(ISession session)
    {
        Session = session ?? throw new ArgumentNullException(nameof(session));
    }

    // Get: hits the DB immediately, returns null if missing — use when the caller needs to branch
    // on existence. See reference/glossary.md and reference/lazy-loading-and-fetching.md § Get vs Load.
    public virtual Task<TEntity> GetAsync(TId id, CancellationToken cancellationToken = default) =>
        Session.GetAsync<TEntity>(id, cancellationToken);

    // Load: returns a proxy without a round trip — use only when you're confident the ID is valid
    // and you're just wiring up a relationship, not reading the entity's own data.
    public virtual Task<TEntity> LoadAsync(TId id, CancellationToken cancellationToken = default) =>
        Session.LoadAsync<TEntity>(id, cancellationToken);

    public virtual Task SaveAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        Session.SaveAsync(entity, cancellationToken);

    public virtual Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        Session.UpdateAsync(entity, cancellationToken);

    public virtual Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        Session.DeleteAsync(entity, cancellationToken);

    // Base IQueryable escape hatch for derived repositories to build specific queries against —
    // derived classes should prefer projecting to a DTO shape (see templates/queryover-projection.cs)
    // for read-heavy queries rather than always returning full TEntity graphs.
    protected IQueryable<TEntity> Query() => Session.Query<TEntity>();

    // NOTE: this base class deliberately does not expose Flush()/transaction control — see
    // reference/session-lifecycle.md for why that boundary belongs at the service layer, not here.
    // If a specific repository method genuinely needs a mid-method flush (rare — usually a sign the
    // transaction boundary is in the wrong place), make that an explicit, commented exception, not
    // a habit.
}

// Example derived repository showing where entity-specific query methods go:
//
// public sealed class OrderRepository : RepositoryBase<Order, Guid>
// {
//     public OrderRepository(ISession session) : base(session) { }
//
//     public Task<List<Order>> GetPendingOrdersAsync(CancellationToken ct = default) =>
//         Query().Where(o => o.Status == OrderStatus.Pending).ToListAsync(ct);
// }
