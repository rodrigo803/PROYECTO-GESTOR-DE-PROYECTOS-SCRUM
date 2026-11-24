using Gestor_proyectos_scrum.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScrumApi();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5265"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("MvcCors", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Middleware global de manejo de errores para traducir MySqlException a respuestas JSON legibles.
// Corriendo sola, toda la app es la API, así que no hace falta acotarlo con UseWhen.
app.UseScrumApiExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("MvcCors");

app.MapScrumApi();

app.Run();
