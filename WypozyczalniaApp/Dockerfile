# Faza 1: Budowa
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# KLUCZOWA ZMIANA 1: Kopiowanie pliku projektu z uwzględnieniem podkatalogu
# Kopiujemy plik .csproj z folderu WypozyczalniaApp do kontenera.
COPY ["WypozyczalniaApp/WypozyczalniaApp.csproj", "WypozyczalniaApp/"] 
WORKDIR /src/WypozyczalniaApp
RUN dotnet restore

# Kopiowanie reszty plików i publikacja (teraz z katalogu roboczego /src)
WORKDIR /src
COPY . .
# KLUCZOWA ZMIANA 2: Używamy pełnej ścieżki do pliku .csproj wewnątrz /src
RUN dotnet publish "WypozyczalniaApp/WypozyczalniaApp.csproj" -c Release -o /app/publish 

# Faza 2: Środowisko Uruchomieniowe
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Ustawienie portu, którego używa Render
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000
ENTRYPOINT ["dotnet", "WypozyczalniaApp.dll"]