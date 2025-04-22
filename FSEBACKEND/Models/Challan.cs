using System;
using FSEBACKEND.Models;
public class Challan
{
    public int Id { get; set; }
    public int LendId { get; set; }
    public decimal Amount { get; set; }
    public DateTime IssuedDate { get; set; } = DateTime.UtcNow;

    public bool SubmittedByUser { get; set; } = false;
    public bool VerifiedByAdmin { get; set; } = false;

    public Lend Lend { get; set; }
}
