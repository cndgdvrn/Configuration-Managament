# Base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

# Build image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["src/DynamicConfig.Admin/DynamicConfig.Admin.csproj", "src/DynamicConfig.Admin/"]
COPY ["src/DynamicConfig.Core/DynamicConfig.Core.csproj", "src/DynamicConfig.Core/"]
RUN dotnet restore "src/DynamicConfig.Admin/DynamicConfig.Admin.csproj"

# Copy source code
COPY . .

# Build the application
WORKDIR "/src/src/DynamicConfig.Admin"
RUN dotnet build "DynamicConfig.Admin.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "DynamicConfig.Admin.csproj" -c Release -o /app/publish

# Final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "DynamicConfig.Admin.dll"] 