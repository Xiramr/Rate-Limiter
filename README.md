# RateLimiter

Распределённая система ограничения частоты запросов (RPM) для защиты gRPC API под нагрузкой. Лимиты настраиваются через отдельный сервис (Writer), динамически подхватываются без рестартов, события запросов обрабатываются асинхронно через Kafka, а проверка превышения лимита и блокировка пользователя выполняются атомарно в Redis.

---

## Оглавление

- [Архитектура](#архитектура)
- [Принципы проектирования](#принципы-проектирования)
- [Технологический стек](#технологический-стек)
- [Сервисы](#сервисы)
  - [UserService](#userservice)
  - [RateLimiter.Writer](#ratelimiterwriter)
  - [RateLimiter.Reader](#ratelimiterreader)
  - [UserRequestsKafkaGenerator](#userrequestskafkagenerator)

---

## Архитектура

- **UserService** — сервис пользователей (PostgreSQL) + проверка блокировки по Redis на уровне gRPC Interceptors
- **RateLimiter.Writer** — CRUD лимитов (MongoDB) по `route` и `requests_per_minute`
- **RateLimiter.Reader** — синхронизация лимитов из MongoDB, обработка событий запросов из Kafka, применение лимитов и банов в Redis
- **UserRequestsKafkaGenerator** — консольный генератор событий запросов в Kafka (RPM)

### Потоки данных

#### 1. Настройка лимитов

- Клиент вызывает **Writer** по gRPC → Writer сохраняет лимит в **MongoDB**
- **Reader** получает изменения через **MongoDB Change Streams** и обновляет in-memory кэш лимитов

#### 2. Обработка запросов

- Генератор отправляет события запросов в Kafka (`topic: user_requests`)
- **Reader** читает события из Kafka, применяет лимит и при превышении выставляет бан в **Redis**
- **UserService** на каждом gRPC вызове проверяет бан в Redis через interceptor и возвращает ошибку при превышении

---

## Принципы проектирования

- **Layered Architecture**: разделение на `Domain`, `Application`, `Infrastructure`, `Grpc/Hosted`, `Presentation`
- **Dependency Inversion**: бизнес-логика зависит от интерфейсов (`IUserRepository`, `IRateLimitRepository`, `IUserBanRepository`)
- **Single Responsibility**: сервисы изолируют ответственность
- **Repository Pattern**: отдельные реализации PostgreSQL/MongoDB/Redis/Kafka
- **DTO/Transport модели**: отдельные модели для Kafka payload и gRPC моделей
- **Interceptors**: кросс-секционные требования реализованы через gRPC interceptors
- **Atomic operations**: проверка лимита и выставление бана выполняется атомарно через Lua-скрипт

---

## Технологический стек

- **Язык/платформа:** C# / .NET
- **RPC:** gRPC
- **Kafka:** Confluent.Kafka
- **MongoDB:** MongoDB.Driver
- **Redis:** StackExchange.Redis
- **PostgreSQL:** Npgsql + Dapper
- **Валидация:** FluentValidation
- **Маппинг:** Mapperly
- **Кэш:** IMemoryCache
- **Логирование:** Microsoft.Extensions.Logging
- **Хэширование пароля:** PBKDF2

---

## Сервисы

---

### UserService

Назначение: CRUD пользователей и защита gRPC методов от превышения лимита.

#### Хранилища

- **PostgreSQL** — основное хранение пользователей  
  (Dapper + Npgsql, функции/процедуры:  
  `create_user`, `get_user_by_id`, `get_user_by_name_surname`, `update_user`, `delete_user`)
- **Redis** — проверка бана пользователя по эндпоинту (`ban:{userId}:{endpoint}`)

#### Ключевые компоненты

- **Хэширование паролей:** PBKDF2 (Rfc2898DeriveBytes, SHA-256, соль + 120000 итераций)
- **Кэширование:** `IMemoryCache` для `GetUserById` и поиска по `Name+Surname`

#### gRPC Interceptors

- `AuthenticationInterceptor`
  - требует header `user_id`
  - сохраняет в `ServerCallContext.UserState`

- `RateLimitInterceptor`
  - до выполнения endpoint проверяет бан в Redis

- `ExceptionMappingInterceptor`
  - преобразует исключения в корректные gRPC статусы

---

### RateLimiter.Writer

Назначение: управление лимитами (RPM) по `route`.

#### Хранилище

**MongoDB**, коллекция лимитов:

- `route` (string)
- `requests_per_minute` (int)
- уникальный индекс по `route`

#### gRPC методы

- `CreateLimit`
- `GetLimitByRoute`
- `UpdateLimit`
- `DeleteLimit`

#### Ошибки

- `InvalidArgument`
- `AlreadyExists`
- `NotFound`

---

### RateLimiter.Reader

Назначение: синхронизация лимитов и применение rate limiting.

#### Источники данных

**MongoDB**
- `InitializeAsync()` — загрузка всех лимитов
- `WatchAsync()` — Change Streams и обновление кэша без рестартов

**Kafka**
- Consumer читает `topic: user_requests`
- JSON → `UserRequest(user_id, endpoint)`
- Применение лимита на каждое сообщение

#### In-memory кэш

- `ConcurrentDictionary<string, RateLimit> _limitsByRoute`
- `ConcurrentDictionary<string, string> _idToRoute`

#### Применение rate limit

Redis Lua-скрипт атомарно:

- проверяет бан `ban:{userId}:{endpoint}`
- увеличивает счётчик `req:{userId}:{endpoint}`
- выставляет TTL окна
- при превышении лимита выставляет бан

#### gRPC методы

- `Ping`
- `GetAllLimits`

---

### UserRequestsKafkaGenerator

Назначение: генерация событий запросов в Kafka с заданным RPM.

#### Команды

- `add <userId> <endpoint> <rpm>`
- `update <id> rpm <value>`
- `update <id> endpoint <value>`
- `remove <id>`
- `list`
- `exit`

Каждая задача публикует сообщения в Kafka с интервалом, рассчитанным от `rpm`.

