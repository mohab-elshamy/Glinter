using Glinter.Modules.Profiles.Application.Common.Services;
using Glinter.Shared.Application.Exceptions;

namespace Glinter.UnitTests;

public sealed class LanguageListNormalizerTests
{
    [Fact]
    public void Languages_are_trimmed_and_deduplicated_case_insensitively()
    {
        var result = LanguageListNormalizer.Normalize(
            " Arabic, English; arabic \n French ");
        Assert.Equal("Arabic, English, French", result);
    }

    [Fact]
    public void Too_many_languages_are_rejected()
    {
        var value = string.Join(
            ",",
            Enumerable.Range(1, 11).Select(x => $"Language{x}"));
        Assert.Throws<ValidationException>(
            () => LanguageListNormalizer.Normalize(value));
    }

    [Fact]
    public void Overlong_language_is_rejected() =>
        Assert.Throws<ValidationException>(
            () => LanguageListNormalizer.Normalize(new string('x', 41)));
}
