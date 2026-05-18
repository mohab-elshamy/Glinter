using CsvHelper.Configuration.Attributes;


namespace Glinter.Modules.LocationCatalog.Infrastructure.Persistence.Import;

public sealed class HdxLocationCsvRow
{
    [Name("adm0_pcode")]
    public string Adm0Pcode { get; set; } = string.Empty;

    [Name("adm0_name")]
    public string Adm0Name { get; set; } = string.Empty;

    [Name("adm0_name1")]
    public string? Adm0Name1 { get; set; }

    [Name("adm0_name2")]
    public string? Adm0Name2 { get; set; }

    [Name("adm0_name3")]
    public string? Adm0Name3 { get; set; }

    [Name("adm1_pcode")]
    public string Adm1Pcode { get; set; } = string.Empty;

    [Name("adm1_name")]
    public string Adm1Name { get; set; } = string.Empty;

    [Name("adm1_name1")]
    public string? Adm1Name1 { get; set; }

    [Name("adm1_name2")]
    public string? Adm1Name2 { get; set; }

    [Name("adm1_name3")]
    public string? Adm1Name3 { get; set; }

    [Name("adm2_pcode")]
    public string Adm2Pcode { get; set; } = string.Empty;

    [Name("adm2_name")]
    public string Adm2Name { get; set; } = string.Empty;

    [Name("adm2_name1")]
    public string? Adm2Name1 { get; set; }

    [Name("adm2_name2")]
    public string? Adm2Name2 { get; set; }

    [Name("adm2_name3")]
    public string? Adm2Name3 { get; set; }

    [Name("adm3_pcode")]
    public string Adm3Pcode { get; set; } = string.Empty;

    [Name("adm3_name")]
    public string Adm3Name { get; set; } = string.Empty;

    [Name("adm3_name1")]
    public string? Adm3Name1 { get; set; }

    [Name("adm3_name2")]
    public string? Adm3Name2 { get; set; }

    [Name("adm3_name3")]
    public string? Adm3Name3 { get; set; }

    [Name("adm3_ref_name")]
    public string? Adm3RefName { get; set; }

    [Name("Latitude")]
    public double? Latitude { get; set; }

    [Name("Longitude")]
    public double? Longitude { get; set; }
}   