namespace CloudDebugger.Features.Api;

/// <summary>
/// Represents a customer in our system
/// </summary>
public class Customer
{
    public int Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? StreetAddress { get; set; }
    public string? City { get; set; }
    public string? ZipCode { get; set; }
}