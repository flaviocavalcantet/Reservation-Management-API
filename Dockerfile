# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Copy solution and project files
COPY ["ReservationManagement.sln", "."]
COPY ["src/Reservation.API/Reservation.API.csproj", "src/Reservation.API/"]
COPY ["src/Reservation.Application/Reservation.Application.csproj", "src/Reservation.Application/"]
COPY ["src/Reservation.Domain/Reservation.Domain.csproj", "src/Reservation.Domain/"]
COPY ["src/Reservation.Infrastructure/Reservation.Infrastructure.csproj", "src/Reservation.Infrastructure/"]
COPY ["tests/Reservation.Tests/Reservation.Tests.csproj", "tests/Reservation.Tests/"]

# Restore dependencies
RUN dotnet restore "ReservationManagement.sln"

# Copy all source code
COPY ["src/", "src/"]
COPY ["tests/", "tests/"]

# Build the solution
RUN dotnet build "ReservationManagement.sln" -c Release -o /app/build

# Publish the API
RUN dotnet publish "src/Reservation.API/Reservation.API.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app

# Install ca-certificates for SSL/TLS
RUN apt-get update && apt-get install -y ca-certificates && rm -rf /var/lib/apt/lists/*

# Copy published application from build stage
COPY --from=build /app/publish .

# Create logs directory
RUN mkdir -p /app/logs

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD dotnet --version

# Run the application
ENTRYPOINT ["dotnet", "Reservation.API.dll"]
