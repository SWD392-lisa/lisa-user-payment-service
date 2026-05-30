using MediatR;
using ProjectLucy.Application.Auth.DTOs;
using ProjectLucy.Application.Common;

namespace ProjectLucy.Application.Auth.Commands.Register;

public record RegisterCommand(
    string FullName,
    string Email,
    string Password,
    string ConfirmPassword,
    DateOnly Birthday,
    string? PhoneNumber
) : IRequest<Result<RegisterResponse>>;
