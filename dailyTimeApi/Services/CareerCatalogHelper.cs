using dailyTimeApi.Exceptions;
using dailyTimeApi.Models.Response;

namespace dailyTimeApi.Services;

internal static class CareerCatalogHelper
{
    public static string NormalizeName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ValidationException("El nombre es obligatorio.");
        var normalized = value.Trim();
        if (normalized.Length > 100)
            throw new ValidationException("El nombre no puede superar 100 caracteres.");
        return normalized;
    }

    public static string? NormalizeDescription(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var normalized = value.Trim();
        if (normalized.Length > 300)
            throw new ValidationException("La descripción no puede superar 300 caracteres.");
        return normalized;
    }

    public static string NormalizeColor(string? value, string fallback = "#64748B")
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;
        var normalized = value.Trim();
        if (normalized.Length > 20)
            throw new ValidationException("El color no puede superar 20 caracteres.");
        return normalized;
    }

    public static CareerCatalogResponse Map(
        int id,
        string name,
        string? description,
        bool isActive,
        DateTime createdAt,
        string? color = null,
        int? sortOrder = null) =>
        new()
        {
            Id = id,
            Name = name,
            Description = description,
            Color = color,
            SortOrder = sortOrder,
            IsActive = isActive,
            CreatedAt = createdAt
        };
}
