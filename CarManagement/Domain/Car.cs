namespace CarManagement.Domain;

public class Car
{
    public int Id { get; set; }
    public required string Make { get; set; }
    public required string Model { get; set; }
    public required int Year { get; set; }
}
