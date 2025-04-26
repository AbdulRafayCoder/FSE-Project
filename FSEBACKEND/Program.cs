using FSEBACKEND.Data;
using FSEBACKEND.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPost("/login", async (LoginRequest login, ApplicationDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(login.Email) || string.IsNullOrWhiteSpace(login.Password))
    {
        return Results.BadRequest("Email and password are required.");
    }

    if (!Regex.IsMatch(login.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
    {
        return Results.BadRequest("Invalid email format.");
    }

    if (login.Password.Length < 8 || !login.Password.Any(char.IsDigit))
    {
        return Results.BadRequest("Password must be at least 8 characters long and contain at least one numeral.");
    }

    var user = await db.Users.SingleOrDefaultAsync(u => u.Email == login.Email && u.Password == login.Password);
    if (user != null)
    {
        return Results.Ok(new { Status = true, Email = user.Email, Role = user.Role });
    }
    else
    {
        return Results.Ok(new { Status = false, Email = (string?)null, Role = (string?)null });
    }
});


app.MapPost("/adduser", async (AddUserRequest newUser, ApplicationDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(newUser.Email) || string.IsNullOrWhiteSpace(newUser.Password))
    {
        return Results.BadRequest("Email and password are required.");
    }

    if (!Regex.IsMatch(newUser.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
    {
        return Results.BadRequest("Invalid email format.");
    }

    if (newUser.Password.Length < 8 || !newUser.Password.Any(char.IsDigit))
    {
        return Results.BadRequest("Password must be at least 8 characters long and contain at least one numeral.");
    }

    var user = new User(newUser.Email, newUser.Password, "student");
    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Results.Ok(new { Status = "User added", User = user });
});

app.MapPost("/addbook", async (AddBookRequest newBook, ApplicationDbContext db) =>
{
    if (newBook.BookId <= 0 || 
        string.IsNullOrWhiteSpace(newBook.BookName) || 
        string.IsNullOrWhiteSpace(newBook.WriteName) || 
        string.IsNullOrWhiteSpace(newBook.PublishDate))
    {
        return Results.BadRequest("BookId (greater than 0), BookName, WriteName, and PublishDate are required.");
    }

    var book = new Book(newBook.BookId, newBook.BookName, newBook.WriteName, newBook.PublishDate);
    db.Books.Add(book);
    await db.SaveChangesAsync();

    return Results.Ok(new { Status = "Book added", Book = book });
});

app.MapGet("/books", async (ApplicationDbContext db) =>
{
    var books = await db.Books.ToListAsync();
    return Results.Ok(books);
});


app.MapPost("/addrequest", async (Request newRequest, ApplicationDbContext db) =>
{
    if (newRequest.BookId <= 0 || string.IsNullOrWhiteSpace(newRequest.Email))
    {
        return Results.BadRequest("BookId (greater than 0) and Email are required.");
    }

    db.Requests.Add(newRequest);
    await db.SaveChangesAsync();
    return Results.Ok(new { Status = "Request added", Request = newRequest });
});

app.MapGet("/requests", async (ApplicationDbContext db) =>
{
    var requests = await db.Requests
        .GroupJoin(
            db.Books,
            request => request.BookId,
            book => book.Id, 
            (request, books) => new { request, books })
        .SelectMany(
            temp => temp.books.DefaultIfEmpty(),
            (temp, book) => new { temp.request, book })
        .GroupJoin(
            db.Users,
            temp => temp.request.Email,
            user => user.Email,
            (temp, users) => new { temp.request, temp.book, users })
        .SelectMany(
            temp => temp.users.DefaultIfEmpty(),
            (temp, user) => new
            {
                RequestId = temp.request.Id,
                UserEmail = user != null ? user.Email : temp.request.Email,
                BookId = temp.book != null ? temp.book.Id : temp.request.BookId, // ✅ Fix field name
                BookName = temp.book != null ? temp.book.BookName : "Unknown Book",
                Accepted = temp.request.Accepted
            })
        .Distinct()
        .ToListAsync();

    return Results.Ok(requests);
});

// app.MapGet("/requests/{id}", async (int id, ApplicationDbContext db) =>
// {
//     var request = await db.Requests
//         .GroupJoin(
//             db.Books,
//             req => req.BookId,
//             book => book.Id,
//             (req, books) => new { req, books })
//         .SelectMany(
//             temp => temp.books.DefaultIfEmpty(),
//             (temp, book) => new { temp.req, book })
//         .GroupJoin(
//             db.Users,
//             temp => temp.req.Email,
//             user => user.Email,
//             (temp, users) => new { temp.req, temp.book, users })
//         .SelectMany(
//             temp => temp.users.DefaultIfEmpty(),
//             (temp, user) => new
//             {
//                 RequestId = temp.req.Id,
//                 UserEmail = user != null ? user.Email : temp.req.Email,
//                 BookId = temp.book != null ? temp.book.Id : temp.req.BookId,
//                 BookName = temp.book != null ? temp.book.BookName : "Unknown Book",
//                 Accepted = temp.req.Accepted
//             })
//         .Where(x => x.RequestId == id)
//         .FirstOrDefaultAsync();

//     return request != null ? Results.Ok(request) : Results.NotFound("Request not found.");
// });


app.MapPut("/requests/{id}", async (int id, Request updatedRequest, ApplicationDbContext db) =>
{
    var request = await db.Requests.FindAsync(id);
    if (request is null) return Results.NotFound("Request not found.");
    request.Accepted = true;
    var lend = new Lend
    {
        RequestId = request.Id,
        StartDate = DateTime.UtcNow,  
        EndDate = DateTime.UtcNow.AddDays(-1)  
    };
    db.Lends.Add(lend);
    await db.SaveChangesAsync();

    return Results.Ok(new { Status = "Request updated and Lend added", Request = request, Lend = lend });
});

app.MapGet("/requests/{email}", async (string email, ApplicationDbContext db) =>
{
    var requests = await db.Requests
        .Where(request => request.Email == email)
        .Where(request =>
            !db.Lends.Any(lend => lend.RequestId == request.Id && lend.IsReturned)) // 🛑 Exclude returned ones
        .GroupJoin(
            db.Books,
            request => request.BookId,
            book => book.Id,
            (request, books) => new { request, books })
        .SelectMany(
            temp => temp.books.DefaultIfEmpty(),
            (temp, book) => new { temp.request, book })
        .GroupJoin(
            db.Users,
            temp => temp.request.Email,
            user => user.Email,
            (temp, users) => new { temp.request, temp.book, users })
        .SelectMany(
            temp => temp.users.DefaultIfEmpty(),
            (temp, user) => new
            {
                RequestId = temp.request.Id,
                UserEmail = user != null ? user.Email : temp.request.Email,
                UserName = user != null ? user.Email : "Unknown User",
                BookId = temp.book != null ? temp.book.Id : temp.request.BookId,
                BookName = temp.book != null ? temp.book.BookName : "Unknown Book",
                Accepted = temp.request.Accepted
            })
        .ToListAsync();

    return requests.Any()
        ? Results.Ok(requests)
        : Results.NotFound("No unaccepted or active requests found.");
});


app.MapDelete("/books/{id}", async (int id, ApplicationDbContext db) =>
{
    var book = await db.Books.FindAsync(id);
    if (book == null)
    {
        return Results.NotFound("Book not found.");
    }

    db.Books.Remove(book);
    await db.SaveChangesAsync();
    return Results.Ok(new { Status = "Book deleted", BookId = id });
});

app.MapDelete("/users/{email}", async (string email, ApplicationDbContext db) =>
{
    var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email);
    if (user == null)
    {
        return Results.NotFound("User not found.");
    }

    db.Users.Remove(user);
    await db.SaveChangesAsync();
    return Results.Ok(new { Status = "User deleted", Email = email });
});


app.MapPut("/lend/return/{requestId}", async (int requestId, ApplicationDbContext db) =>
{
    var lend = await db.Lends.FirstOrDefaultAsync(l => l.RequestId == requestId);

    if (lend == null)
    {
        return Results.NotFound($"No lend record found for request ID {requestId}.");
    }

    lend.IsReturned = true;
    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        message = $"Lend record with request ID {requestId} marked as returned.",
        lend.Id,
        lend.RequestId,
        lend.IsReturned
    });
});

