using FluentValidation;
using MyFO.Application.Auth.DTOs;

namespace MyFO.Application.Auth.Validators;

public class CreateFamilyRequestValidator : AbstractValidator<CreateFamilyRequest>
{
    public CreateFamilyRequestValidator()
    {
        RuleFor(x => x.Name)
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
