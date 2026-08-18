using dailyTimeApi.Exceptions;
using dailyTimeApi.Models.Entities;
using dailyTimeApi.Models.Request;
using dailyTimeApi.Models.Response;
using dailyTimeApi.Repository.Interfaces;
using dailyTimeApi.Services.Interfaces;

namespace dailyTimeApi.Services;

public class CareerFieldService : ICareerFieldService
{
    private readonly ICareerFieldRepository _repository;

    public CareerFieldService(ICareerFieldRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<CareerCatalogResponse>> GetAllAsync(
        bool? onlyActive = null, CancellationToken cancellationToken = default)
    {
        var items = await _repository.GetAllAsync(onlyActive, cancellationToken);
        return items.Select(Map).ToList();
    }

    public async Task<CareerCatalogResponse> GetByIdAsync(
        int id, CancellationToken cancellationToken = default) =>
        Map(await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Carrera profesional {id} no encontrada."));

    public async Task<CareerCatalogResponse> CreateAsync(
        CreateCareerCatalogRequest request, CancellationToken cancellationToken = default)
    {
        var name = CareerCatalogHelper.NormalizeName(request.Name);
        var description = CareerCatalogHelper.NormalizeDescription(request.Description);
        if (await _repository.ExistsByNameAsync(name, null, cancellationToken))
            throw new ValidationException($"Ya existe la carrera profesional '{name}'.");

        var entity = new CareerField
        {
            Name = name,
            Description = description,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(entity, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<CareerCatalogResponse> UpdateAsync(
        int id, UpdateCareerCatalogRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Carrera profesional {id} no encontrada.");
        var name = CareerCatalogHelper.NormalizeName(request.Name);
        var description = CareerCatalogHelper.NormalizeDescription(request.Description);

        if (await _repository.ExistsByNameAsync(name, id, cancellationToken))
            throw new ValidationException($"Ya existe la carrera profesional '{name}'.");

        entity.Name = name;
        entity.Description = description;
        entity.IsActive = request.IsActive;
        _repository.Update(entity);
        await _repository.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Carrera profesional {id} no encontrada.");
        if (await _repository.IsInUseAsync(id, cancellationToken))
            throw new ValidationException("No se puede eliminar una carrera profesional que está en uso.");
        _repository.Remove(entity);
        await _repository.SaveChangesAsync(cancellationToken);
    }

    private static CareerCatalogResponse Map(CareerField entity) =>
        CareerCatalogHelper.Map(entity.Id, entity.Name, entity.Description, entity.IsActive, entity.CreatedAt);
}
