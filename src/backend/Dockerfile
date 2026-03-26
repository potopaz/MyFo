# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files first (for layer caching)
COPY MyFO.slnx ./
COPY MyFO.API/MyFO.API.csproj MyFO.API/
COPY MyFO.Application/MyFO.Application.csproj MyFO.Application/
COPY MyFO.Domain/MyFO.Domain.csproj MyFO.Domain/
COPY MyFO.Infrastructure/MyFO.Infrastructure.csproj MyFO.Infrastructure/

# Restore dependencies
RUN dotnet restore MyFO.slnx

# Copy source code and publish
COPY . .
RUN dotnet publish MyFO.API/MyFO.API.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "MyFO.API.dll"]
