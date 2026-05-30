using MediatR;
using ProjectLucy.Application.Auth.DTOs;
using ProjectLucy.Application.Common;

namespace ProjectLucy.Application.Auth.Commands.RefreshToken;

public record RefreshTokenCommand(string RefreshToken)
    : IRequest<Result<LoginResponse>>;
