# Faza 1: Budowa
    FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
    WORKDIR /src
    
    # Kopiowanie pliku projektu i przywracanie zależności
    COPY ["WypozyczalniaApp.csproj", "WypozyczalniaApp/"] # Zmień, jeśli ścieżka jest inna
    WORKDIR /src/WypozyczalniaApp
    RUN dotnet restore
    
    # Kopiowanie reszty plików i publikacja
    WORKDIR /src
    COPY . .
    RUN dotnet publish "WypozyczalniaApp/WypozyczalniaApp.csproj" -c Release -o /app/publish 
    
    # Faza 2: Środowisko Uruchomieniowe
    FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
    WORKDIR /app
    COPY --from=build /app/publish .
    
    # Ustawienie portu, którego używa Render
    ENV ASPNETCORE_URLS=http://+:10000
    EXPOSE 10000
    ENTRYPOINT ["dotnet", "WypozyczalniaApp.dll"]