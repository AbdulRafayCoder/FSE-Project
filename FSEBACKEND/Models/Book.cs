namespace FSEBACKEND.Models
{
    public class Book
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public string BookName { get; set; } = string.Empty;
        public string WriteName { get; set; } = string.Empty;
        public string PublishDate { get; set; } = string.Empty;

        public Book() { }

        public Book(int bookId, string bookName, string writeName, string publishDate)
        {
            BookId = bookId;
            BookName = bookName;
            WriteName = writeName;
            PublishDate = publishDate;
        }
    }
}