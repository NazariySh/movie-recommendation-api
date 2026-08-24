# MovieMatch API

**MovieMatch API** is the backend for a movie/series recommendation platform. It combines collaborative filtering (ML.NET matrix factorization) with semantic search over embeddings (Pgvector) to produce personalized "for you," cold-start, and "because you watched" recommendations, re-ranked with MMR for diversity.

Beyond recommendations, it covers the full catalog and account surface: movie/series browsing with genres and filters, full-text/fuzzy search, reviews and star ratings, watchlists, a first-run onboarding survey used to bootstrap taste profiles for new users, and an admin area for managing movies, artists, users, and the ML model (data import, training, evaluation). Auth is JWT-based with refresh-token rotation, email verification, password reset, and Google Sign-In.

The solution follows Clean Architecture, split into four projects plus the API host:

- `MovieRecommendation.Domain` – entities, enums, no external dependencies.
- `MovieRecommendation.Application` – MediatR commands/queries, validators, DTOs, interfaces (business logic, no infrastructure concerns).
- `MovieRecommendation.Infrastructure` – EF Core/PostgreSQL, Identity, email, blob storage, caching — implementations of the Application interfaces.
- `MovieRecommendation.ML` – embedding generation, the collaborative-filtering recommender, and semantic search/re-ranking.
- `MovieRecommendation.API` – ASP.NET Core host: controllers, middleware, DI wiring, and (on publish) hosts the built Angular SPA.

---

## 🚀 Technologies Used

- **.NET 10** – Core framework for building the application.
- **ASP.NET Core Identity + JWT Bearer** – Authentication and authorization, with refresh-token rotation via HttpOnly cookies.
- **MediatR** – CQRS-style command/query handling in the Application layer.
- **Entity Framework Core + Npgsql** – ORM and PostgreSQL provider, migrations managed from the Infrastructure layer.
- **Pgvector.EntityFrameworkCore** – Stores movie embeddings and powers semantic similarity search directly in PostgreSQL.
- **Microsoft.ML + Microsoft.ML.Recommender** – Collaborative-filtering recommender (matrix factorization) trained on user ratings.
- **OpenAI SDK** – Generates embeddings used for semantic search.
- **AutoMapper / FluentValidation** – DTO mapping and request validation.
- **Serilog** – Structured logging.
- **MailKit + MimeKit** – SMTP email delivery (verification, password reset).
- **Azure Blob Storage** – Poster/avatar image storage.
- **In-memory caching** (`ICacheService` / `MemoryCacheService`) – No external cache dependency (no Redis).
- **xUnit + Moq + FluentAssertions** – Unit testing (`tests/MovieRecommendation.UnitTests`).

---

## 🛠 Setup Instructions

### 1. Prerequisites

- **[.NET SDK 10](https://dotnet.microsoft.com/download/dotnet/10.0)**
- **[Git](https://git-scm.com/downloads)**
- **PostgreSQL** with the [`pgvector`](https://github.com/pgvector/pgvector) extension available
- **Node.js + npm** (only needed for `dotnet publish`, which builds the Angular client into `wwwroot`; not required for local `dotnet run`)

### 2. Clone the Repository

```bash
git clone <repository-url>
cd movie-recommendation/movie-recommendation-api
```

### 3. Restore the Packages

```bash
dotnet restore
```

### 4. Configure the Database

Enable the `pgvector` extension once on your target database:

```sql
CREATE EXTENSION IF NOT EXISTS vector;
```

Set the connection string in `src/MovieRecommendation.API/appsettings.Development.json` (a working local default is already checked in):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=movie_match_db;Username=postgres;Password=postgres"
  }
}
```

Apply migrations:

```bash
dotnet ef database update --project src/MovieRecommendation.Infrastructure
```

Other settings you'll likely want to fill in for full functionality (JWT issuer/audience, Google auth client ID, SMTP credentials, TMDB access token, OpenAI API key, Azure Blob connection string) live in the same `appsettings.Development.json`, grouped under `JwtSettings`, `GoogleAuth`, `Email:Smtp`, `TmdbSettings`, `OpenAI`, and `BlobStorageSettings`. Everything not set falls back to sane dev defaults (e.g. email sending logs the link instead of sending it).

### 5. Run the Application

```bash
dotnet run --project src/MovieRecommendation.API
```

The API will be available at:
➡️ https://localhost:7170/api (HTTPS)
➡️ http://localhost:5014/api (HTTP)

### 6. Run Tests

```bash
dotnet test tests/MovieRecommendation.UnitTests
```
