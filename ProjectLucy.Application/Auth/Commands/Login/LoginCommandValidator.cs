using FluentValidation;

namespace ProjectLucy.Application.Auth.Commands.Login;

/// <summary>
/// FluentValidation validator for LoginCommand.
/// Replaces DataAnnotations on the old LoginRequest DTO.
/// Runs automatically via the ValidationBehavior MediatR pipeline.
/// </summary>
public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters");
    }
}
