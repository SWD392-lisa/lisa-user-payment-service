using MediatR;
using ProjectLucy.Application.DTOs.LoginDtos;
using ProjectLucy.Application.DTOs.RefreshTokenDtos;
using ProjectLucy.Application.Common;

namespace ProjectLucy.Application.Auth.Commands.RefreshToken;

public record RefreshTokenCommand(RefreshTokenRequest Request)
    : IRequest<Result<LoginResponse>>;
