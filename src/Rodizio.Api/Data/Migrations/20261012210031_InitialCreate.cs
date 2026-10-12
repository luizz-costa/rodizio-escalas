using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Rodizio.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DiasEspeciais",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    Descricao = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiasEspeciais", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Escalas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Escalas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Postos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Descricao = table.Column<string>(type: "text", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Postos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Funcionarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Matricula = table.Column<string>(type: "text", nullable: false),
                    Cargo = table.Column<string>(type: "text", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    EscalaId = table.Column<int>(type: "integer", nullable: true),
                    Antiguidade = table.Column<int>(type: "integer", nullable: true),
                    PodeSegunda = table.Column<bool>(type: "boolean", nullable: false),
                    PodeTerca = table.Column<bool>(type: "boolean", nullable: false),
                    PodeQuarta = table.Column<bool>(type: "boolean", nullable: false),
                    PodeQuinta = table.Column<bool>(type: "boolean", nullable: false),
                    PodeSexta = table.Column<bool>(type: "boolean", nullable: false),
                    PodeSabado = table.Column<bool>(type: "boolean", nullable: false),
                    PodeDomingo = table.Column<bool>(type: "boolean", nullable: false),
                    PodeFeriado = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Funcionarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Funcionarios_Escalas_EscalaId",
                        column: x => x.EscalaId,
                        principalTable: "Escalas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Impedimentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FuncionarioId = table.Column<int>(type: "integer", nullable: false),
                    Motivo = table.Column<string>(type: "text", nullable: false),
                    DataInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    DataFim = table.Column<DateOnly>(type: "date", nullable: true),
                    Cancelado = table.Column<bool>(type: "boolean", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Impedimentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Impedimentos_Funcionarios_FuncionarioId",
                        column: x => x.FuncionarioId,
                        principalTable: "Funcionarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Servicos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    EscalaId = table.Column<int>(type: "integer", nullable: false),
                    FuncionarioId = table.Column<int>(type: "integer", nullable: false),
                    EscaladoOriginalId = table.Column<int>(type: "integer", nullable: true),
                    Cancelado = table.Column<bool>(type: "boolean", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Servicos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Servicos_Escalas_EscalaId",
                        column: x => x.EscalaId,
                        principalTable: "Escalas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Servicos_Funcionarios_EscaladoOriginalId",
                        column: x => x.EscaladoOriginalId,
                        principalTable: "Funcionarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Servicos_Funcionarios_FuncionarioId",
                        column: x => x.FuncionarioId,
                        principalTable: "Funcionarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Escalas",
                columns: new[] { "Id", "Nome" },
                values: new object[,]
                {
                    { 1, "Escala A" },
                    { 2, "Escala B" },
                    { 3, "Escala C" },
                    { 4, "Escala D" },
                    { 5, "Escala E" },
                    { 6, "Escala F" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiasEspeciais_Data",
                table: "DiasEspeciais",
                column: "Data",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Funcionarios_EscalaId",
                table: "Funcionarios",
                column: "EscalaId");

            migrationBuilder.CreateIndex(
                name: "IX_Impedimentos_FuncionarioId",
                table: "Impedimentos",
                column: "FuncionarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Servicos_EscaladoOriginalId",
                table: "Servicos",
                column: "EscaladoOriginalId");

            migrationBuilder.CreateIndex(
                name: "IX_Servicos_EscalaId",
                table: "Servicos",
                column: "EscalaId");

            migrationBuilder.CreateIndex(
                name: "IX_Servicos_FuncionarioId_Data",
                table: "Servicos",
                columns: new[] { "FuncionarioId", "Data" },
                unique: true,
                filter: "\"Cancelado\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiasEspeciais");

            migrationBuilder.DropTable(
                name: "Impedimentos");

            migrationBuilder.DropTable(
                name: "Postos");

            migrationBuilder.DropTable(
                name: "Servicos");

            migrationBuilder.DropTable(
                name: "Funcionarios");

            migrationBuilder.DropTable(
                name: "Escalas");
        }
    }
}
