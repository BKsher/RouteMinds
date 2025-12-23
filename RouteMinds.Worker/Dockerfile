# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["RouteMinds.Worker/RouteMinds.Worker.csproj", "RouteMinds.Worker/"]
COPY ["RouteMinds.Domain/RouteMinds.Domain.csproj", "RouteMinds.Domain/"]
COPY ["RouteMinds.Infrastructure/RouteMinds.Infrastructure.csproj", "RouteMinds.Infrastructure/"]

# Restore
RUN dotnet restore "RouteMinds.Worker/RouteMinds.Worker.csproj"

# Copy code
COPY . .

# Build and Publish
WORKDIR "/src/RouteMinds.Worker"
RUN dotnet build "RouteMinds.Worker.csproj" -c Release -o /app/build
RUN dotnet publish "RouteMinds.Worker.csproj" -c Release -o /app/publish

# Stage 2: Run
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "RouteMinds.Worker.dll"]