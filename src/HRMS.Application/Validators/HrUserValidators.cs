using FluentValidation;
using HRMS.Application.DTOs.HrUser;

namespace HRMS.Application.Validators;

public class CreateHrUserValidator : AbstractValidator<CreateHrUserDto>
{
    public CreateHrUserValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");

        RuleFor(x => x.OrganizationId)
            .NotEqual(Guid.Empty).WithMessage("OrganizationId, if provided, must be a valid id.")
            .When(x => x.OrganizationId.HasValue);
    }
}
