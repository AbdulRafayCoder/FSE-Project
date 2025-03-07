using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseHttpsRedirection();


app.MapPost("/login", (LoginRequest login) =>
{
    var filePath = "./database/user.json";
    if (!File.Exists(filePath))
    {
        return Results.BadRequest("Users file not found.");
    }
    
    var json = File.ReadAllText(filePath);
    var users = JsonSerializer.Deserialize<List<User>>(json);
    
    User? matchingUser = null;
    if (users != null)
    {
        foreach (var user in users)
        {
            if (user.Email == login.Email && user.Password == login.Password)
            {
                matchingUser = user;
                break;
            }
        }
    }
    
    if (matchingUser != null)
    {
        return Results.Ok(new { Status = true, Email = matchingUser.Email, Role = matchingUser.Role });
    }
    else
    {
        return Results.Ok(new { Status = false, Email = (string?)null, Role = (string?)null });
    }
});


app.MapPost("/adduser", (AddUserRequest newUser) =>
{
    if (string.IsNullOrWhiteSpace(newUser.Email) || string.IsNullOrWhiteSpace(newUser.Password))
    {
        return Results.BadRequest("Email and password are required.");
    }

    var filePath = "./database/user.json";
    List<User> users;
    if (File.Exists(filePath))
    {
        var json = File.ReadAllText(filePath);
        users = JsonSerializer.Deserialize<List<User>>(json) ?? new List<User>();
    }
    else
    {
        users = new List<User>();
    }
    
    var user = new User(newUser.Email, newUser.Password, "Student");
    users.Add(user);
    var newJson = JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(filePath, newJson);
    
    return Results.Ok(new { Status = "User added", User = user });
});

app.MapPost("/addbook", (AddBookRequest newBook) =>
{
    if (newBook.BookId <= 0 || 
        string.IsNullOrWhiteSpace(newBook.BookName) || 
        string.IsNullOrWhiteSpace(newBook.WriteName) || 
        string.IsNullOrWhiteSpace(newBook.PublishDate))
    {
        return Results.BadRequest("BookId (greater than 0), BookName, WriteName, and PublishDate are required.");
    }
    
    var filePath = "./database/book.json";
    List<Book> books;
    if (File.Exists(filePath))
    {
        var json = File.ReadAllText(filePath);
        books = JsonSerializer.Deserialize<List<Book>>(json) ?? new List<Book>();
    }
    else
    {
        books = new List<Book>();
    }
    
    var book = new Book(newBook.BookId, newBook.BookName, newBook.WriteName, newBook.PublishDate);
    books.Add(book);
    
    var newJson = JsonSerializer.Serialize(books, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(filePath, newJson);
    
    return Results.Ok(new { Status = "Book added", Book = book });
});


app.MapGet("/books", () =>
{
    var filePath = "./database/book.json";
    if (!File.Exists(filePath))
    {
        return Results.Ok(new List<Book>());
    }
    var json = File.ReadAllText(filePath);
    var books = JsonSerializer.Deserialize<List<Book>>(json) ?? new List<Book>();
    return Results.Ok(books);
});


app.Run();
record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
record LoginRequest(string Email, string Password);
record AddUserRequest(string Email, string Password);
record User(string Email, string Password, string Role);
record AddBookRequest(int BookId, string BookName, string WriteName, string PublishDate);
record Book(int BookId, string BookName, string WriteName, string PublishDate);