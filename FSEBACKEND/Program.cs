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