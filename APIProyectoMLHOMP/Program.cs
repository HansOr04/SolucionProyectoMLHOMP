using APIProyectoMLHOMP.Data;
using APIProyectoMLHOMP.Controllers;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSqlServer<ProyectoContextPloContext>(builder.Configuration.GetConnectionString("FlatConnection"));

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapUserEndpoints();

app.MapApartmentEndpoints();

app.MapBookingEndpoints();

app.MapReviewEndpoints();

app.Run();
