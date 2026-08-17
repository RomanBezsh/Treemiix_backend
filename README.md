# CloneAmazonBack

Бэкенд клона Amazon на ASP.NET Core 9 + EF Core 9 + PostgreSQL (Npgsql). JWT, роли, rate limiting, логирование.

## Запуск локально

### Нужно
- .NET SDK 9.0
- PostgreSQL (подойдёт и Neon)

### Настройка
Задай переменные окружения (или пропиши в `appsettings.json`). Секреты в репозиторий не пушим.

| Переменная | Что это |
|---|---|
| `ConnectionStrings__DefaultConnection` | строка подключения к PostgreSQL |
| `Jwt__Key` | ключ для подписи JWT, минимум 32 символа |
| `Jwt__Issuer` | issuer токена, по умолчанию `CloneAmazonBack` |
| `Jwt__Audience` | audience токена, по умолчанию `CloneAmazonFront` |
| `Jwt__ExpiryHours` | сколько часов живёт токен, по умолчанию 24 |
| `Admin__Email` | email первого админа, создаётся автоматом при старте |
| `Admin__Password` | пароль первого админа |
| `RateLimiting__AuthPermitLimit` | лимит запросов/мин на `/api/auth`, по умолчанию 10 |
| `RateLimiting__GlobalPermitLimit` | лимит запросов/мин на всё остальное, по умолчанию 100 |
| `Auth__RequireEmailConfirmation` | требовать подтверждение почты при регистрации, по умолчанию false |

Строка подключения выглядит так:
```
Host=localhost;Database=cloneamazon;Username=postgres;Password=secret
```

### Миграции
```bash
dotnet ef database update
```

### Запуск
```bash
dotnet run
```

При старте само создаётся: роли `Admin`, `Seller`, `User` (если их ещё нет) и первый админ из `Admin__Email`/`Admin__Password`, если эти переменные заданы и такого email ещё нет.

### Подтверждение почты
Если `Auth__RequireEmailConfirmation = true` — после регистрации юзеру приходит 6-значный код, живёт 15 минут:
```
POST /api/auth/confirm-email
{ "email": "user@example.com", "code": "123456" }
```
Без подтверждения залогиниться нельзя. По умолчанию (`false`) почта считается подтверждённой сразу.

## Тесты

`CloneAmazonBack.Tests` — юнит-тесты на сервисы, гоняются на EF InMemory, реальная БД не нужна.

```bash
dotnet test
dotnet test CloneAmazonBack.Tests\CloneAmazonBack.Tests.csproj
```

Запустить конкретное:
```bash
dotnet test --filter "AuthService"          # по имени
dotnet test --filter "ProductServiceTests"  # один класс
dotnet test --filter "Name~UpdateAsync"     # по части имени
```

## API

Swagger в Development: `https://localhost:<port>/swagger`

Без токена доступны: регистрация, логин, категории, товары, отзывы, вопросы, ответы, галереи, видео, атрибуты, продавцы, а также промокоды и гифткарты по коду.

Всё остальное — только с JWT (`Authorization: Bearer <token>`). Роли:
- `Admin` — пользователи, роли, категории, промокоды, статусы продавцов/заказов
- `Seller` — товары, галереи, видео, атрибуты
- `User` — корзина, заказы, отзывы, вопросы, адреса, профиль, гифткарты

## Деплой на Render

1. Создаёшь Web Service из репозитория.
2. Прописываешь переменные окружения из таблицы выше. Обязательны `ConnectionStrings__DefaultConnection` и `Jwt__Key`.
3. `dotnet ef database update` — вручную или через Build Command при первом деплое.
