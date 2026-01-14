namespace CodeRush.AiGen.Main.Shared;

public sealed class Address {
    public string? Line1 { get; init; }
    public string? City { get; init; }
    public string? Region { get; init; }     // State/Province
    public string? PostalCode { get; init; }
    public string? CountryCode { get; init; } // e.g., "US"
}

