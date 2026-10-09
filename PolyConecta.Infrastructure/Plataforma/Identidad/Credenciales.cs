using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PolyConecta.Application.Common;
using PolyConecta.Application.Plataforma.Seguridad;

namespace PolyConecta.Infrastructure.Plataforma.Identidad;

/// <summary>Contraseñas con Identity (R-01), en la misma transacción del caso de uso. La política rota es 400.</summary>
public sealed class Credenciales(UserManager<CredencialUsuario> usuarios) : ICredenciales
{
    public async Task CrearAsync(long userId, string usuario, string contrasena, CancellationToken cancellationToken = default)
    {
        var r = await usuarios.CreateAsync(new CredencialUsuario { UserName = usuario, UserId = userId, LockoutEnabled = true }, contrasena);
        Exigir(r);
    }

    public async Task RestablecerAsync(long userId, string contrasena, CancellationToken cancellationToken = default)
    {
        var credencial = await usuarios.Users.SingleOrDefaultAsync(c => c.UserId == userId, cancellationToken)
            ?? throw new KeyNotFoundException("El usuario no tiene credenciales.");
        // Se valida primero: si la nueva no cumple la política, no se quita la anterior.
        foreach (var validador in usuarios.PasswordValidators)
            Exigir(await validador.ValidateAsync(usuarios, credencial, contrasena));
        Exigir(await usuarios.RemovePasswordAsync(credencial));
        Exigir(await usuarios.AddPasswordAsync(credencial, contrasena));
        // Restablecer también lo desbloquea.
        await usuarios.SetLockoutEndDateAsync(credencial, null);
        await usuarios.ResetAccessFailedCountAsync(credencial);
    }

    private static void Exigir(IdentityResult r)
    {
        if (!r.Succeeded)
            throw new ValidacionException(r.Errors.Select(e => new ErrorValidacion("contrasena", Traducir(e))).ToList());
    }

    private static string Traducir(IdentityError e) => e.Code switch
    {
        "PasswordTooShort" => "La contraseña tiene menos de 8 caracteres.",
        "PasswordRequiresUpper" => "La contraseña lleva al menos una mayúscula.",
        "PasswordRequiresDigit" => "La contraseña lleva al menos un número.",
        "DuplicateUserName" => "Ya existe ese usuario.",
        "InvalidUserName" => "El usuario solo lleva letras, números y . _ - @ +.",
        _ => e.Description,
    };
}
