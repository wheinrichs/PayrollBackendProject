FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY PayrollBackendProject.API/PayrollBackendProject.API.csproj PayrollBackendProject.API/
COPY PayrollBackendProject.Application/PayrollBackendProject.Application.csproj PayrollBackendProject.Application/
COPY PayrollBackendProject.Domain/PayrollBackendProject.Domain.csproj PayrollBackendProject.Domain/
COPY PayrollBackendProject.Infrastructure/PayrollBackendProject.Infrastructure.csproj PayrollBackendProject.Infrastructure/

RUN dotnet restore PayrollBackendProject.API/PayrollBackendProject.API.csproj

COPY . .
RUN dotnet publish PayrollBackendProject.API/PayrollBackendProject.API.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "PayrollBackendProject.API.dll"]