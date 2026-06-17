using MediatR;
using ProjectLucy.Application.DTOs.LoginDtos;
using ProjectLucy.Application.Common;

namespace ProjectLucy.Application.Auth.Commands.Login;

/// <summary>
/// Command that carries the login request data and declares its return type.
/// MediatR routes this to LoginCommandHandler.
/// </summary>
public record LoginCommand(LoginRequest Request)
    : IRequest<Result<LoginResponse>>;
