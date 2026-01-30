
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// ✅ Configuración de autenticación con cookies
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", options =>
    {
        options.LoginPath = "/Account/Login";       // Ruta de inicio de sesión
        options.LogoutPath = "/Account/Logout";     // Ruta de cierre de sesión
        options.AccessDeniedPath = "/Account/AccessDenied"; // Ruta de acceso denegado
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);  // Tiempo de expiración de la cookie
        options.SlidingExpiration = true;           // Renovación automática de la cookie

        // ✅ Ajustes críticos para funcionar con IP y hostnames
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // Permite HTTP en pruebas
        options.Cookie.SameSite = SameSiteMode.Lax; // Evita problemas de redirección cruzada
    });

// Configuración de autorización
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAuthenticatedUser", policy =>
    {
        policy.RequireAuthenticatedUser();
    });
});

builder.Services.AddScoped<IDatabaseService, DatabaseService>();

var app = builder.Build();

// ✅ Middleware para entorno no desarrollo
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");

    // ⚠️ Opción: Desactivar HSTS si pruebas con IP sin HTTPS
    // app.UseHsts();
}

// ✅ Redirección HTTPS (desactiva si pruebas solo con HTTP)
app.UseHttpsRedirection();

// Archivos estáticos
app.UseStaticFiles();

// Routing
app.UseRouting();

// ✅ Middleware de autenticación y autorización
app.UseAuthentication();
app.UseAuthorization();

// ✅ Configuración de rutas
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
