using System;
using FSEBACKEND.Models;
namespace FSEBACKEND.Models
{
    public class Lend
    {
        public int Id { get; set; }
        public int RequestId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsReturned { get; set; } = false;
        public Request Request { get; set; }
    }
}
