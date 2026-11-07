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

## Продавцы

Любой пользователь с подтверждённым email может открыть магазин. Онбординг устроен как
упрощённый Seller Central: заявка заполняется по шагам, затем её проверяет администратор.

| Метод | Endpoint | Описание |
|---|---|---|
| GET | `/api/sellers/plans` | тариф продавца (цена из конфига) |
| GET | `/api/sellers/me` | свой seller account и текущий статус |
| POST | `/api/sellers/onboarding/start` | начать онбординг, выдаёт роль `Seller` и новые токены |
| PUT | `/api/sellers/onboarding/business` | данные бизнеса |
| PUT | `/api/sellers/onboarding/storefront` | название, slug, описание, логотип |
| PUT | `/api/sellers/onboarding/payout` | реквизиты для выплат |
| POST | `/api/sellers/onboarding/submit` | отправить заявку на проверку |
| GET | `/api/admin/sellers?status=PendingVerification` | очередь заявок (только Admin) |
| POST | `/api/admin/sellers/{id}/approve` | одобрить магазин |
| POST | `/api/admin/sellers/{id}/reject` | отклонить с причиной |
| POST | `/api/admin/sellers/{id}/suspend` | заморозить |
| POST | `/api/admin/sellers/{id}/reactivate` | вернуть в работу |
| POST | `/api/admin/sellers/{id}/subscription/paid` | отметить подписку оплаченной |

Статусы: `Draft` → `PendingVerification` → `Active` / `Rejected`, плюс `Suspended` и `Inactive`.
Пока магазин не `Active` или подписка не оплачена, продавец не может публиковать товары.

`/api/sellers/me` и ответы онбординга возвращают владельцу его приватные поля
(`taxNumber`, `legalAddress`, `bankAccount`), чтобы фронтенд мог восстановить шаги
после перезагрузки. Публичные `/api/sellers` и `/api/sellers/{id}` эти поля не отдают.

Биллинг на первом этапе ручной: при одобрении заявки подписка активируется на
`Seller:Subscription:PeriodDays` дней, продление админ отмечает через
`/api/admin/sellers/{id}/subscription/paid`. Stripe можно подключить позже без изменения
остального флоу.

Цена и период тарифа задаются в конфиге:

```json
"Seller": {
  "CommissionRate": 0.10,
  "Subscription": {
    "PlanName": "Professional",
    "MonthlyPrice": 39.99,
    "Currency": "USD",
    "PeriodDays": 30
  }
}
```

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
Database__AllowDemoSeed=true
Database__DemoSeedProductCount=60
Email__Enabled=true
Email__Provider=Resend
Email__ApiKey=...
Email__FromAddress=no-reply@your-domain.com
```

`Database__MigrateOnStartup` по умолчанию включён: при старте приложение применяет все ожидающие миграции
и пишет в лог `Database migrations applied`. Отключить можно через `Database__MigrateOnStartup=false` —
тогда миграции нужно применять вручную **до** деплоя новой версии:

```bash
dotnet ef database update
```

## Демо-данные

Для быстрого наполнения базы есть админский endpoint:

```http
POST /api/admin/demo/seed?productCount=60&reset=false
Authorization: Bearer <admin access token>
```

Он создаёт (или дополняет) демо-маркетплейс:

- 10 продавцов: 7 `Active`, 1 `Suspended`, 1 `PendingVerification`, 1 `Rejected`;
- 12 категорий верхнего уровня и 60 подкатегорий;
- до 60 товаров с галереями, атрибутами, историей цен и брендами;
- 12 покупателей, профили, адреса и аватары;
- отзывы, вопросы, ответы и голоса;
- корзины, заказы, промокоды и подарочные карты;
- все изображения товаров, логотипы продавцов и аватары хранятся в PostgreSQL
  в `MediaFiles` и раздаются через `/media/{fileName}`.

Фотографии товаров скачиваются с Unsplash в размере 600×600 (`q=55`, JPEG, обычно 20–70 КБ).
Если внешний хост недоступен, автоматически создаётся лёгкая SVG-заглушка, поэтому сидинг
не падает без интернета. Повторный запуск идемпотентен: существующие демо-объекты не дублируются.

`reset=true` удаляет **только** демо-данные: пользователей с email `@demo.treemiix.local`,
товары с `SKU` на `DEMO-`, продавцов с slug на `demo-`, медиа с именем на `demo-` и
подкатегории с slug на `demo-`. Реальные товары и пользователи не затрагиваются.

Демо-аккаунты (пароль у всех `Demo123!`):

- продавцы: `seller1@demo.treemiix.local` … `seller10@demo.treemiix.local`;
- покупатели: `customer1@demo.treemiix.local` … `customer12@demo.treemiix.local`.

Пример вызова из PowerShell:

```powershell
$login = Invoke-RestMethod -Method Post `
  -Uri "https://treemiix-backend.onrender.com/api/auth/login" `
  -ContentType "application/json" `
  -Body '{"email":"ADMIN_EMAIL","password":"ADMIN_PASSWORD"}'

Invoke-RestMethod -Method Post `
  -Uri "https://treemiix-backend.onrender.com/api/admin/demo/seed?productCount=60&reset=false" `
  -Headers @{ Authorization = "Bearer $($login.token)" }
```

`Database:AllowDemoSeed=false` полностью отключает endpoint (по умолчанию `true`).
В Development при `Database:SeedDemoData=true` сидинг выполняется автоматически при старте.
В Production автоматического сидинга нет — только явный вызов админом.

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

Таблица создаётся миграцией `AddMediaStorage`. Она применяется автоматически при старте
(`Database:MigrateOnStartup` включён по умолчанию), вручную — `dotnet ef database update`.

Ссылки на изображения, загруженные до перехода на БД, будут отдавать 404, пока файлы не перезальют
через админку.

## Health checks

- `/health` — все проверки
- `/health/ready` — PostgreSQL и Redis
- `/health/live` — liveness
