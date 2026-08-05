using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SalesDeliveryBI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryInvoiceReturnTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Delivery, Sales Invoice, and Return/Credit Note modules — same "plain table, no OLTP source"
            // pattern as bi.mv_sales_order_summary (AddSalesOrderSummaryTable): no pg_cron schedule, seeded
            // directly by DatabaseSeeder, "last refresh" comes from bi.mv_refresh_log like every other
            // dashboard. Each FKs into the previous stage's table, chaining the pipeline
            // Quotation -> SalesOrder -> Delivery -> Invoice -> Return.
            migrationBuilder.Sql(
                """
                CREATE TABLE bi.mv_delivery_performance (
                    delivery_id uuid PRIMARY KEY,
                    challan_no text NOT NULL,
                    delivery_date date NOT NULL,
                    sales_order_id uuid NOT NULL REFERENCES bi.mv_sales_order_summary ("so_id") ON DELETE RESTRICT,
                    buyer_id uuid NOT NULL REFERENCES sales."Buyers" ("Id") ON DELETE RESTRICT,
                    buyer_name text NOT NULL,
                    unit_id uuid NOT NULL REFERENCES sales."Units" ("Id") ON DELETE RESTRICT,
                    unit_name text NOT NULL,
                    delivered_value_usd numeric NOT NULL,
                    promised_date date NOT NULL,
                    delay_days int NOT NULL,
                    delivery_status text NOT NULL,
                    last_refresh_date timestamptz NOT NULL
                );

                CREATE UNIQUE INDEX ux_mv_delivery_performance_challan_no ON bi.mv_delivery_performance (challan_no);
                """);

            migrationBuilder.Sql(
                """
                CREATE TABLE bi.mv_sales_invoice_summary (
                    invoice_id uuid PRIMARY KEY,
                    invoice_no text NOT NULL,
                    invoice_date date NOT NULL,
                    delivery_id uuid NOT NULL REFERENCES bi.mv_delivery_performance ("delivery_id") ON DELETE RESTRICT,
                    sales_order_id uuid NOT NULL REFERENCES bi.mv_sales_order_summary ("so_id") ON DELETE RESTRICT,
                    buyer_id uuid NOT NULL REFERENCES sales."Buyers" ("Id") ON DELETE RESTRICT,
                    buyer_name text NOT NULL,
                    unit_id uuid NOT NULL REFERENCES sales."Units" ("Id") ON DELETE RESTRICT,
                    unit_name text NOT NULL,
                    currency_code text NOT NULL,
                    invoice_value_usd numeric NOT NULL,
                    paid_amount_usd numeric NOT NULL,
                    due_date date NOT NULL,
                    last_refresh_date timestamptz NOT NULL
                );

                CREATE UNIQUE INDEX ux_mv_sales_invoice_summary_invoice_no ON bi.mv_sales_invoice_summary (invoice_no);
                """);

            migrationBuilder.Sql(
                """
                CREATE TABLE bi.mv_sales_return_summary (
                    return_id uuid PRIMARY KEY,
                    return_no text NOT NULL,
                    return_date date NOT NULL,
                    invoice_id uuid NOT NULL REFERENCES bi.mv_sales_invoice_summary ("invoice_id") ON DELETE RESTRICT,
                    buyer_id uuid NOT NULL REFERENCES sales."Buyers" ("Id") ON DELETE RESTRICT,
                    buyer_name text NOT NULL,
                    unit_id uuid NOT NULL REFERENCES sales."Units" ("Id") ON DELETE RESTRICT,
                    unit_name text NOT NULL,
                    return_value_usd numeric NOT NULL,
                    return_qty int NOT NULL,
                    reason_code text NOT NULL,
                    last_refresh_date timestamptz NOT NULL
                );

                CREATE UNIQUE INDEX ux_mv_sales_return_summary_return_no ON bi.mv_sales_return_summary (return_no);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS bi.mv_sales_return_summary;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS bi.mv_sales_invoice_summary;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS bi.mv_delivery_performance;");
        }
    }
}
