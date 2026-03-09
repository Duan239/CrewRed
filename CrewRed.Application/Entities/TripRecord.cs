namespace CrewRed.Infrastructure.Entities;


public class TripRecord
{
    public DateTime PickupDatetimeUtc { get; set; }
    public DateTime DropoffDatetimeUtc { get; set; }
    public int PassengerCount { get; set; }
    public decimal TripDistance { get; set; }
    public string StoreAndFwdFlag { get; set; } = string.Empty;
    public int PULocationID { get; set; }
    public int DOLocationID { get; set; }
    public decimal FareAmount { get; set; }
    public decimal TipAmount { get; set; }
}
