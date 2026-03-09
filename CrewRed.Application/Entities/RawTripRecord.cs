namespace CrewRed.Infrastructure.Entities;

using CsvHelper.Configuration.Attributes;
public class RawTripRecord
{
    [Name("VendorID")]
    public string? VendorID { get; set; }

    [Name("tpep_pickup_datetime")]
    public string? PickupDatetime { get; set; }

    [Name("tpep_dropoff_datetime")]
    public string? DropoffDatetime { get; set; }

    [Name("passenger_count")]
    public string? PassengerCount { get; set; }

    [Name("trip_distance")]
    public string? TripDistance { get; set; }

    [Name("RatecodeID")]
    public string? RatecodeID { get; set; }

    [Name("store_and_fwd_flag")]
    public string? StoreAndFwdFlag { get; set; }

    [Name("PULocationID")]
    public string? PULocationID { get; set; }

    [Name("DOLocationID")]
    public string? DOLocationID { get; set; }

    [Name("payment_type")]
    public string? PaymentType { get; set; }

    [Name("fare_amount")]
    public string? FareAmount { get; set; }

    [Name("extra")]
    public string? Extra { get; set; }

    [Name("mta_tax")]
    public string? MtaTax { get; set; }

    [Name("tip_amount")]
    public string? TipAmount { get; set; }

    [Name("tolls_amount")]
    public string? TollsAmount { get; set; }

    [Name("improvement_surcharge")]
    public string? ImprovementSurcharge { get; set; }

    [Name("total_amount")]
    public string? TotalAmount { get; set; }

    [Name("congestion_surcharge")]
    public string? CongestionSurcharge { get; set; }
}
