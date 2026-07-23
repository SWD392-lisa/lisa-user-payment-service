FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files first to maximize restore cache reuse.
COPY ["NuGet.Config", "./"]
COPY ["ProjectLucy.API/ProjectLucy.API.csproj", "ProjectLucy.API/"]
COPY ["ProjectLucy.Application/ProjectLucy.Application.csproj", "ProjectLucy.Application/"]
COPY ["ProjectLucy.Domain/ProjectLucy.Domain.csproj", "ProjectLucy.Domain/"]
COPY ["ProjectLucy.Infrastructure/ProjectLucy.Infrastructure.csproj", "ProjectLucy.Infrastructure/"]
COPY ["ProjectLucy.Shared/ProjectLucy.Shared.csproj", "ProjectLucy.Shared/"]

RUN dotnet restore "ProjectLucy.API/ProjectLucy.API.csproj"

COPY . .
RUN dotnet publish "ProjectLucy.API/ProjectLucy.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
# Poll for config file changes instead of inotify — avoids "inotify instance limit reached" crash on constrained containers (Render).
ENV DOTNET_USE_POLLING_FILE_WATCHER=true

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ProjectLucy.API.dll"]