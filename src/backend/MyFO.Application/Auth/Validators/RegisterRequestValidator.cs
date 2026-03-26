using FluentValidation;
using MyFO.Application.Auth.DTOs;

namespace MyFO.Application.Auth.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es requerido.")
            .EmailAddress().WithMessage("El email no es válido.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es requerida.")
            .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("El nombre completo es requerido.")
            .MaximumLength(200);

        RuleFor(x => x.FamilyName)
            .NotEmpty().WithMessage("El nombre de la familia es requerido.")
            .MaximumLength(100);

        RuleFor(x => x.PrimaryCurrencyCode)
            .NotEmpty().WithMessage("La moneda principal es requerida.")
            .Length(3).WithMessage("El código de moneda debe tener 3 caracteres (ej: ARS, USD).");

        RuleFor(x => x.SecondaryCurrencyCode)
            .NotEmpty().WithMessage("La moneda secundaria es requerida.")
            .Length(3).WithMessage("El código de moneda debe tener 3 caracteres (ej: ARS, USD).");
    }
}
