using dailyTimeApi.Data;
using dailyTimeApi.Models.Entities;
using dailyTimeApi.Models.Request;
using dailyTimeApi.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace dailyTimeApi.Repository;

public class JobOfferRepository : IJobOfferRepository
{
    private readonly AppDbContext _context;

    public JobOfferRepository(AppDbContext context) => _context = context;

    public Task<JobOffer?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.JobOffers.AsNoTracking()
            .Include(x => x.JobPortal)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<JobOffer?> GetTrackedByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.JobOffers
            .Include(x => x.JobPortal)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<JobOffer>> GetAllAsync(
        JobOfferFilterRequest? filter = null,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyFilter(_context.JobOffers.AsNoTracking().Include(x => x.JobPortal), filter);
        return await query
            .OrderByDescending(x => x.PostedAt ?? x.CapturedAt)
            .ThenByDescending(x => x.CapturedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetDistinctCountriesAsync(
        CancellationToken cancellationToken = default) =>
        await _context.JobOffers.AsNoTracking()
            .Where(x => x.Country != null && x.Country != "")
            .Select(x => x.Country!)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<string>> GetDistinctLanguagesAsync(
        CancellationToken cancellationToken = default) =>
        await _context.JobOffers.AsNoTracking()
            .Where(x => x.Language != null && x.Language != "")
            .Select(x => x.Language!)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<string>> GetDistinctWorkModalitiesAsync(
        CancellationToken cancellationToken = default) =>
        await _context.JobOffers.AsNoTracking()
            .Where(x => x.WorkModality != null && x.WorkModality != "")
            .Select(x => x.WorkModality!)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<string>> GetDistinctContractTypesAsync(
        CancellationToken cancellationToken = default) =>
        await _context.JobOffers.AsNoTracking()
            .Where(x => x.ContractType != null && x.ContractType != "")
            .Select(x => x.ContractType!)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<string>> GetDistinctTechStacksAsync(
        CancellationToken cancellationToken = default)
    {
        var raw = await _context.JobOffers.AsNoTracking()
            .Where(x => x.TechStack != null && x.TechStack != "")
            .Select(x => x.TechStack!)
            .ToListAsync(cancellationToken);

        return raw
            .SelectMany(s => s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(s => s.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public Task<JobOffer?> FindByPortalAndKeyAsync(
        int portalId, string externalKey, CancellationToken cancellationToken = default) =>
        _context.JobOffers.FirstOrDefaultAsync(
            x => x.JobPortalId == portalId && x.ExternalKey == externalKey,
            cancellationToken);

    public async Task<JobOffer?> FindByUrlAsync(
        string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        var normalized = url.Trim();
        var byUrl = await _context.JobOffers.AsNoTracking()
            .Include(x => x.JobPortal)
            .FirstOrDefaultAsync(x => x.Url == normalized, cancellationToken);
        if (byUrl != null)
            return byUrl;

        return await _context.JobOffers.AsNoTracking()
            .Include(x => x.JobPortal)
            .FirstOrDefaultAsync(
                x => x.ExternalKey == normalized ||
                     (x.Url != null && x.Url.StartsWith(normalized)),
                cancellationToken);
    }

    public Task<JobOffer?> FindByPortalTitleCompanyAsync(
        int portalId, string title, string? company, CancellationToken cancellationToken = default)
    {
        var normalizedTitle = title.Trim().ToLower();
        var normalizedCompany = (company ?? string.Empty).Trim().ToLower();
        return _context.JobOffers.FirstOrDefaultAsync(
            x => x.JobPortalId == portalId
                 && x.Title.ToLower() == normalizedTitle
                 && ((x.Company ?? string.Empty).ToLower() == normalizedCompany),
            cancellationToken);
    }

    public async Task<int> BulkUpdateStatusAsync(
        IReadOnlyList<int> ids, string status, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
            return 0;

        var now = DateTime.UtcNow;
        return await _context.JobOffers
            .Where(x => ids.Contains(x.Id))
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(x => x.Status, status)
                    .SetProperty(x => x.UpdatedAt, now),
                cancellationToken);
    }

    public async Task<int> BulkDeleteAsync(
        IReadOnlyList<int> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
            return 0;

        return await _context.JobOffers
            .Where(x => ids.Contains(x.Id))
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task AddAsync(JobOffer entity, CancellationToken cancellationToken = default) =>
        await _context.JobOffers.AddAsync(entity, cancellationToken);

    public void Update(JobOffer entity) => _context.JobOffers.Update(entity);

    public void Remove(JobOffer entity) => _context.JobOffers.Remove(entity);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    private static IQueryable<JobOffer> ApplyFilter(
        IQueryable<JobOffer> query, JobOfferFilterRequest? filter)
    {
        if (filter is null)
            return query;

        if (filter.PortalId.HasValue)
            query = query.Where(x => x.JobPortalId == filter.PortalId.Value);

        if (!string.IsNullOrWhiteSpace(filter.Status))
            query = query.Where(x => x.Status == filter.Status.Trim());

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = $"%{filter.Search.Trim()}%";
            query = query.Where(x =>
                EF.Functions.Like(x.Title, term)
                || (x.Company != null && EF.Functions.Like(x.Company, term))
                || (x.Location != null && EF.Functions.Like(x.Location, term)));
        }

        if (!string.IsNullOrWhiteSpace(filter.Country))
            query = query.Where(x => x.Country == filter.Country.Trim());

        if (!string.IsNullOrWhiteSpace(filter.Language))
            query = query.Where(x => x.Language == filter.Language.Trim());

        if (!string.IsNullOrWhiteSpace(filter.WorkModality))
            query = query.Where(x => x.WorkModality == filter.WorkModality.Trim());

        if (!string.IsNullOrWhiteSpace(filter.ContractType))
            query = query.Where(x => x.ContractType == filter.ContractType.Trim());

        if (!string.IsNullOrWhiteSpace(filter.TechStack))
        {
            var tech = filter.TechStack.Trim();
            query = query.Where(x => x.TechStack != null && EF.Functions.Like(x.TechStack, $"%{tech}%"));
        }

        if (filter.CapturedFrom.HasValue)
            query = query.Where(x => x.CapturedAt >= filter.CapturedFrom.Value);

        if (filter.CapturedTo.HasValue)
        {
            var capturedTo = filter.CapturedTo.Value;
            if (capturedTo.TimeOfDay == TimeSpan.Zero)
                capturedTo = capturedTo.Date.AddDays(1).AddTicks(-1);
            query = query.Where(x => x.CapturedAt <= capturedTo);
        }

        if (filter.PostedFrom.HasValue)
            query = query.Where(x => x.PostedAt != null && x.PostedAt >= filter.PostedFrom.Value);

        if (filter.PostedTo.HasValue)
        {
            var postedTo = filter.PostedTo.Value;
            if (postedTo.TimeOfDay == TimeSpan.Zero)
                postedTo = postedTo.Date.AddDays(1).AddTicks(-1);
            query = query.Where(x => x.PostedAt != null && x.PostedAt <= postedTo);
        }

        return query;
    }
}
