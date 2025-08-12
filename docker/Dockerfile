# Use the official .NET runtime as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5555

# Use the official .NET SDK as a build image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies (from docker directory perspective)
COPY ["../src/Micube.MCP.Server/Micube.MCP.Server.csproj", "Micube.MCP.Server/"]
COPY ["../src/Micube.MCP.Core/Micube.MCP.Core.csproj", "Micube.MCP.Core/"]
COPY ["../src/Micube.MCP.SDK/Micube.MCP.SDK.csproj", "Micube.MCP.SDK/"]

RUN dotnet restore "Micube.MCP.Server/Micube.MCP.Server.csproj"

# Copy everything else and build
COPY ../src/ .
RUN dotnet build "Micube.MCP.Server/Micube.MCP.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Micube.MCP.Server/Micube.MCP.Server.csproj" -c Release -o /app/publish

# Final stage/image
FROM base AS final
WORKDIR /app

# Create necessary directories
RUN mkdir -p /app/tools /app/docs /app/prompts /app/logs

# Copy published application
COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://*:5555

# Create non-root user for security
#RUN groupadd -r mcpuser && useradd -r -g mcpuser mcpuser
#RUN chown -R mcpuser:mcpuser /app
#USER mcpuser

ENTRYPOINT ["dotnet", "Micube.MCP.Server.dll"]