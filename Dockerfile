# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so the package layer is cached between code changes.
COPY TaskManagement.Domain/TaskManagement.Domain.csproj TaskManagement.Domain/
COPY TaskManagement.Application/TaskManagement.Application.csproj TaskManagement.Application/
COPY TaskManagement.Infrastructure/TaskManagement.Infrastructure.csproj TaskManagement.Infrastructure/
COPY TaskManagementAPI/TaskManagementAPI.csproj TaskManagementAPI/
RUN dotnet restore TaskManagementAPI/TaskManagementAPI.csproj

COPY TaskManagement.Domain/ TaskManagement.Domain/
COPY TaskManagement.Application/ TaskManagement.Application/
COPY TaskManagement.Infrastructure/ TaskManagement.Infrastructure/
COPY TaskManagementAPI/ TaskManagementAPI/
RUN dotnet publish TaskManagementAPI/TaskManagementAPI.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    UseHttpsRedirection=false
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "TaskManagementAPI.dll"]
