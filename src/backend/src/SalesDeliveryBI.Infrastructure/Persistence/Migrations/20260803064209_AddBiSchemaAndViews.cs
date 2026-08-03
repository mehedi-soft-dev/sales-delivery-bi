using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SalesDeliveryBI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBiSchemaAndViews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE SCHEMA IF NOT EXISTS bi;
                CREATE EXTENSION IF NOT EXISTS pg_cron;
                """);

            migrationBuilder.Sql(
                """
                CREATE TABLE bi.mv_refresh_log (
                    id BIGSERIAL PRIMARY KEY,
                    mv_name TEXT NOT NULL,
                    started_at TIMESTAMPTZ NOT NULL,
                    finished_at TIMESTAMPTZ,
                    status TEXT NOT NULL DEFAULT 'RUNNING',
                    rows_affected BIGINT,
                    error_message TEXT
                );
                """);

            migrationBuilder.Sql(
                """
                CREATE MATERIALIZED VIEW bi.mv_sales_quotation_summary AS
                SELECT
                    q."Id" AS quotation_id, q."QuotationNo" AS quotation_no, q."QuotationDate" AS quotation_date,
                    q."BuyerId" AS buyer_id, b."BuyerName" AS buyer_name,
                    q."MerchandiserId" AS merchandiser_id, m."MerchandiserName" AS merchandiser_name,
                    q."UnitId" AS unit_id, u."UnitName" AS unit_name,
                    q."StyleNo" AS style_no, q."Season" AS season, q."CurrencyCode" AS currency_code,
                    -- COALESCE guards against a NULL wiping every USD aggregate when no FxRate row exists yet for
                    -- this currency/date (see database/schema-plan.md's open FX-ownership dependency).
                    q."Value" * COALESCE(fx."RateToUsd", 1) AS quotation_value_usd,
                    q."Status" AS status, q."StatusDate" AS status_date,
                    (CURRENT_DATE - q."StatusDate"::date) AS days_in_status,
                    (COALESCE(q."ConvertedDate", now())::date - q."QuotationDate") AS days_open,
                    q."ConvertedToSoNo" AS converted_to_so_no, q."ConvertedDate" AS converted_date,
                    (q."ConvertedDate"::date - q."QuotationDate") AS conversion_days,
                    q."LostReason" AS lost_reason, q."CreatedBy" AS created_by,
                    now() AS last_refresh_date
                FROM sales."Quotations" q
                JOIN sales."Buyers" b ON b."Id" = q."BuyerId"
                JOIN sales."Merchandisers" m ON m."Id" = q."MerchandiserId"
                JOIN sales."Units" u ON u."Id" = q."UnitId"
                LEFT JOIN sales."FxRates" fx ON fx."CurrencyCode" = q."CurrencyCode"
                                              AND fx."RateDate" = q."QuotationDate";

                CREATE UNIQUE INDEX ux_mv_quotation_summary ON bi.mv_sales_quotation_summary (quotation_id);
                """);

            migrationBuilder.Sql(
                """
                CREATE MATERIALIZED VIEW bi.mv_quotation_pipeline_daily AS
                SELECT
                    CURRENT_DATE AS snapshot_date,
                    unit_id,
                    unit_name,
                    status,
                    COUNT(*) AS quotation_count,
                    SUM(quotation_value_usd) AS quotation_value_usd,
                    now() AS last_refresh_date
                FROM bi.mv_sales_quotation_summary
                GROUP BY unit_id, unit_name, status;

                CREATE UNIQUE INDEX ux_mv_quotation_pipeline_daily ON bi.mv_quotation_pipeline_daily (snapshot_date, unit_id, status);
                """);

            migrationBuilder.Sql(
                """
                CREATE MATERIALIZED VIEW bi.mv_quotation_conversion_rate AS
                SELECT
                    date_trunc('month', quotation_date)::date AS month,
                    buyer_id, buyer_name,
                    merchandiser_id, merchandiser_name,
                    unit_id, unit_name,
                    COUNT(*) AS quotations_count,
                    COUNT(*) FILTER (WHERE status = 'Converted') AS won_count,
                    COUNT(*) FILTER (WHERE status IN ('Rejected', 'Expired')) AS lost_count,
                    ROUND(100.0 * COUNT(*) FILTER (WHERE status = 'Converted') / NULLIF(COUNT(*), 0), 2) AS conversion_rate_pct,
                    SUM(quotation_value_usd) AS quotation_value_usd,
                    SUM(quotation_value_usd) FILTER (WHERE status = 'Converted') AS won_value_usd,
                    SUM(quotation_value_usd) FILTER (WHERE status IN ('Rejected', 'Expired')) AS lost_value_usd,
                    AVG(conversion_days) FILTER (WHERE status = 'Converted') AS avg_conversion_days,
                    now() AS last_refresh_date
                FROM bi.mv_sales_quotation_summary
                GROUP BY date_trunc('month', quotation_date), buyer_id, buyer_name, merchandiser_id, merchandiser_name, unit_id, unit_name;

                CREATE UNIQUE INDEX ux_mv_quotation_conversion_rate ON bi.mv_quotation_conversion_rate (month, buyer_id, merchandiser_id, unit_id);
                """);

            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION bi.refresh_materialized_view(p_mv_name text)
                RETURNS void
                LANGUAGE plpgsql
                AS $$
                DECLARE
                    v_log_id bigint;
                    v_row_count bigint;
                BEGIN
                    INSERT INTO bi.mv_refresh_log (mv_name, started_at, status)
                    VALUES (p_mv_name, now(), 'RUNNING')
                    RETURNING id INTO v_log_id;

                    BEGIN
                        EXECUTE format('REFRESH MATERIALIZED VIEW CONCURRENTLY %s', p_mv_name);
                        EXECUTE format('SELECT count(*) FROM %s', p_mv_name) INTO v_row_count;

                        UPDATE bi.mv_refresh_log
                        SET finished_at = now(), status = 'SUCCESS', rows_affected = v_row_count
                        WHERE id = v_log_id;
                    EXCEPTION WHEN OTHERS THEN
                        UPDATE bi.mv_refresh_log
                        SET finished_at = now(), status = 'FAILED', error_message = SQLERRM
                        WHERE id = v_log_id;
                        RAISE;
                    END;
                END;
                $$;
                """);

            migrationBuilder.Sql(
                """
                SELECT cron.schedule('refresh_mv_quotation_summary', '*/3 * * * *',
                    $$SELECT bi.refresh_materialized_view('bi.mv_sales_quotation_summary')$$);
                SELECT cron.schedule('refresh_mv_quotation_pipeline_daily', '*/15 * * * *',
                    $$SELECT bi.refresh_materialized_view('bi.mv_quotation_pipeline_daily')$$);
                SELECT cron.schedule('refresh_mv_quotation_conversion_rate', '*/15 * * * *',
                    $$SELECT bi.refresh_materialized_view('bi.mv_quotation_conversion_rate')$$);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                SELECT cron.unschedule('refresh_mv_quotation_summary');
                SELECT cron.unschedule('refresh_mv_quotation_pipeline_daily');
                SELECT cron.unschedule('refresh_mv_quotation_conversion_rate');
                """);

            migrationBuilder.Sql("DROP FUNCTION IF EXISTS bi.refresh_materialized_view(text);");

            migrationBuilder.Sql("DROP MATERIALIZED VIEW IF EXISTS bi.mv_quotation_conversion_rate;");
            migrationBuilder.Sql("DROP MATERIALIZED VIEW IF EXISTS bi.mv_quotation_pipeline_daily;");
            migrationBuilder.Sql("DROP MATERIALIZED VIEW IF EXISTS bi.mv_sales_quotation_summary;");

            migrationBuilder.Sql("DROP TABLE IF EXISTS bi.mv_refresh_log;");

            migrationBuilder.Sql("DROP SCHEMA IF EXISTS bi CASCADE;");
        }
    }
}
