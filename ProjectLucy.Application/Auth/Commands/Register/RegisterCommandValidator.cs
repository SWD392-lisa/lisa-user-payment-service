using FluentValidation;
using ProjectLucy.Domain.Interfaces;

namespace ProjectLucy.Application.Auth.Commands.Register;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    private readonly IUserRepository _userRepo;

    public RegisterCommandValidator(IUserRepository userRepo)
    {
        _userRepo = userRepo;

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required")
            .MaximumLength(255).WithMessage("Full name cannot exceed 255 characters");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(255)
            .MustAsync(BeUniqueEmailAsync)
                .WithMessage("Email is already registered");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least 1 uppercase letter")
            .Matches(@"[0-9]").WithMessage("Password must contain at least 1 number")
            .Matches(@"[^a-zA-Z0-9\s]").WithMessage("Password must contain at least 1 special character");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Confirm password is required")
            .Equal(x => x.Password).WithMessage("Passwords do not match");

        RuleFor(x => x.Birthday)
            .NotEmpty().WithMessage("Birthday is required");

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(30).WithMessage("Phone number cannot exceed 30 characters")
            .Matches(@"^[\d\s\+\-\(\)]+$").WithMessage("Invalid phone number format")
            .MustAsync(BeUniquePhoneAsync)
                .WithMessage("Phone number is already registered")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
    }

    private Task<bool> BeUniqueEmailAsync(RegisterCommand cmd, string email, CancellationToken ct)
        => _userRepo.ExistsByEmailAsync(email.Trim().ToLower(), ct).ContinueWith(t => !t.Result, ct);

    private Task<bool> BeUniquePhoneAsync(RegisterCommand cmd, string? phone, CancellationToken ct)
        => _userRepo.ExistsByPhoneAsync(phone!.Trim(), ct).ContinueWith(t => !t.Result, ct);
}
