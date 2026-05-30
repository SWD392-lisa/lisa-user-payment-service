using MediatR;
using ProjectLucy.Application.Common;

namespace ProjectLucy.Application.Auth.Commands.Logout;

public record LogoutCommand(string RefreshToken) : IRequest<Result<object?>>;
