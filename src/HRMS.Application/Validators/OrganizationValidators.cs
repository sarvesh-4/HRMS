using FluentValidation;
using HRMS.Application.DTOs.Organization;

namespace HRMS.Application.Validators;

public class CreateOrganizationValidator : AbstractValidator<CreateOrganizationDto>
{
    public CreateOrganizationValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Organization name is required.")
            .MaximumLength(200);

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Organization address is required.")
            .MaximumLength(500);
    }
}
