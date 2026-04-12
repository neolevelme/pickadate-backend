using Pickadate.BuildingBlocks.Domain;

namespace Pickadate.Domain.Invitations;

public class Place : ValueObject
{
    public string GooglePlaceId { get; }
    public string Name { get; }
    public string FormattedAddress { get; }
    public double Lat { get; }
    public double Lng { get; }

    private Place(string googlePlaceId, string name, string formattedAddress, double lat, double lng)
    {
        GooglePlaceId = googlePlaceId;
        Name = name;
        FormattedAddress = formattedAddress;
        Lat = lat;
        Lng = lng;
    }

    public static Place Create(string googlePlaceId, string name, string formattedAddress, double lat, double lng)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Place name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(formattedAddress))
            throw new ArgumentException("Place address is required.", nameof(formattedAddress));
        if (lat is < -90 or > 90)
            throw new ArgumentOutOfRangeException(nameof(lat), "Latitude must be in [-90, 90].");
        if (lng is < -180 or > 180)
            throw new ArgumentOutOfRangeException(nameof(lng), "Longitude must be in [-180, 180].");

        return new Place(googlePlaceId ?? "", name.Trim(), formattedAddress.Trim(), lat, lng);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return GooglePlaceId;
        yield return Name;
        yield return FormattedAddress;
        yield return Lat;
        yield return Lng;
    }
}
