using TecnoMarket.Web.Servicios;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession(opciones =>
{
    opciones.IdleTimeout = TimeSpan.FromHours(2);
    opciones.Cookie.HttpOnly = true;
    opciones.Cookie.IsEssential = true;
});

// La web consume la MISMA API que la app de Android.
builder.Services.AddHttpClient<ApiCliente>(cliente =>
{
    cliente.BaseAddress = new Uri(
        builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5204/");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseRouting();
app.UseSession();
app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

app.Run();