app.MapPost("/challan/generate", async (ApplicationDbContext db) =>
{
    var today = DateTime.UtcNow.Date;

    var overdueLends = await db.Lends
        .Where(l => !l.IsReturned && l.EndDate.HasValue && l.EndDate.Value.Date < today)
        .ToListAsync();

    if (!overdueLends.Any())
    {
        return Results.Ok("No overdue lends found.");
    }

    foreach (var lend in overdueLends)
    {
        // Check if a challan already exists for this lend to avoid duplicates
        var challanExists = await db.Challans.AnyAsync(c => c.LendId == lend.Id);
        if (!challanExists)
        {
            var challan = new Challan
            {
                LendId = lend.Id,
                Amount = 1000
            };

            db.Challans.Add(challan);
        }
    }

    await db.SaveChangesAsync();

    return Results.Ok($"{overdueLends.Count} challans processed.");
});

app.MapGet("/challans", async (ApplicationDbContext db) =>
{
    var challans = await db.Challans
        .Include(c => c.Lend)
        .Select(c => new
        {
            c.Id,
            c.Amount,
            c.IssuedDate,
            LendId = c.LendId,
            RequestId = c.Lend.RequestId,
            submitted = c.SubmittedByUser  ,
            verified = c . VerifiedByAdmin
        })
        .ToListAsync();

    return Results.Ok(challans);
});

