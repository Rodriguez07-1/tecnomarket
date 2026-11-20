using Microsoft.OpenApi;
using TecnoMarket.Api;
using TecnoMarket.Api.Data;
using TecnoMarket.Api.Repositorios;
using TecnoMarket.Api.Seguridad;
using TecnoMarket.Api.Servicios;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Swagger UI: documentación interactiva de la MISMA API que usan Android y Web.
builder.Services.AddSwaggerGen(opciones =>
{
    opciones.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TecnoMarket API",
        Version = "v1",
        Description = "Una sola API C# para la app Android, la Web y el panel de analítica. " +
                      "Rutas compatibles con Supabase (/auth/v1, /rest/v1) sobre la BD existente."
    });

    var esquema = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Pegá aquí el access_token que devuelve POST /auth/v1/token. " +
                      "Las rutas de administrador exigen la cuenta rr4439210@gmail.com."
    };
    opciones.AddSecurityDefinition("Bearer", esquema);
    opciones.AddSecurityRequirement(documento => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", documento)] = new List<string>()
    });
});

builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();

// La misma base de datos de Supabase, sin tablas redundantes.
builder.Services.AddSingleton<SupabaseDb>();
builder.Services.AddScoped<CatalogoRepositorio>();
builder.Services.AddScoped<PedidoRepositorio>();
builder.Services.AddScoped<FavoritoRepositorio>();
builder.Services.AddScoped<UsuarioRepositorio>();
builder.Services.AddScoped<SesionSupabase>();
builder.Services.AddScoped<SupabaseRest>();

// Cliente para el puente de Auth, validación de JWT y fotos en Storage.
var urlSupabase = builder.Configuration["Supabase:Url"] ?? "https://uvllrtzcatnrfopjtohs.supabase.co";
var claveSupabase = builder.Configuration["Supabase:PublishableKey"] ?? "";
builder.Services.AddHttpClient("supabase", cliente =>
{
    cliente.BaseAddress = new Uri(urlSupabase);
    cliente.DefaultRequestHeaders.Add("apikey", claveSupabase);
    cliente.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Permite que la página web (otro puerto) y el celular consuman la API.
builder.Services.AddCors(opciones =>
{
    opciones.AddPolicy("Todos", politica => politica
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Swagger queda disponible también en producción/demo ( SWAGGER /swagger ).
app.UseSwagger();
app.UseSwaggerUI(opciones =>
{
    opciones.SwaggerEndpoint("/swagger/v1/swagger.json", "TecnoMarket API v1");
    opciones.RoutePrefix = "swagger";
    opciones.DocumentTitle = "TecnoMarket API · Swagger";
});

app.UseCors("Todos");
app.Use(async (context, next) =>
{
    context.Request.EnableBuffering();
    try
    {
        await next();
    }
    catch (InvalidOperationException error)
    {
        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = 503;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                System.Text.Json.JsonSerializer.Serialize(new { mensaje = error.Message }));
        }
    }
});
app.UseHttpsRedirection();
app.UseSesionTecnoMarket();
app.MapControllers();

app.Run();
