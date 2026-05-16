using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DesenvWebApi.Api.Migrations
{
    /// <inheritdoc />
    public partial class CorrigirAcentuacaoGeneros : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Corrige registos gravados com acentuação pt-PT (género/Género) para pt-BR (gênero/Gênero).
            migrationBuilder.Sql("""
                UPDATE "Generos"
                SET "Nome" = REPLACE("Nome", 'género', 'gênero')
                WHERE "Nome" LIKE '%género%';

                UPDATE "Generos"
                SET "Nome" = REPLACE("Nome", 'Género', 'Gênero')
                WHERE "Nome" LIKE '%Género%';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "Generos"
                SET "Nome" = REPLACE("Nome", 'gênero', 'género')
                WHERE "Nome" LIKE '%gênero%';

                UPDATE "Generos"
                SET "Nome" = REPLACE("Nome", 'Gênero', 'Género')
                WHERE "Nome" LIKE '%Gênero%';
                """);
        }
    }
}
