FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file first to maximize restore cache reuse.
COPY ["NuGet.Config", "./"]
COPY ["ProjectLucy.Gateway/ProjectLucy.Gateway.csproj", "ProjectLucy.Gateway/"]

RUN dotnet restore "ProjectLucy.Gateway/ProjectLucy.Gateway.csproj"

COPY . .
RUN dotnet publish "ProjectLucy.Gateway/ProjectLucy.Gateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ProjectLucy.Gateway.dll"]
