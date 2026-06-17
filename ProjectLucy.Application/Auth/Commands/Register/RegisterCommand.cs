using MediatR;
using ProjectLucy.Application.DTOs.RegisterDtos;
using ProjectLucy.Application.Common;

namespace ProjectLucy.Application.Auth.Commands.Register;

public record RegisterCommand(RegisterRequest Request)
    : IRequest<Result<RegisterResponse>>;
