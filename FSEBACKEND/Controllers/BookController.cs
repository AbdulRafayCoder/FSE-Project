using Microsoft.AspNetCore.Mvc;
using FSEBACKEND.Data;
using FSEBACKEND.Models;

namespace FSEBACKEND.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BookController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BookController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("addbook")]
        public IActionResult AddBook(AddBookRequest newBook)
        {
            if (newBook.BookId <= 0 || 
                string.IsNullOrWhiteSpace(newBook.BookName) || 
                string.IsNullOrWhiteSpace(newBook.WriteName) || 
                string.IsNullOrWhiteSpace(newBook.PublishDate))
            {
                return BadRequest("BookId (greater than 0), BookName, WriteName, and PublishDate are required.");
            }

            var book = new Book(newBook.BookId, newBook.BookName, newBook.WriteName, newBook.PublishDate);
            _context.Books.Add(book);
            _context.SaveChanges();

            return Ok(new { Status = "Book added", Book = book });
        }

        [HttpGet("books")]
        public IActionResult GetBooks()
        {
            var books = _context.Books.ToList();
            return Ok(books);
        }
    }
}