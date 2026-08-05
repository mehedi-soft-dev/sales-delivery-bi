using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SalesDeliveryBI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesOrderSummaryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Sales Order module (docs/plans, "MV instead of actual table" — discussed with the user): no OLTP
            // source exists for this module, so bi.mv_sales_order_summary is a plain table, seeded directly by
            // DatabaseSeeder — not a real materialized view, so no unique-index-for-CONCURRENTLY-refresh
            // requirement and no pg_cron schedule statement (nothing to refresh from). "Last refresh" still
            // comes from bi.mv_refresh_log like every other dashboard; the seeder inserts a row there itself.
            migrationBuilder.Sql(
                """
                CREATE TABLE bi.mv_sales_order_summary (
                    so_id uuid PRIMARY KEY,
                    so_no text NOT NULL,
                    so_date date NOT NULL,
                    quotation_id uuid NULL REFERENCES sales."Quotations" ("Id") ON DELETE RESTRICT,
                    buyer_id uuid NOT NULL REFERENCES sales."Buyers" ("Id") ON DELETE RESTRICT,
                    buyer_name text NOT NULL,
                    merchandiser_id uuid NOT NULL REFERENCES sales."Merchandisers" ("Id") ON DELETE RESTRICT,
                    merchandiser_name text NOT NULL,
                    unit_id uuid NOT NULL REFERENCES sales."Units" ("Id") ON DELETE RESTRICT,
                    unit_name text NOT NULL,
                    currency_code text NOT NULL,
                    order_value_usd numeric NOT NULL,
                    delivered_value_usd numeric NOT NULL,
                    pending_value_usd numeric NOT NULL,
                    status text NOT NULL,
                    promised_delivery_date date NOT NULL,
                    last_refresh_date timestamptz NOT NULL
                );

                CREATE UNIQUE INDEX ux_mv_sales_order_summary_so_no ON bi.mv_sales_order_summary (so_no);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS bi.mv_sales_order_summary;");
        }
    }
}
