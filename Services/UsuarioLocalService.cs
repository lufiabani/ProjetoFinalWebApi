using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DesenvWebApi.Api.Data;
using DesenvWebApi.Api.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

// UsuarioLocalService.cs — espelha o utilizador autenticado na tabela Usuarios (chave KeycloakSub).
namespace DesenvWebApi.Api.Services;

public class UsuarioLocalService : IUsuarioLocalService
{
    private readonly AppDbContext _db;

    public UsuarioLocalService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Usuario> GarantirUsuarioAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        // "sub" é o identificador único do utilizador no Keycloak.
        var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? principal.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(sub))
            throw new UnauthorizedAccessException("Token sem claim 'sub'.");

        var email = principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value
            ?? principal.FindFirst("email")?.Value;
        var nome = principal.FindFirst("name")?.Value
            ?? principal.FindFirst("given_name")?.Value
            ?? principal.FindFirst("preferred_username")?.Value;

        var agora = DateTime.UtcNow;
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.KeycloakSub == sub, cancellationToken);

        // Primeiro login: insere; logins seguintes: atualiza email/nome se o IdP mudou.
        if (usuario is null)
        {
            usuario = new Usuario
            {
                KeycloakSub = sub,
                Email = email,
                NomeExibicao = nome,
                CriadoEm = agora,
                AtualizadoEm = agora
            };
            _db.Usuarios.Add(usuario);
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
                return usuario;
            }
            catch (DbUpdateException ex) when (EhViolacaoUniqueKeycloakSub(ex))
            {
                // Vários pedidos em paralelo (ex.: /me + /favoritos no primeiro acesso) podem tentar inserir ao mesmo tempo.
                _db.Entry(usuario).State = EntityState.Detached;
                usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.KeycloakSub == sub, cancellationToken);
                if (usuario is null)
                    throw;
            }
        }

        usuario.Email = email;
        usuario.NomeExibicao = nome;
        usuario.AtualizadoEm = agora;
        await _db.SaveChangesAsync(cancellationToken);
        return usuario;
    }

    // PostgreSQL 23505 — outro pedido já criou o Utilizador com o mesmo KeycloakSub.
    private static bool EhViolacaoUniqueKeycloakSub(DbUpdateException ex) =>
        ex.InnerException is PostgresException pg && pg.SqlState == PostgresErrorCodes.UniqueViolation;
}
