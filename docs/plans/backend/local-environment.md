# SalesDeliveryBI — Local Dev Environment

Ports chosen to avoid conflicts with other containers already running on this machine (5432/5433, 6379/6380 were taken).

| Service | Container Name | Host Port | Image |
|---|---|---|---|
| Postgres | `salesdeliverybi-postgres` | `5434` | `salesdeliverybi-postgres-pgcron` (custom, built from `docker/postgres/Dockerfile`) |
| Redis | `salesdeliverybi-redis` | `6381` | `redis:7-alpine` |

**Connection strings:**
```
Postgres: Host=localhost;Port=5434;Database=salesdeliverybi;Username=salesdeliverybi;Password=salesdeliverybi
Redis:    localhost:6381
```

## Postgres image — `pg_cron` included

Standard `postgres:16-alpine` doesn't ship `pg_cron` (Alpine has no package for it). Switched to a custom image: `postgres:16` (Debian-based) + `postgresql-16-cron` installed via `apt-get`, defined in [`docker/postgres/Dockerfile`](../../../docker/postgres/Dockerfile):

```dockerfile
FROM postgres:16

RUN apt-get update \
    && apt-get install -y --no-install-recommends postgresql-16-cron \
    && rm -rf /var/lib/apt/lists/*
```

**Build + run (recreate from scratch):**

```bash
docker build -t salesdeliverybi-postgres-pgcron "docker/postgres"

docker run -d \
  --name salesdeliverybi-postgres \
  -e POSTGRES_USER=salesdeliverybi \
  -e POSTGRES_PASSWORD=salesdeliverybi \
  -e POSTGRES_DB=salesdeliverybi \
  -p 5434:5432 \
  salesdeliverybi-postgres-pgcron \
  postgres -c shared_preload_libraries=pg_cron -c cron.database_name=salesdeliverybi
```

`shared_preload_libraries` and `cron.database_name` are passed as `postgres` command-line overrides at container start — no need to bake them into a custom `postgresql.conf`.

**Enable the extension once per fresh database:**
```sql
CREATE EXTENSION IF NOT EXISTS pg_cron;
```

Verified working: `pg_cron 1.6` loaded, `shared_preload_libraries = pg_cron` confirmed via `SHOW shared_preload_libraries;`.

## Redis

```bash
docker run -d \
  --name salesdeliverybi-redis \
  -p 6381:6379 \
  redis:7-alpine
```
