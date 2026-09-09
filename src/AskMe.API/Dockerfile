FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/AskMe.Domain/AskMe.Domain.csproj", "src/AskMe.Domain/"]
COPY ["src/AskMe.Application/AskMe.Application.csproj", "src/AskMe.Application/"]
COPY ["src/AskMe.Infrastructure/AskMe.Infrastructure.csproj", "src/AskMe.Infrastructure/"]
COPY ["src/AskMe.API/AskMe.API.csproj", "src/AskMe.API/"]
RUN dotnet restore "src/AskMe.API/AskMe.API.csproj"

COPY . .
RUN dotnet publish "src/AskMe.API/AskMe.API.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "AskMe.API.dll"]
