# 🗺️ ConcreteMap: Интерактивная карта заводов ЖБИ

Fullstack-проект интерактивной карты с административной панелью, реализованный на .NET 10 и Vanilla JS.

![Скриншот Карты](path/to/your/screenshot.png) <!-- Вставь сюда ссылку на скриншот -->

## ✨ Ключевые фичи
- **Интерактивная карта:** Отображение объектов на Яндекс.Картах с кластеризацией.
- **Полнотекстовый поиск:** Быстрый нечеткий поиск по продукции с использованием `pg_trgm` в PostgreSQL.
- **Панель Администратора:** Управление пользователями, импорт/экспорт данных через Excel.
- **Безопасность:** JWT-аутентификация и ролевая модель (Admin/User).
- **Контейнеризация:** Полная поддержка Docker и Docker Compose для быстрого развертывания.

## 🚀 Технологический стек
- **Backend:** C# 12, .NET 10, ASP.NET Core Web API
- **База данных:** PostgreSQL 14 (с расширением `pg_trgm`)
- **ORM:** Entity Framework Core 9
- **Frontend:** HTML5, CSS3, Vanilla JavaScript (ES6+)
- **API Карты:** Yandex Maps API v2.1
- **DevOps:** Docker, Docker Compose

## 🏁 Локальный запуск (Docker)

### Требования
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### Инструкция
1. **Клонируйте репозиторий:**
   ```bash
   git clone https://github.com/RomanBukatov/concrete-map.git
   cd concrete-map
   ```

2. **Настройте `docker-compose.yml`:**
   Откройте файл `docker-compose.yml` и укажите ваш API-ключ для Яндекс.Карт в переменной `YandexMaps__ApiKey`.

3. **Запустите проект:**
   ```bash
   docker-compose up -d --build
   ```

4. **Откройте приложение:**
   - Сайт: http://localhost:8080
   - Логин: admin
   - Пароль: admin123

## 🏗️ Структура проекта (Clean Architecture)
- **ConcreteMap.Domain:** Сущности, DTO, "сердце" приложения.
- **ConcreteMap.Infrastructure:** Работа с БД (EF Core), сервисы импорта/экспорта.
- **ConcreteMap.Api:** Контроллеры, JWT, точка входа, статика фронтенда (wwwroot).
