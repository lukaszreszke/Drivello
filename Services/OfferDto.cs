using Range = Drivello.Models.Range;

namespace Drivello.Services;

public record OfferDto
{
    public decimal PricePerMinute { get; init; }
    public Range Range { get; init; }
}