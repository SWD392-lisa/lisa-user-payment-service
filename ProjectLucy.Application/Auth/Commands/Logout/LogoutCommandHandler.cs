using MediatR;
using ProjectLucy.Application.Common;
using ProjectLucy.Application.Common.Exceptions;
using ProjectLucy.Application.Interfaces;
using ProjectLucy.Domain.Interfaces;

namespace ProjectLucy.Application.Auth.Commands.Logout;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result<object?>>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;

    public LogoutCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<object?>> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var storedToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken, cancellationToken);

        if (storedToken is null)
            throw new NotFoundException("Refresh token not found");

        if (storedToken.IsRevoked == true)
        {
            return Result<object?>.Success("Already logged out");
        }

        storedToken.IsRevoked = true;
        await _refreshTokenRepository.UpdateAsync(storedToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<object?>.Success("Logged out successfully");
    }
}
