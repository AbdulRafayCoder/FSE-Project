namespace FSEBACKEND.Models
{
    public class Request
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public string Email { get; set; } = string.Empty;
        public bool Accepted { get; set; }

        public Request() { }
        public Request(int bookId, string email, bool accepted)
        {
            BookId = bookId;
            Email = email;
            Accepted = accepted;
        }
    }
}
