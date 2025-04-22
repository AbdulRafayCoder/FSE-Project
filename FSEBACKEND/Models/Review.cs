using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FSEBACKEND.Models
{
    public class Review
    {
        public int Id { get; set; }
        public int LendId { get; set; }
        
        [Required]
        public int BookId { get; set; }

        [Required]
        public string UserEmail { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Comment { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Book Book { get; set; }
    }
}
