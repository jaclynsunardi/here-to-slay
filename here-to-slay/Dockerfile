FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY here-to-slay.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish here-to-slay.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080
ENTRYPOINT ["dotnet", "here-to-slay.dll"]
