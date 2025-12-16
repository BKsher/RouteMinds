# Stage 1: Build the code
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the project files first (for better caching)
# We have to copy Domain/Infra too because API depends on them!
COPY ["RouteMinds.API/RouteMinds.API.csproj", "RouteMinds.API/"]
COPY ["RouteMinds.Domain/RouteMinds.Domain.csproj", "RouteMinds.Domain/"]
COPY ["RouteMinds.Infrastructure/RouteMinds.Infrastructure.csproj", "RouteMinds.Infrastructure/"]

# Restore dependencies (NuGet packages)
RUN dotnet restore "RouteMinds.API/RouteMinds.API.csproj"

# Copy the rest of the code
COPY . .

# Build and Publish
WORKDIR "/src/RouteMinds.API"
RUN dotnet build "RouteMinds.API.csproj" -c Release -o /app/build
RUN dotnet publish "RouteMinds.API.csproj" -c Release -o /app/publish

# Stage 2: Run the app (Small image)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "RouteMinds.API.dll"]