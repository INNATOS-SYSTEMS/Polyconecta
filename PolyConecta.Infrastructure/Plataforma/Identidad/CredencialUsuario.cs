using Microsoft.AspNetCore.Identity;

namespace PolyConecta.Infrastructure.Plataforma.Identidad;

/// <summary>
/// Credenciales de un usuario (R-01): Identity solo guarda usuario, contraseña y bloqueo. Apunta 1:1 a
/// la entidad de dominio <c>User</c> por <see cref="UserId"/>; el dominio no conoce Identity (CT-07).
/// </summary>
public sealed class CredencialUsuario : IdentityUser<long>
{
    public long UserId { get; set; }
}
