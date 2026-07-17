# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG VERSION=0.0.0-dev
WORKDIR /src

# Copy project file and restore dependencies
COPY PaperlessMCP.csproj .
RUN dotnet restore

# Copy source code and build
COPY . .
RUN dotnet publish -c Release -o /app/publish /p:Version=${VERSION}

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
ARG VERSION=0.0.0-dev
WORKDIR /app

# OCI Labels
LABEL org.opencontainers.image.title="PaperlessMCP"
LABEL org.opencontainers.image.description="Model Context Protocol server for Paperless-ngx"
LABEL org.opencontainers.image.version="${VERSION}"
LABEL org.opencontainers.image.source="https://github.com/TeeJS/PaperlessMCP"
LABEL org.opencontainers.image.licenses="MIT"

# Copy published application
COPY --from=build /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5000
ENV MCP_PORT=5000

# Expose port for HTTP transport
EXPOSE 5000

# Run the application (HTTP mode by default)
ENTRYPOINT ["dotnet", "PaperlessMCP.dll"]
