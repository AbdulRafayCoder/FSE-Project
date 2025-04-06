using Microsoft.EntityFrameworkCore;
using FSEBACKEND.Data;
using FSEBACKEND.Models;

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

app.MapGet("/requests/{id}", async (int id, ApplicationDbContext db) =>
{
    var request = await db.Requests
        .GroupJoin(
            db.Books,
            req => req.BookId,
            book => book.Id,
            (req, books) => new { req, books })
        .SelectMany(
            temp => temp.books.DefaultIfEmpty(),
            (temp, book) => new { temp.req, book })
        .GroupJoin(
            db.Users,
            temp => temp.req.Email,
            user => user.Email,
            (temp, users) => new { temp.req, temp.book, users })
        .SelectMany(
            temp => temp.users.DefaultIfEmpty(),
            (temp, user) => new
            {
                RequestId = temp.req.Id,
                UserEmail = user != null ? user.Email : temp.req.Email,
                BookId = temp.book != null ? temp.book.Id : temp.req.BookId,
                BookName = temp.book != null ? temp.book.BookName : "Unknown Book",
                Accepted = temp.req.Accepted
            })
        .Where(x => x.RequestId == id)
        .FirstOrDefaultAsync();

    return request != null ? Results.Ok(request) : Results.NotFound("Request not found.");
});


app.MapPut("/requests/{id}", async (int id, Request updatedRequest, ApplicationDbContext db) =>
{
    var request = await db.Requests.FindAsync(id);
    if (request is null) return Results.NotFound("Request not found.");
    request.Accepted = true;
    var lend = new Lend
    {
        RequestId = request.Id,
        StartDate = DateTime.UtcNow,  
        EndDate = DateTime.UtcNow.AddDays(14)  
    };
    db.Lends.Add(lend);
    await db.SaveChangesAsync();

    return Results.Ok(new { Status = "Request updated and Lend added", Request = request, Lend = lend });
});

app.MapGet("/requests/unaccepted/{email}", async (string email, ApplicationDbContext db) =>
{
    var requests = await db.Requests
        .Where(request => request.Email == email && !request.Accepted)  
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

    return requests.Any() ? Results.Ok(requests) : Results.NotFound("No unaccepted requests found.");
});

app.MapGet("/requests/accepted/{email}", async (string email, ApplicationDbContext db) =>
{
    var requests = await db.Requests
        .Where(request => request.Email == email && request.Accepted)  
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
            (temp, user) => new { temp.request, temp.book, user })
        .GroupJoin(
            db.Lends,
            temp => temp.request.Id,
            lend => lend.RequestId,
            (temp, lends) => new { temp.request, temp.book, temp.user, lends })
        .SelectMany(
            temp => temp.lends.DefaultIfEmpty(),
            (temp, lend) => new
            {
                RequestId = temp.request.Id,
                UserEmail = temp.user != null ? temp.user.Email : temp.request.Email,
                BookId = temp.book != null ? temp.book.Id : temp.request.BookId,
                BookName = temp.book != null ? temp.book.BookName : "Unknown Book",
                StartDate = lend != null ? lend.StartDate : (DateTime?)null,
                EndDate = lend != null ? lend.EndDate : (DateTime?)null
            })
        .ToListAsync();

    return requests.Any() ? Results.Ok(requests) : Results.NotFound("No accepted requests found.");
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


app.Run();
record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
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