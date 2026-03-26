using FluentValidation;
using MyFO.Application.Auth.DTOs;

namespace MyFO.Application.Auth.Validators;

public class InitiateRegistrationRequestValidator : AbstractValidator<InitiateRegistrationRequest>
{
    public InitiateRegistrationRequestValidator()
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
    }
}
