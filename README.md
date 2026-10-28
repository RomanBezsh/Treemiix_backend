# Treemiix Backend

Маркетплейс-бэкенд на ASP.NET Core 9 + EF Core 9 + PostgreSQL. JWT, refresh-токены, роли, rate limiting, загрузка медиа, health checks.

## Требования

- .NET SDK 9.0
- PostgreSQL (локально, Neon, Supabase и т.д.)
- Redis — опционально

## Локальный запуск

1. Скопируйте пример конфигурации:

```bash
cp appsettings.example.json appsettings.Development.json
```

2. Заполните в `appsettings.Development.json`:

| Ключ | Назначение |
|---|---|
| `ConnectionStrings:DefaultConnection` | строка подключения к PostgreSQL |
| `Jwt:Key` | секрет JWT, минимум 32 символа |
| `Admin:Email` / `Admin:Password` | учётка администратора (создаётся при старте, если её нет) |
| `Cors:AllowedOrigins` | адреса фронтенда |

3. Примените миграции:

```bash
dotnet ef database update
```

4. Запустите:

```bash
dotnet run
```

Swagger доступен только в Development: `/swagger`.

## Аккаунты и авторизация

| Метод | Endpoint | Описание |
|---|---|---|
| POST | `/api/auth/register` | регистрация |
| POST | `/api/auth/login` | вход, возвращает access + refresh токены |
| POST | `/api/auth/refresh` | обмен refresh-токена на новую пару (с ротацией) |
| POST | `/api/auth/logout` | отзыв refresh-токена (требует access-токен) |
| POST | `/api/auth/confirm-email` | подтверждение email кодом |
| POST | `/api/auth/resend-confirmation` | повторная отправка кода |
| POST | `/api/auth/change-password` | смена пароля (требует access-токен) |
| POST | `/api/auth/forgot-password` | отправка кода сброса пароля |
| POST | `/api/auth/reset-password` | сброс пароля по коду |

Ответ `login`/`refresh`:

```json
{
  "token": "<access JWT>",
  "expiresAt": "2026-10-23T10:00:00Z",
  "refreshToken": "<refresh token>",
  "refreshTokenExpiresAt": "2026-11-21T10:00:00Z",
  "emailConfirmationRequired": false,
  "user": { "id": "...", "email": "...", "firstName": "...", "lastName": "...", "role": "User" }
}
```

Если включено `Auth:RequireEmailConfirmation`, регистрация возвращает `token` и `refreshToken` пустыми, пока email не подтверждён.

Защита от подбора пароля: после `Auth:MaxFailedLoginAttempts` неудачных попыток аккаунт блокируется на `Auth:LockoutMinutes` минут.

## Отправка писем

Коды подтверждения и сброса пароля отправляются через `IEmailSender`.

### Resend (рекомендуется для Render)

Render часто блокирует исходящий SMTP, поэтому используйте HTTP API:

```
Email__Enabled=true
Email__Provider=Resend
Email__ApiKey=re_...
Email__FromAddress=no-reply@your-domain.com
```

### SMTP

```
Email__Enabled=true
Email__Provider=Smtp
Email__Host=smtp.example.com
Email__Port=587
Email__UseStartTls=true
Email__UserName=...
Email__Password=...
Email__FromAddress=no-reply@your-domain.com
```

В Development при выключенной почте письма пишутся в лог приложения.

> Если `Auth:RequireEmailConfirmation=true`, а почта не настроена, приложение не запустится — иначе пользователи не смогут подтвердить email.

## Деплой на Render

1. Web Service из Dockerfile.
2. Обязательные env-переменные:

```
ConnectionStrings__DefaultConnection=...
Jwt__Key=<минимум 32 символа>
Jwt__Issuer=CloneAmazonBack
Jwt__Audience=CloneAmazonFront
Admin__Email=...
Admin__Password=...
Cors__AllowedOrigins__0=https://your-frontend.com
Database__MigrateOnStartup=true
Database__SeedDemoData=false
Email__Enabled=true
Email__Provider=Resend
Email__ApiKey=...
Email__FromAddress=no-reply@your-domain.com
```

`Database__MigrateOnStartup=true` применяет миграции до старта обработки запросов. Если оставить `false`, примените миграции вручную **до** деплоя новой версии:

```bash
dotnet ef database update
```

## Тесты

```bash
dotnet test CloneAmazonBack.Tests/CloneAmazonBack.Tests.csproj
```

Если установлен только runtime .NET 10, а проект на net9.0:

```powershell
$env:DOTNET_ROLL_FORWARD="Major"
dotnet test CloneAmazonBack.Tests/CloneAmazonBack.Tests.csproj
```

## Медиа

Загруженные изображения хранятся в PostgreSQL, в таблице `MediaFiles` (колонка `Bytes` типа `bytea`),
и раздаются контроллером по `/media/{fileName}` и `/uploads/{fileName}`.

Почему в БД, а не на диск: файловая система контейнера на Render (и большинстве PaaS) эфемерная —
всё, что записано в `wwwroot`, исчезает при каждом деплое или рестарте.

- только изображения (`jpg`, `jpeg`, `png`, `gif`, `webp`, `bmp`), видео не поддерживается;
- лимит файла — 5 МБ (`UploadController.MaxImageBytes`, `FormOptions.MultipartBodyLengthLimit` — 6 МБ);
- имена — случайные GUID, поэтому отдача идёт с `Cache-Control: public, max-age=31536000, immutable` и `ETag`;
- импорт по URL защищён от SSRF (запрещены loopback/private/link-local хосты);
- эндпоинты `POST /api/upload` и `POST /api/upload/media` требуют авторизации.

Таблица создаётся миграцией `AddMediaStorage`; на деплое примените её
(`dotnet ef database update` или `Database__MigrateOnStartup=true`).

## Health checks

- `/health` — все проверки
- `/health/ready` — PostgreSQL и Redis
- `/health/live` — liveness
