using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using SistemaEscolarWeb.Data;
using SistemaEscolarWeb.Permissions;
using SistemaEscolarWeb.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    options.Filters.Add(new PermissionAuthorizationFilter());
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<PersonalService>();
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddScoped<RolService>();
builder.Services.AddScoped<PermisoRolService>();
builder.Services.AddScoped<InsumoService>();
builder.Services.AddScoped<MovimientoInsumoService>();
builder.Services.AddScoped<ReparacionService>();
builder.Services.AddScoped<BajaService>();
builder.Services.AddScoped<ImpresionService>();
builder.Services.AddScoped<SistemaEscolarWeb.Repositories.ReporteRepository>();
builder.Services.AddScoped<ReporteService>();
builder.Services.AddSingleton<SistemaEscolarWeb.Reports.PdfGenerator>();
builder.Services.AddSingleton<SistemaEscolarWeb.Reports.ExcelGenerator>();
builder.Services.AddScoped<BitacoraService>();
builder.Services.AddScoped<SistemaEscolarWeb.Repositories.ProveedorRepository>();
builder.Services.AddScoped<ProveedorService>();
builder.Services.AddScoped<SistemaEscolarWeb.Repositories.TecnologiaRepository>();
builder.Services.AddScoped<TecnologiaService>();
builder.Services.AddScoped<SistemaEscolarWeb.Repositories.AsignacionRepository>();
builder.Services.AddScoped<AsignacionService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "SistemaEscolarWeb.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
        options.SlidingExpiration = true;
        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async context =>
            {
                if (context.Principal?.Identity is not ClaimsIdentity identity || identity.IsAuthenticated != true)
                    return;

                var rol = context.Principal.FindFirst(ClaimTypes.Role)?.Value;
                if (string.IsNullOrWhiteSpace(rol))
                    return;

                var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
                var permisos = await db.Roles
                    .AsNoTracking()
                    .Where(r => r.NombreRol == rol)
                    .Join(db.RolesPermisos.AsNoTracking().Where(rp => rp.Activo),
                        rolDb => rolDb.IdRol,
                        rolPermiso => rolPermiso.IdRol,
                        (_, rolPermiso) => rolPermiso)
                    .Join(db.Permisos.AsNoTracking(),
                        rolPermiso => rolPermiso.IdPermiso,
                        permiso => permiso.IdPermiso,
                        (_, permiso) => permiso.NombrePermiso)
                    .Distinct()
                    .ToListAsync();

                foreach (var claim in identity.FindAll(PermissionNames.ClaimType).ToList())
                    identity.RemoveClaim(claim);

                identity.AddClaims(permisos.Select(permiso => new Claim(PermissionNames.ClaimType, permiso)));
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    AppPolicies.Register(options);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Account/Error");
}

app.UseStaticFiles();
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        var headers = context.Response.Headers;
        headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
        headers["Pragma"] = "no-cache";
        headers["Expires"] = "0";
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        return Task.CompletedTask;
    });

    await next();
});
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

await DbInitializer.SeedAsync(app.Services);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
