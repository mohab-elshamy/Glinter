namespace Glinter.Modules.Profiles.Application.Common.Services;

public static class LanguageListNormalizer
{
    public const int MaximumItems = 10;
    public const int MaximumItemLength = 40;

    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var values = value.Split(
            [',', ';', '\n'],
            StringSplitOptions.TrimEntries |
            StringSplitOptions.RemoveEmptyEntries);
        if (values.Length > MaximumItems)
            throw new ValidationException(
                $"Languages cannot contain more than {MaximumItems} values.");
        if (values.Any(x => x.Length > MaximumItemLength))
            throw new ValidationException(
                $"Each language cannot exceed {MaximumItemLength} characters.");

        return string.Join(
            ", ",
            values.Distinct(StringComparer.OrdinalIgnoreCase));
    }
}
