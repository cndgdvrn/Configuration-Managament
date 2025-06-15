# Base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

# Build image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["src/Sample.ServiceA/Sample.ServiceA.csproj", "src/Sample.ServiceA/"]
COPY ["src/DynamicConfig.Core/DynamicConfig.Core.csproj", "src/DynamicConfig.Core/"]
RUN dotnet restore "src/Sample.ServiceA/Sample.ServiceA.csproj"

# Copy source code
COPY . .

# Build the application
WORKDIR "/src/src/Sample.ServiceA"
RUN dotnet build "Sample.ServiceA.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "Sample.ServiceA.csproj" -c Release -o /app/publish

# Final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Sample.ServiceA.dll"] 