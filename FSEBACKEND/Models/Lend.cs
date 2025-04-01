namespace FSEBACKEND.Models
{
    public class Lend
    {
        public int Id { get; set; }
        public int RequestId { get; set; }  // Fixed naming convention
        public DateTime? StartDate { get; set; }  // You might want to use DateTime for better handling of dates
        public DateTime? EndDate { get; set; }  // Nullable DateTime to represent the end date
    }
}