using dailyTimeApi.Exceptions;
using dailyTimeApi.Models.Entities;
using dailyTimeApi.Models.Request;
using dailyTimeApi.Models.Response;
using dailyTimeApi.Repository.Interfaces;
using dailyTimeApi.Services.Interfaces;

namespace dailyTimeApi.Services;

public class CareerProfileService : ICareerProfileService
{
    private readonly ICareerProfileRepository _repository;

    public CareerProfileService(ICareerProfileRepository repository) => _repository = repository;

    public async Task<CareerProfileResponse> GetAsync(CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetAsync(cancellationToken);
        return entity is null ? Empty() : Map(entity);
    }

    public async Task<CareerProfileResponse> UpsertAsync(
        UpsertCareerProfileRequest request, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var entity = await _repository.GetTrackedAsync(cancellationToken);
        if (entity is null)
        {
            entity = new CareerProfile { CreatedAt = now };
            await _repository.AddAsync(entity, cancellationToken);
        }

        Apply(entity, request);
        entity.UpdatedAt = now;
        await _repository.SaveChangesAsync(cancellationToken);

        var saved = await _repository.GetAsync(cancellationToken)
            ?? throw new NotFoundException("No se pudo leer el perfil de postulación.");
        return Map(saved);
    }

    private static void Apply(CareerProfile entity, UpsertCareerProfileRequest request)
    {
        entity.FullName = Require(request.FullName, 200, "nombre");
        entity.Headline = Optional(request.Headline, 300, "titular");
        entity.Location = Optional(request.Location, 150, "ubicación");
        entity.Timezone = Optional(request.Timezone, 80, "zona horaria");
        entity.Availability = Optional(request.Availability, 120, "disponibilidad");
        entity.PreferredModality = Optional(request.PreferredModality, 80, "modalidad");
        entity.Email = Optional(request.Email, 200, "correo");
        entity.Phone = Optional(request.Phone, 50, "teléfono");
        entity.Summary = Optional(request.Summary, 8000, "resumen");

        var salary = request.Salary;
        if (salary?.Min is not null && salary.Max is not null && salary.Min > salary.Max)
            throw new ValidationException("El salario mínimo no puede ser mayor que el máximo.");
        entity.SalaryMin = salary?.Min;
        entity.SalaryMax = salary?.Max;
        entity.SalaryCurrency = Optional(salary?.Currency, 10, "moneda");
        entity.SalaryPeriod = Optional(salary?.Period, 30, "periodo salarial");
        entity.SalaryNotes = Optional(salary?.Notes, 500, "notas de salario");

        Replace(entity.Links, request.Links, (item, order) => new CareerProfileLink
        {
            Label = Require(item.Label, 80, "etiqueta del enlace"),
            Url = Require(item.Url, 500, "url del enlace"),
            SortOrder = order
        });
        Replace(entity.Languages, request.Languages, (item, order) => new CareerProfileLanguage
        {
            Name = Require(item.Name, 80, "idioma"),
            Level = Optional(item.Level, 80, "nivel de idioma"),
            SortOrder = order
        });
        ReplaceNamed(entity.Countries, request.PreferredCountries, 80, "país",
            (name, order) => new CareerProfileCountry { Name = name, SortOrder = order });
        ReplaceNamed(entity.Stacks, request.PreferredStacks, 80, "stack",
            (name, order) => new CareerProfileStack { Name = name, SortOrder = order });
        ReplaceNamed(entity.Strengths, request.Strengths, 300, "fortaleza",
            (text, order) => new CareerProfileStrength { Text = text, SortOrder = order });
        Replace(entity.Education, request.Education, (item, order) => new CareerProfileEducation
        {
            Title = Require(item.Title, 200, "formación"),
            Place = Optional(item.Place, 200, "institución"),
            Year = Optional(item.Year, 20, "año de formación"),
            SortOrder = order
        });
        Replace(entity.Certifications, request.Certifications, (item, order) => new CareerProfileCertification
        {
            Title = Require(item.Title, 200, "certificación"),
            Issuer = Optional(item.Issuer, 200, "emisor"),
            Year = Optional(item.Year, 20, "año de certificación"),
            SortOrder = order
        });
        Replace(entity.CoverLetters, request.CoverLetters, (item, order) => new CareerCoverLetter
        {
            Name = Require(item.Name, 120, "nombre de la carta"),
            Language = Optional(item.Language, 20, "idioma de la carta") ?? "es",
            Stack = Optional(item.Stack, 80, "stack de la carta"),
            Body = Require(item.Body, 16000, "cuerpo de la carta"),
            IsActive = item.IsActive,
            SortOrder = order
        });
    }

