# CloneAmazonBack

Backend для клона Amazon: API на ASP.NET Core 9 + EF Core 9 + PostgreSQL (Npgsql), JWT-авторизация, роли, rate limiting, логирование.

## Запуск локально

### 1. Требования
- .NET SDK 9.0
- PostgreSQL (или Neon/облачная БД)

### 2. Конфигурация
Скопируй настройки в переменные окружения (или впиши в `appsettings.json`). Секреты в репозиторий не коммитятся.

| Переменная | Описание |
|---|---|
| `ConnectionStrings__DefaultConnection` | Строка подключения к PostgreSQL |
| `Jwt__Key` | Секретный ключ подписи JWT (минимум 32 символа) |
| `Jwt__Issuer` | Эмитент токена (по умолчанию `CloneAmazonBack`) |
| `Jwt__Audience` | Аудитория токена (по умолчанию `CloneAmazonFront`) |
| `Jwt__ExpiryHours` | Время жизни токена в часах (по умолчанию 24) |
| `Admin__Email` | Email первого админа (создаётся автоматически при старте) |
| `Admin__Password` | Пароль первого админа |
| `RateLimiting__AuthPermitLimit` | Лимит запросов/мин на /api/auth (по умолчанию 10) |
| `RateLimiting__GlobalPermitLimit` | Лимит запросов/мин для всех остальных (по умолчанию 100) |
| `Auth__RequireEmailConfirmation` | Включить подтверждение почты при регистрации (по умолчанию false) |

Пример строки подключения PostgreSQL:
```
Host=localhost;Database=cloneamazon;Username=postgres;Password=secret
```

### 3. Применение миграций
```bash
dotnet ef database update
```

### 4. Запуск
```bash
dotnet run
```

При старте автоматически:
- создаются роли `Admin`, `Seller`, `User` (если их нет);
- создаётся первый админ из `Admin__Email`/`Admin__Password` (если заданы и email не существует).

### Подтверждение почты
Если `Auth__RequireEmailConfirmation = true`, при регистрации юзер получает 6-значный код (валиден 15 минут). Подтвердить:
```
POST /api/auth/confirm-email
{ "email": "user@example.com", "code": "123456" }
```
До подтверждения вход по логину/паролю запрещён. При `false` (по умолчанию) почта считается подтверждённой сразу.

## Тесты

Проект `CloneAmazonBack.Tests` — unit-тесты на сервисы (EF InMemory, реальная БД не нужна).

```bash
dotnet test                        # из папки проекта
dotnet test CloneAmazonBack.Tests\CloneAmazonBack.Tests.csproj
```

Фильтрация:
```bash
dotnet test --filter "AuthService"                     # по имени
dotnet test --filter "ProductServiceTests"             # один класс
dotnet test --filter "Name~UpdateAsync"                # по части имени
```

## API

Swagger при запуске в Development: `https://localhost:<port>/swagger`

Публичные эндпоинты (без токена): регистрация, логин, категории, товары, отзывы, вопросы, ответы, галереи, видео, атрибуты, продавцы, промокоды и гифткарты по коду.

Остальное — только с JWT-токеном (`Authorization: Bearer <token>`). Роли:
- `Admin` — управление пользователями, ролями, категориями, промокодами, статусами продавцов/заказов;
- `Seller` — создание/изменение товаров, галерей, видео, атрибутов;
- `User` — корзина, заказы, отзывы, вопросы, адреса, профиль, гифткарты.

## Деплой (Render)

1. Создай Web Service из репозитория.
2. Задай переменные окружения (см. таблицу выше). Обязательные: `ConnectionStrings__DefaultConnection`, `Jwt__Key`.
3. `dotnet ef database update` выполняется при первом запуске вручную или через Build Command.