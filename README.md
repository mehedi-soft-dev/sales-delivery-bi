# Sales & Delivery BI

BI/reporting dashboards (Quotations, Sales Orders, Delivery, Invoice, Returns) over a Garment/Textile ERP.

**Stack:** ASP.NET Core (.NET 10) · Angular 21 + PrimeNG + ApexCharts · PostgreSQL + pg_cron · Redis

## Run

```bash
docker compose up --build
```

| Service | URL |
|---|---|
| Frontend | http://127.0.0.1:58090 |
| Backend API | http://127.0.0.1:58080 |
| Postgres | 127.0.0.1:5444 |
| Redis | 127.0.0.1:6391 |

Use `127.0.0.1`, not `localhost` (Docker Desktop/Windows IPv6 quirk).

Migrations + seed data apply automatically on first startup — no extra setup.

## Login

Password for every account: `Passw0rd!1`

- `admin@salesdeliverybi.dev` — SuperAdmin
- `general.manager@salesdeliverybi.dev` — GeneralManager
- `commercial.manager@salesdeliverybi.dev` — CommercialManager
- `commercial.officer@salesdeliverybi.dev` — CommercialOfficer
- `merchandiser@salesdeliverybi.dev` — Merchandiser
- `finance.manager@salesdeliverybi.dev` — FinanceManager
- `viewer@salesdeliverybi.dev` — Viewer

## Docs

- [`docs/requirements`](docs/requirements)
- [`docs/plans`](docs/plans)