    private static void Replace<TEntity, TRequest>(
        ICollection<TEntity> collection,
        IEnumerable<TRequest>? items,
        Func<TRequest, int, TEntity> factory)
    {
        collection.Clear();
        var order = 0;
        foreach (var item in items ?? [])
            collection.Add(factory(item, order++));
    }

    private static void ReplaceNamed<TEntity>(
        ICollection<TEntity> collection,
        IEnumerable<string>? values,
        int maxLength,
        string fieldLabel,
        Func<string, int, TEntity> factory)
    {
        collection.Clear();
        var order = 0;
        foreach (var raw in values ?? [])
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;
            collection.Add(factory(Require(raw, maxLength, fieldLabel), order++));
        }
    }

    private static string Require(string? value, int maxLength, string fieldLabel)
    {
        var normalized = Optional(value, maxLength, fieldLabel);
        if (normalized is null)
            throw new ValidationException($"El {fieldLabel} es obligatorio.");
        return normalized;
    }

    private static string? Optional(string? value, int maxLength, string fieldLabel)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        if (normalized.Length > maxLength)
            throw new ValidationException($"El {fieldLabel} no puede superar {maxLength} caracteres.");
        return normalized;
    }

    private static CareerProfileResponse Empty() => new()
    {
        Id = 0,
        FullName = string.Empty,
        Salary = new CareerProfileSalaryResponse()
    };

    private static CareerProfileResponse Map(CareerProfile entity) => new()
    {
        Id = entity.Id,
        FullName = entity.FullName,
        Headline = entity.Headline,
        Location = entity.Location,
        Timezone = entity.Timezone,
        Availability = entity.Availability,
        PreferredModality = entity.PreferredModality,
        Email = entity.Email,
        Phone = entity.Phone,
        Links = entity.Links.OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
            .Select(x => new CareerProfileLinkResponse { Id = x.Id, Label = x.Label, Url = x.Url })
            .ToList(),
        Languages = entity.Languages.OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
            .Select(x => new CareerProfileLanguageResponse { Id = x.Id, Name = x.Name, Level = x.Level })
            .ToList(),
        Salary = new CareerProfileSalaryResponse
        {
            Min = entity.SalaryMin,
            Max = entity.SalaryMax,
            Currency = entity.SalaryCurrency,
            Period = entity.SalaryPeriod,
            Notes = entity.SalaryNotes
        },
        PreferredCountries = entity.Countries.OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
            .Select(x => x.Name).ToList(),
        PreferredStacks = entity.Stacks.OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
            .Select(x => x.Name).ToList(),
        Summary = entity.Summary,
        Strengths = entity.Strengths.OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
            .Select(x => x.Text).ToList(),
        Education = entity.Education.OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
            .Select(x => new CareerProfileEducationResponse
            {
                Id = x.Id,
                Title = x.Title,
                Place = x.Place,
                Year = x.Year
            })
            .ToList(),
        Certifications = entity.Certifications.OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
            .Select(x => new CareerProfileCertificationResponse
            {
                Id = x.Id,
                Title = x.Title,
                Issuer = x.Issuer,
                Year = x.Year
            })
            .ToList(),
        CoverLetters = entity.CoverLetters.OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
            .Select(x => new CareerCoverLetterResponse
            {
                Id = x.Id,
                Name = x.Name,
                Language = x.Language,
                Stack = x.Stack,
                Body = x.Body,
                IsActive = x.IsActive
            })
            .ToList(),
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };
}
