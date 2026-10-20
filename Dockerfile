FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8081
ENV ASPNETCORE_URLS=http://+:8081


FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Arts.Domain/Arts.Domain.csproj", "src/Arts.Domain/"]
COPY ["src/Arts.Application/Arts.Application.csproj", "src/Arts.Application/"]
COPY ["src/Arts.Infrastructure/Arts.Infrastructure.csproj", "src/Arts.Infrastructure/"]
COPY ["src/Arts.Api/Arts.Api.csproj", "src/Arts.Api/"]

RUN dotnet restore "src/Arts.Api/Arts.Api.csproj"
COPY . .
WORKDIR "/src/src/Arts.Api"
RUN dotnet build "Arts.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Arts.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Arts.Api.dll"]