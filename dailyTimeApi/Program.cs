using dailyTimeApi.Data;
using dailyTimeApi.Extensions;
using dailyTimeApi.Models.Entities;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var corsOrigins = builder.Configuration["Cors:Origins"]
    ?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    ?? ["http://localhost:4010"];

builder.Services.AddCors(o => o.AddPolicy("DevFront", p =>
    p.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDailyTimeServices();

var app = builder.Build();

var ensureCreated = app.Configuration.GetValue("Database:EnsureCreated", false);
var seedMinimal = app.Configuration.GetValue("Database:SeedMinimal", false);

if (ensureCreated || seedMinimal)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (ensureCreated)
    {
        db.Database.EnsureCreated();
    }

    if (seedMinimal)
    {
        SeedMinimalCatalogs(db);
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var disableHttpsRedirect = app.Configuration.GetValue("DisableHttpsRedirection", false);
if (!disableHttpsRedirect)
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();
app.UseCors("DevFront");
app.MapControllers();
app.Run();

static void SeedMinimalCatalogs(AppDbContext db)
{
    if (!db.WorkItemStatuses.Any())
    {
        db.WorkItemStatuses.AddRange(
            new WorkItemStatus
            {
                Name = "Pendiente",
                Description = "Trabajo registrado que todavía no ha comenzado.",
                Color = "#64748B",
                IsFinal = false,
                ItemType = "task",
                CreatedAt = DateTime.UtcNow,
            },
            new WorkItemStatus
            {
                Name = "En progreso",
                Description = "Tarea en curso.",
                Color = "#3B82F6",
                IsFinal = false,
                ItemType = "task",
                CreatedAt = DateTime.UtcNow,
            },
            new WorkItemStatus
            {
                Name = "Bloqueada",
                Description = "Tarea detenida por un impedimento.",
                Color = "#EF4444",
                IsFinal = false,
                ItemType = "task",
                CreatedAt = DateTime.UtcNow,
            },
            new WorkItemStatus
            {
                Name = "Finalizada",
                Description = "Tarea completada.",
                Color = "#22C55E",
                IsFinal = true,
                ItemType = "task",
                CreatedAt = DateTime.UtcNow,
            },
            new WorkItemStatus
            {
                Name = "Borrador",
                Description = "Nota en elaboración, aún no lista.",
                Color = "#64748B",
                IsFinal = false,
                ItemType = "note",
                CreatedAt = DateTime.UtcNow,
            },
            new WorkItemStatus
            {
                Name = "Activa",
                Description = "Nota vigente / en uso.",
                Color = "#3B82F6",
                IsFinal = false,
                ItemType = "note",
                CreatedAt = DateTime.UtcNow,
            },
            new WorkItemStatus
            {
                Name = "Archivada",
                Description = "Nota cerrada o archivada.",
                Color = "#22C55E",
                IsFinal = true,
                ItemType = "note",
                CreatedAt = DateTime.UtcNow,
            });
    }

    if (!db.WorkItemCategories.Any())
    {
        db.WorkItemCategories.AddRange(
            new WorkItemCategory { Name = "General", Description = "Categoría por defecto de tareas.", ItemType = "task", IsActive = true, CreatedAt = DateTime.UtcNow },
            new WorkItemCategory { Name = "Trabajo", Description = "Tareas laborales.", ItemType = "task", IsActive = true, CreatedAt = DateTime.UtcNow },
            new WorkItemCategory { Name = "Personal", Description = "Tareas personales.", ItemType = "task", IsActive = true, CreatedAt = DateTime.UtcNow },
            new WorkItemCategory { Name = "Proyecto", Description = "Tareas ligadas a un proyecto.", ItemType = "task", IsActive = true, CreatedAt = DateTime.UtcNow },
            new WorkItemCategory { Name = "Reunión", Description = "Preparación o seguimiento de reunión.", ItemType = "task", IsActive = true, CreatedAt = DateTime.UtcNow },
            new WorkItemCategory { Name = "Seguimiento", Description = "Pendientes de seguimiento.", ItemType = "task", IsActive = true, CreatedAt = DateTime.UtcNow },
            new WorkItemCategory { Name = "Recordatorio", Description = "Recordatorios puntuales.", ItemType = "task", IsActive = true, CreatedAt = DateTime.UtcNow },
            new WorkItemCategory { Name = "General", Description = "Categoría por defecto de notas.", ItemType = "note", IsActive = true, CreatedAt = DateTime.UtcNow },
            new WorkItemCategory { Name = "Idea", Description = "Ideas y brainstorming.", ItemType = "note", IsActive = true, CreatedAt = DateTime.UtcNow },
            new WorkItemCategory { Name = "Reunión", Description = "Apuntes de reunión.", ItemType = "note", IsActive = true, CreatedAt = DateTime.UtcNow },
            new WorkItemCategory { Name = "Referencia", Description = "Referencias y enlaces útiles.", ItemType = "note", IsActive = true, CreatedAt = DateTime.UtcNow },
            new WorkItemCategory { Name = "Documentación", Description = "Notas de documentación.", ItemType = "note", IsActive = true, CreatedAt = DateTime.UtcNow },
            new WorkItemCategory { Name = "Decisión", Description = "Decisiones tomadas.", ItemType = "note", IsActive = true, CreatedAt = DateTime.UtcNow },
            new WorkItemCategory { Name = "Diario", Description = "Notas tipo diario / bitácora.", ItemType = "note", IsActive = true, CreatedAt = DateTime.UtcNow });
    }

    db.SaveChanges();
}
