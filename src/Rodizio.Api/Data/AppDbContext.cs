using Microsoft.EntityFrameworkCore;
using Rodizio.Api.Models;

namespace Rodizio.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Funcionario> Funcionarios { get; set; }
    public DbSet<Posto> Postos { get; set; }
    public DbSet<Escala> Escalas { get; set; }
    public DbSet<DiaEspecial> DiasEspeciais { get; set; }
    public DbSet<Impedimento> Impedimentos { get; set; }
    public DbSet<Servico> Servicos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Seed: as 6 escalas fixas, com Ids estáveis.
        modelBuilder.Entity<Escala>().HasData(
            new Escala { Id = 1, Nome = "Escala A" },
            new Escala { Id = 2, Nome = "Escala B" },
            new Escala { Id = 3, Nome = "Escala C" },
            new Escala { Id = 4, Nome = "Escala D" },
            new Escala { Id = 5, Nome = "Escala E" },
            new Escala { Id = 6, Nome = "Escala F" }
        );

        // Funcionario.EscalaId é chave estrangeira para Escala.
        // Sem propriedade de navegação: o JSON do funcionário mostra só o escalaId.
        // Restrict: o banco não deixa apagar uma escala que tenha funcionários.
        modelBuilder.Entity<Funcionario>()
            .HasOne<Escala>()
            .WithMany()
            .HasForeignKey(f => f.EscalaId)
            .OnDelete(DeleteBehavior.Restrict);

        
        // Um mesmo dia não pode ser cadastrado duas vezes como dia especial.
        modelBuilder.Entity<DiaEspecial>()
            .HasIndex(d => d.Data)
            .IsUnique();

        
        modelBuilder.Entity<Impedimento>(entidade =>
        {
            // Motivo gravado como texto ("AtestadoMedico"), legível para o ETL (B4-1).
            entidade.Property(i => i.Motivo).HasConversion<string>();

            // Todo impedimento pertence a um funcionário existente.
            entidade.HasOne<Funcionario>()
                .WithMany()
                .HasForeignKey(i => i.FuncionarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        modelBuilder.Entity<Servico>(entidade =>
        {
            entidade.HasOne<Escala>()
                .WithMany()
                .HasForeignKey(s => s.EscalaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Duas ligações com Funcionario: quem cumpriu e quem estava escalado (troca).
            entidade.HasOne<Funcionario>()
                .WithMany()
                .HasForeignKey(s => s.FuncionarioId)
                .OnDelete(DeleteBehavior.Restrict);

            entidade.HasOne<Funcionario>()
                .WithMany()
                .HasForeignKey(s => s.EscaladoOriginalId)
                .OnDelete(DeleteBehavior.Restrict);

            // B5-2: uma pessoa só pode ter um serviço ATIVO por dia.
            // Índice único parcial: vale só para as linhas não canceladas.
            entidade.HasIndex(s => new { s.FuncionarioId, s.Data })
                .IsUnique()
                .HasFilter("\"Cancelado\" = false");
        });        
    }
}