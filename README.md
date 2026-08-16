# Sales & Delivery BI

BI/reporting dashboards (Quotations, Sales Orders, Delivery, Invoice, Returns) over a Garment/Textile ERP.

<img width="1918" height="950" alt="image" src="https://github.com/user-attachments/assets/a9f3726b-993f-4eba-8d2a-3adb6692dc62" />


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
