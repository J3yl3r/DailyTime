using dailyTimeApi.Data;
using dailyTimeApi.Models.Entities;
using dailyTimeApi.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace dailyTimeApi.Repository;

public class PersonRepository : IPersonRepository
{
    private readonly AppDbContext _context;

    public PersonRepository(AppDbContext context) => _context = context;

    public Task<Person?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.People.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Person>> GetAllAsync(
        bool? onlyActive = null, CancellationToken cancellationToken = default)
    {
        var query = _context.People.AsNoTracking().AsQueryable();
        if (onlyActive == true)
            query = query.Where(x => x.IsActive);

        return await query.OrderBy(x => x.Name).ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsByNameAsync(
        string name, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.People.AsNoTracking().Where(x => x.Name == name);
        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);
        return query.AnyAsync(cancellationToken);
    }

    public async Task<bool> IsInUseAsync(int id, CancellationToken cancellationToken = default)
    {
        if (await _context.TaskItems.AsNoTracking()
            .AnyAsync(x => x.PersonId == id, cancellationToken))
            return true;

        return await _context.Notes.AsNoTracking()
            .AnyAsync(x => x.PersonId == id, cancellationToken);
    }

    public async Task AddAsync(Person entity, CancellationToken cancellationToken = default) =>
        await _context.People.AddAsync(entity, cancellationToken);

    public void Update(Person entity) => _context.People.Update(entity);
    public void Remove(Person entity) => _context.People.Remove(entity);
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
