using Microsoft.AspNetCore.Builder;
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// VULNERABLE LOGIN ENDPOINT
app.MapPost("/login", (string email, string password) =>
{
    const string connectionString = "Data Source=localhost;Initial Catalog=TestDB;Integrated Security=true;";
    
    using var connection = new SqlConnection(connectionString);
    using var command = connection.CreateCommand();
    
    command.CommandText = $"SELECT * FROM USERS WHERE Email = '{email}' AND Password = '{password}';";
    
    connection.Open();
    using var reader = command.ExecuteReader();
    reader.Read();
    
    return new User(Convert.ToInt32(reader["Id"]), reader["Email"].ToString()!);
});

app.Run();