app.MapGet("/challans/user/{email}", async (string email, ApplicationDbContext db) =>
{
    var challans = await db.Challans
        .Include(c => c.Lend)
        .ThenInclude(l => l.Request)
        .Where(c => c.Lend.Request.Email == email)
        .Select(c => new
        {
            ChallanId = c.Id,
            c.LendId,
            c.Amount,
            c.IssuedDate,
            c.SubmittedByUser,
            c.VerifiedByAdmin
        })
        .ToListAsync();

    return challans.Any()
        ? Results.Ok(challans)
        : Results.NotFound($"No challans found for user with email: {email}");
});

app.MapPost("/challans/submit/{challanId}", async (int challanId, ApplicationDbContext db) =>
{
    var challan = await db.Challans.FindAsync(challanId);
    if (challan == null)
        return Results.NotFound("Challan not found.");

    challan.SubmittedByUser = true;
    await db.SaveChangesAsync();

    return Results.Ok("Challan marked as submitted by user.");
});

app.MapPost("/challans/verify/{challanId}", async (int challanId, ApplicationDbContext db) =>
{
    var challan = await db.Challans.FindAsync(challanId);
    if (challan == null)
        return Results.NotFound("Challan not found.");

    if (!challan.SubmittedByUser)
        return Results.BadRequest("User has not submitted this challan yet.");

    challan.VerifiedByAdmin = true;
    await db.SaveChangesAsync();

    return Results.Ok("Challan verified by admin.");
});

app.MapGet("/books/user/{email}/not-reviewed", async (string email, ApplicationDbContext db) =>
{
    // Get all books the user has lent and returned
    var lentBooks = await db.Lends
        .Where(l => l.Request.Email == email && l.IsReturned)
        .Select(l => new { l.Id, l.Request.BookId })
        .ToListAsync();

    // Get all book IDs the user has reviewed
    var reviewedBookIds = await db.Reviews
        .Where(r => r.UserEmail == email)
        .Select(r => r.BookId)
        .ToListAsync();

    // Filter out books already reviewed
    var booksToReview = lentBooks
        .Where(l => !reviewedBookIds.Contains(l.BookId))
        .ToList();

    // Fetch the corresponding book details
    var bookIdsToFetch = booksToReview.Select(b => b.BookId).ToList();
    var books = await db.Books
        .Where(b => bookIdsToFetch.Contains(b.BookId))
        .ToListAsync();

    // Combine data
    var result = booksToReview
        .Select(lb =>
        {
            var book = books.FirstOrDefault(b => b.BookId == lb.BookId);
            return new
            {
                id = lb.Id,
                bookId = book.Id,
                bookName = book.BookName,
                writeName = book.WriteName,
                publishDate = book.PublishDate 
            };
        })
        .ToList();

    return result.Any() ? Results.Ok(result) : Results.Ok("No books found that the user has read but not reviewed.");
});

app.MapPost("/books/review", async (Review review, ApplicationDbContext db) =>
{
    // Ensure the book exists
    var bookExists = await db.Books.AnyAsync(b => b.BookId == review.BookId);
    if (!bookExists)
    {
        return Results.NotFound($"Book with ID {review.BookId} not found.");
    }

    // Ensure the user has borrowed the book (via the "Lend" table)
    var userHasLentBook = await db.Lends
                                  .AnyAsync(l => l.Request.Email == review.UserEmail && l.Request.BookId == review.BookId && l.IsReturned);
    if (!userHasLentBook)
    {
        return Results.BadRequest("User must have lent the book before submitting a review.");
    }

    // Create and save the review
    review.CreatedAt = DateTime.UtcNow; // Ensure the review creation date is set
    db.Reviews.Add(review);
    await db.SaveChangesAsync();

    return Results.Ok(new { message = "Review successfully added.", review });
});


app.MapGet("/admin/reviews", async (ApplicationDbContext db) =>
{
    var reviews = await db.Reviews
        .Include(r => r.Book)
        .Select(r => new
        {
            ReviewId = r.Id,
            r.BookId,
            BookName = r.Book.BookName,
            WriterName = r.Book.WriteName,
            r.Comment,
            r.Rating
        })
        .ToListAsync();

    return reviews.Any()
        ? Results.Ok(reviews)
        : Results.NotFound("No reviews found.");
});


app.Run();
record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public class ReviewDto
{
    public int BookId { get; set; }
    public string UserEmail { get; set; }
    public string Comment { get; set; }
    public int Rating { get; set; } 
}

public record LoginRequest
{
    public required string Email { get; init; }
    public required string Password { get; init; }

    public LoginRequest() { }

    public LoginRequest(string email, string password)
    {
        Email = email;
        Password = password;
    }
}
public record AddUserRequest(string Email, string Password);
public record AddBookRequest(int BookId, string BookName, string WriteName, string PublishDate);