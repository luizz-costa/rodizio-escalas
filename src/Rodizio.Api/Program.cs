using Rodizio.Api.Regras;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Rodizio.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Enums trafegam no JSON como texto ("ViagemAServico"), não como número.
builder.Services.AddControllers()
    .AddJsonOptions(opcoes =>
        opcoes.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));


// D-18: a calculadora da lista de prioridade, entregue por injeção de dependência.
builder.Services.AddScoped<CalculadoraPrioridade>();

builder.Services.AddScoped<GeradorDemonstracao>();

var app = builder.Build();

// Aplica as migrations pendentes ao iniciar. No container não existe o "dotnet ef";
// fora dele, substitui o "dotnet ef database update". Num banco já atualizado, não faz nada.
using (var escopoMigracao = app.Services.CreateScope())
{
    var banco = escopoMigracao.ServiceProvider.GetRequiredService<AppDbContext>();
    banco.Database.Migrate();
}


// B8: "dotnet run -- --demo" gera os dados fictícios de demonstração e encerra,
// sem subir a API. O escopo é criado à mão porque não há requisição HTTP aqui.
if (args.Contains("--demo"))
{
    using var escopo = app.Services.CreateScope();
    var gerador = escopo.ServiceProvider.GetRequiredService<GeradorDemonstracao>();
    await gerador.ExecutarAsync(new DateOnly(2026, 9, 1), DateOnly.FromDateTime(DateTime.Now));
    return;
}

if (app.Environment.IsDevelopment())
{
    // forçando o formato V2 que a interface nunca recusa
    app.UseSwagger(options =>
    {
        options.SerializeAsV2 = true;
    });

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "API Gestão de Escalas v1");
    });
}

// D-19: a própria API entrega as telas (pasta wwwroot). Mesma origem: sem CORS.
app.UseDefaultFiles();   // "/" abre o index.html
app.UseStaticFiles();    // entrega os arquivos html, css e js da wwwroot

// Sem UseHttpsRedirection para evitar bloqueios de rede no VS Code
app.MapControllers();

app.Run();
