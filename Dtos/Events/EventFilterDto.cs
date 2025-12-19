namespace Ping.Dtos.Events;

public record EventFilterDto
{
    public double? MinPrice { get; init; }
    public double? MaxPrice { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public int? GenreId { get; init; }
    
    // Geospatial
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public double? RadiusKm { get; init; }
    public string? Query { get; init; }
}
