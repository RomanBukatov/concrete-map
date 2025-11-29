# 1. БЕРЕМ ОБРАЗ ДЛЯ СБОРКИ (SDK)
# Это как скачать Visual Studio на Linux. Используем .NET 10 Preview.
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# 2. КОПИРУЕМ ФАЙЛЫ ПРОЕКТОВ
# Сначала копируем только "паспорта" проектов, чтобы скачать библиотеки.
COPY ["ConcreteMap.Api/ConcreteMap.Api.csproj", "ConcreteMap.Api/"]
COPY ["ConcreteMap.Infrastructure/ConcreteMap.Infrastructure.csproj", "ConcreteMap.Infrastructure/"]
COPY ["ConcreteMap.Domain/ConcreteMap.Domain.csproj", "ConcreteMap.Domain/"]

# 3. СКАЧИВАЕМ БИБЛИОТЕКИ (Nuget Restore)
RUN dotnet restore "ConcreteMap.Api/ConcreteMap.Api.csproj"

# 4. КОПИРУЕМ ВЕСЬ ОСТАЛЬНОЙ КОД
COPY . .

# 5. СОБИРАЕМ ПРОЕКТ (Build & Publish)
# Превращаем C# код в DLL файлы.
WORKDIR "/src/ConcreteMap.Api"
RUN dotnet publish "ConcreteMap.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 6. ГОТОВИМ ФИНАЛЬНЫЙ ОБРАЗ
# Берем чистый Linux с .NET Runtime (без компилятора, он легче).
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app

# Переносим скомпилированные файлы из этапа сборки
COPY --from=build /app/publish .

# Говорим: "При старте запусти эту команду"
ENTRYPOINT ["dotnet", "ConcreteMap.Api.dll"]