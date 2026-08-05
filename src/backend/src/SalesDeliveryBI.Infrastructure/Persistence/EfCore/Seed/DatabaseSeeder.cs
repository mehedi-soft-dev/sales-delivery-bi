using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SalesDeliveryBI.Application.Abstractions;
using SalesDeliveryBI.Application.Common;
using SalesDeliveryBI.Domain.Entities;
using SalesDeliveryBI.Domain.Enums;
using SalesDeliveryBI.Infrastructure.Security;

namespace SalesDeliveryBI.Infrastructure.Persistence.EfCore.Seed;

/// <summary>
/// Development-only dev/test dataset — see docs/plans/database/seed-data.md for the source table
/// and the QuotationItem/QuotationStatusHistory generation rules implemented here.
/// </summary>
public class DatabaseSeeder
{
    private static readonly Guid SeedSystemUserId = Security.SystemCurrentUserContext.SystemUserId;

    private static readonly (Guid Id, string Name, string Type)[] SeedUnits =
    [
        (Guid.Parse("11111111-1111-1111-1111-111111111101"), "Unit-1", "Knit"),
        (Guid.Parse("11111111-1111-1111-1111-111111111102"), "Unit-2", "Woven"),
        (Guid.Parse("11111111-1111-1111-1111-111111111103"), "Unit-3", "Sweater"),
    ];

    private static readonly (Guid Id, string Name)[] SeedBuyers =
    [
        (Guid.Parse("22222222-2222-2222-2222-222222222201"), "H&M"),
        (Guid.Parse("22222222-2222-2222-2222-222222222202"), "Zara"),
        (Guid.Parse("22222222-2222-2222-2222-222222222203"), "Primark"),
        (Guid.Parse("22222222-2222-2222-2222-222222222204"), "C&A"),
        (Guid.Parse("22222222-2222-2222-2222-222222222205"), "Mango"),
        (Guid.Parse("22222222-2222-2222-2222-222222222206"), "Next"),
    ];

    private static readonly (Guid Id, string Name, string HomeUnitName)[] SeedMerchandisers =
    [
        (Guid.Parse("33333333-3333-3333-3333-333333333301"), "Fatema Begum", "Unit-1"),
        (Guid.Parse("33333333-3333-3333-3333-333333333302"), "Jahid Hasan", "Unit-2"),
        (Guid.Parse("33333333-3333-3333-3333-333333333303"), "Mehedi Hasan", "Unit-1"),
        (Guid.Parse("33333333-3333-3333-3333-333333333304"), "Sumaiya Akter", "Unit-2"),
    ];

    private static readonly QuotationStatus[] ForwardStatusOrder =
    [
        QuotationStatus.Draft,
        QuotationStatus.Submitted,
        QuotationStatus.Negotiation,
        QuotationStatus.PendingApproval,
        QuotationStatus.Approved,
        QuotationStatus.Converted,
    ];

    private static readonly Dictionary<string, (string Item1, string Item2)> ItemDescriptionsByUnitType =
        new()
        {
            ["Knit"] = ("Men's T-Shirt", "Men's Polo Shirt"),
            ["Woven"] = ("Men's Shirt", "Men's Pant"),
            ["Sweater"] = ("Men's Sweater", "Men's Cardigan"),
        };

    private static readonly (Guid Id, string Name)[] SeedRoles =
    [
        (Guid.Parse("55555555-5555-5555-5555-555555555501"), RoleNames.SuperAdmin),
        (Guid.Parse("55555555-5555-5555-5555-555555555502"), RoleNames.GeneralManager),
        (Guid.Parse("55555555-5555-5555-5555-555555555503"), RoleNames.CommercialManager),
        (Guid.Parse("55555555-5555-5555-5555-555555555504"), RoleNames.CommercialOfficer),
        (Guid.Parse("55555555-5555-5555-5555-555555555505"), RoleNames.Merchandiser),
        (Guid.Parse("55555555-5555-5555-5555-555555555506"), RoleNames.FinanceManager),
        (Guid.Parse("55555555-5555-5555-5555-555555555507"), RoleNames.Viewer),
    ];

    /// <summary>
    /// Role → permission-code mapping (docs/requirements/Sales_Delivery_Module_BI_Developer_Guidelines.md §5),
    /// seeded into sales.RolePermissions — the DB is the source of truth, not a static in-code table.
    /// </summary>
    private static readonly (string RoleName, string[] PermissionCodes)[] SeedRolePermissions =
    [
        (RoleNames.SuperAdmin,
            [
                PermissionCodes.QuotationViewPipeline, PermissionCodes.QuotationViewConversion,
                PermissionCodes.QuotationViewAging, PermissionCodes.QuotationViewSummary,
                PermissionCodes.QuotationViewAllUnits, PermissionCodes.SalesOrderView, PermissionCodes.SalesOrderViewAllUnits,
                PermissionCodes.DeliveryView, PermissionCodes.DeliveryViewAllUnits,
                PermissionCodes.InvoiceView, PermissionCodes.InvoiceViewAllUnits,
                PermissionCodes.ReturnView, PermissionCodes.ReturnViewAllUnits,
                PermissionCodes.AdminView, PermissionCodes.AdminManage,
            ]),
        (RoleNames.GeneralManager,
            [
                PermissionCodes.QuotationViewPipeline, PermissionCodes.QuotationViewConversion,
                PermissionCodes.QuotationViewAging, PermissionCodes.QuotationViewSummary,
                PermissionCodes.QuotationViewAllUnits, PermissionCodes.SalesOrderView, PermissionCodes.SalesOrderViewAllUnits,
                PermissionCodes.DeliveryView, PermissionCodes.DeliveryViewAllUnits,
                PermissionCodes.InvoiceView, PermissionCodes.InvoiceViewAllUnits,
                PermissionCodes.ReturnView, PermissionCodes.ReturnViewAllUnits,
            ]),
        (RoleNames.CommercialManager,
            [
                PermissionCodes.QuotationViewPipeline, PermissionCodes.QuotationViewConversion,
                PermissionCodes.QuotationViewAging, PermissionCodes.QuotationViewSummary, PermissionCodes.SalesOrderView,
                PermissionCodes.DeliveryView, PermissionCodes.InvoiceView, PermissionCodes.ReturnView,
            ]),
        (RoleNames.CommercialOfficer,
            [
                PermissionCodes.QuotationViewPipeline, PermissionCodes.QuotationViewSummary, PermissionCodes.SalesOrderView,
                PermissionCodes.DeliveryView, PermissionCodes.InvoiceView, PermissionCodes.ReturnView,
            ]),
        (RoleNames.Merchandiser,
            [
                PermissionCodes.QuotationViewPipeline, PermissionCodes.QuotationViewSummary, PermissionCodes.SalesOrderView,
                PermissionCodes.DeliveryView, PermissionCodes.InvoiceView, PermissionCodes.ReturnView,
            ]),
        (RoleNames.FinanceManager,
            [
                PermissionCodes.QuotationViewConversion, PermissionCodes.QuotationViewSummary, PermissionCodes.QuotationViewAllUnits,
                PermissionCodes.SalesOrderView, PermissionCodes.SalesOrderViewAllUnits,
                PermissionCodes.DeliveryView, PermissionCodes.DeliveryViewAllUnits,
                PermissionCodes.InvoiceView, PermissionCodes.InvoiceViewAllUnits,
                PermissionCodes.ReturnView, PermissionCodes.ReturnViewAllUnits,
            ]),
        (RoleNames.Viewer,
            [
                PermissionCodes.QuotationViewSummary, PermissionCodes.SalesOrderView,
                PermissionCodes.DeliveryView, PermissionCodes.InvoiceView, PermissionCodes.ReturnView,
            ]),
    ];

    /// <summary>
    /// Dev-only login users — one per seeded role (docs/requirements/Sales_Delivery_Module_BI_Developer_Guidelines.md §5),
    /// unit assignments matching each role's documented access pattern (all-units vs assigned-units). Fixed dev password,
    /// same "dev-only, replace once real thing exists" spirit as the dev JWT signing key in appsettings.Development.json.
    /// </summary>
    private const string SeedUserPassword = "Passw0rd!1";

    private static readonly (Guid Id, string Email, string DisplayName, string RoleName, string[] UnitNames)[] SeedUsers =
    [
        (Guid.Parse("66666666-6666-6666-6666-666666666601"), "admin@salesdeliverybi.dev", "Admin User",
            RoleNames.SuperAdmin, []),
        (Guid.Parse("66666666-6666-6666-6666-666666666602"), "commercial.manager@salesdeliverybi.dev", "Commercial Manager",
            RoleNames.CommercialManager, ["Unit-1"]),
        (Guid.Parse("66666666-6666-6666-6666-666666666603"), "merchandiser@salesdeliverybi.dev", "Merchandiser",
            RoleNames.Merchandiser, ["Unit-2"]),
        (Guid.Parse("66666666-6666-6666-6666-666666666604"), "general.manager@salesdeliverybi.dev", "General Manager",
            RoleNames.GeneralManager, []),
        (Guid.Parse("66666666-6666-6666-6666-666666666605"), "commercial.officer@salesdeliverybi.dev", "Commercial Officer",
            RoleNames.CommercialOfficer, ["Unit-1", "Unit-2"]),
        (Guid.Parse("66666666-6666-6666-6666-666666666606"), "finance.manager@salesdeliverybi.dev", "Finance Manager",
            RoleNames.FinanceManager, []),
        (Guid.Parse("66666666-6666-6666-6666-666666666607"), "viewer@salesdeliverybi.dev", "Viewer User",
            RoleNames.Viewer, ["Unit-3"]),
    ];

    private readonly AppDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(AppDbContext context, IPasswordHasher passwordHasher, ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        Dictionary<string, Unit> units = await SeedUnitsAsync(cancellationToken);
        Dictionary<string, Buyer> buyers = await SeedBuyersAsync(cancellationToken);
        Dictionary<string, Merchandiser> merchandisers = await SeedMerchandisersAsync(units, cancellationToken);
        Dictionary<string, Role> roles = await SeedRolesAsync(cancellationToken);
        await SeedRolePermissionsAsync(roles, cancellationToken);
        await SeedUsersAsync(units, roles, cancellationToken);

        List<SeedQuotationRecord> records = LoadSeedRecords();

        await SeedFxRatesAsync(records, cancellationToken);
        await SeedQuotationsAsync(records, units, buyers, merchandisers, cancellationToken);
        await SeedSalesOrdersAsync(cancellationToken);
        await SeedDeliveriesAsync(cancellationToken);
        await SeedInvoicesAsync(cancellationToken);
        await SeedReturnsAsync(cancellationToken);

        _logger.LogInformation("Database seeding complete: {UnitCount} units, {BuyerCount} buyers, {QuotationCount} quotations",
            units.Count, buyers.Count, records.Count);
    }

    /// <summary>
    /// Sales Order module (docs/plans, "MV instead of actual table" — discussed with the user): no OLTP
    /// table exists for this module, so bi.mv_sales_order_summary is a plain table seeded directly here,
    /// one row per already-Converted quotation. Backfills Quotation.ConvertedToSoNo (always null in the
    /// source seed data — never actually populated) with the generated SO number, closing the loop between
    /// the two modules. Idempotent by quotation_id; re-running always appends a fresh bi.mv_refresh_log row
    /// (mirrors a real MV refresh always logging a run, even one that changes nothing) so "Data as of"
    /// still advances the same way it would for a real refresh.
    /// </summary>
    private async Task SeedSalesOrdersAsync(CancellationToken cancellationToken)
    {
        List<Quotation> convertedQuotations = await _context.Quotations
            .Include(q => q.Buyer)
            .Include(q => q.Merchandiser)
            .Include(q => q.Unit)
            .Where(q => q.Status == QuotationStatus.Converted)
            .OrderBy(q => q.QuotationDate)
            .ToListAsync(cancellationToken);

        HashSet<Guid> existingQuotationIds = (await _context.Database
                .SqlQuery<Guid>($"SELECT quotation_id AS \"Value\" FROM bi.mv_sales_order_summary WHERE quotation_id IS NOT NULL")
                .ToListAsync(cancellationToken))
            .ToHashSet();

        int existingCount = await _context.Database
            .SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM bi.mv_sales_order_summary")
            .SingleAsync(cancellationToken);

        int nextSequence = existingCount + 1;

        foreach (Quotation quotation in convertedQuotations)
        {
            if (existingQuotationIds.Contains(quotation.Id))
            {
                continue;
            }

            string soNo = $"SO-2026-{nextSequence:D4}";
            nextSequence++;

            DateOnly soDate = DateOnly.FromDateTime(quotation.ConvertedDate ?? quotation.StatusDate);
            DateOnly promisedDeliveryDate = soDate.AddDays(30);

            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO bi.mv_sales_order_summary
                    (so_id, so_no, so_date, quotation_id, buyer_id, buyer_name, merchandiser_id, merchandiser_name,
                     unit_id, unit_name, currency_code, order_value_usd, delivered_value_usd, pending_value_usd,
                     status, promised_delivery_date, last_refresh_date)
                VALUES
                    ({quotation.Id}, {soNo}, {soDate}, {quotation.Id}, {quotation.BuyerId}, {quotation.Buyer!.BuyerName},
                     {quotation.MerchandiserId}, {quotation.Merchandiser!.MerchandiserName}, {quotation.UnitId}, {quotation.Unit!.UnitName},
                     {quotation.CurrencyCode}, {quotation.Value}, 0::numeric, {quotation.Value},
                     'Open', {promisedDeliveryDate}, now())
                """,
                cancellationToken);

            quotation.ConvertedToSoNo = soNo;
        }

        await _context.SaveChangesAsync(cancellationToken);

        int totalRows = await _context.Database
            .SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM bi.mv_sales_order_summary")
            .SingleAsync(cancellationToken);

        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO bi.mv_refresh_log (mv_name, started_at, finished_at, status, rows_affected)
            VALUES ('bi.mv_sales_order_summary', now(), now(), 'SUCCESS', {totalRows})
            """,
            cancellationToken);
    }

    private sealed record SalesOrderSeedRow(
        Guid SoId, string SoNo, DateOnly SoDate, Guid BuyerId, string BuyerName,
        Guid UnitId, string UnitName, decimal OrderValueUsd, DateOnly PromisedDeliveryDate);

    /// <summary>
    /// Delivery module (same "plain table, no OLTP source" pattern as Sales Order — discussed with the
    /// user). One full delivery per Sales Order: every 3rd ships 5 days past its promise date, the rest
    /// 5 days ahead of it, so the on-time-rate KPI has real variety instead of a trivial 100%/0%. Backfills
    /// the originating SalesOrder's delivered/pending value + status now that it's actually been delivered.
    /// </summary>
    private async Task SeedDeliveriesAsync(CancellationToken cancellationToken)
    {
        List<SalesOrderSeedRow> salesOrders = await _context.Database
            .SqlQuery<SalesOrderSeedRow>($"""
                SELECT so_id AS "SoId", so_no AS "SoNo", so_date AS "SoDate", buyer_id AS "BuyerId", buyer_name AS "BuyerName",
                       unit_id AS "UnitId", unit_name AS "UnitName", order_value_usd AS "OrderValueUsd",
                       promised_delivery_date AS "PromisedDeliveryDate"
                FROM bi.mv_sales_order_summary
                ORDER BY so_no
                """)
            .ToListAsync(cancellationToken);

        HashSet<Guid> existingSalesOrderIds = (await _context.Database
                .SqlQuery<Guid>($"SELECT sales_order_id AS \"Value\" FROM bi.mv_delivery_performance")
                .ToListAsync(cancellationToken))
            .ToHashSet();

        int existingCount = await _context.Database
            .SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM bi.mv_delivery_performance")
            .SingleAsync(cancellationToken);

        int nextSequence = existingCount + 1;

        for (int i = 0; i < salesOrders.Count; i++)
        {
            SalesOrderSeedRow so = salesOrders[i];
            if (existingSalesOrderIds.Contains(so.SoId))
            {
                continue;
            }

            string challanNo = $"CH-2026-{nextSequence:D4}";
            nextSequence++;

            bool late = i % 3 == 2;
            DateOnly deliveryDate = so.PromisedDeliveryDate.AddDays(late ? 5 : -5);
            int delayDays = deliveryDate.DayNumber - so.PromisedDeliveryDate.DayNumber;
            string deliveryStatus = delayDays > 0 ? "Late" : "On-Time";

            // delivery_id deliberately reuses so.SoId — same 1:1-per-order simplification as
            // SeedSalesOrdersAsync reusing the originating quotation's Id.
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO bi.mv_delivery_performance
                    (delivery_id, challan_no, delivery_date, sales_order_id, buyer_id, buyer_name, unit_id, unit_name,
                     delivered_value_usd, promised_date, delay_days, delivery_status, last_refresh_date)
                VALUES
                    ({so.SoId}, {challanNo}, {deliveryDate}, {so.SoId}, {so.BuyerId}, {so.BuyerName}, {so.UnitId}, {so.UnitName},
                     {so.OrderValueUsd}, {so.PromisedDeliveryDate}, {delayDays}, {deliveryStatus}, now())
                """,
                cancellationToken);

            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                UPDATE bi.mv_sales_order_summary
                SET delivered_value_usd = {so.OrderValueUsd}, pending_value_usd = 0::numeric, status = 'Delivered'
                WHERE so_id = {so.SoId}
                """,
                cancellationToken);
        }

        int totalRows = await _context.Database
            .SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM bi.mv_delivery_performance")
            .SingleAsync(cancellationToken);

        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO bi.mv_refresh_log (mv_name, started_at, finished_at, status, rows_affected)
            VALUES ('bi.mv_delivery_performance', now(), now(), 'SUCCESS', {totalRows})
            """,
            cancellationToken);
    }

    private sealed record DeliverySeedRow(
        Guid DeliveryId, Guid SalesOrderId, DateOnly DeliveryDate, Guid BuyerId, string BuyerName,
        Guid UnitId, string UnitName, decimal DeliveredValueUsd);

    /// <summary>
    /// Sales Invoice module (same "plain table, no OLTP source" pattern — discussed with the user). One
    /// invoice per Delivery, raised a few days after delivery, due in 30 days. Every 3rd invoice is fully
    /// paid at seed time; the rest are left unpaid so ArStatus/DaysOverdue (computed live in
    /// InvoiceRepository off CURRENT_DATE, never stored) show real "Current"/"Overdue" variety as time
    /// passes each seeded due_date — same live-computation convention as Quotation's days_open.
    /// </summary>
    private async Task SeedInvoicesAsync(CancellationToken cancellationToken)
    {
        List<DeliverySeedRow> deliveries = await _context.Database
            .SqlQuery<DeliverySeedRow>($"""
                SELECT delivery_id AS "DeliveryId", sales_order_id AS "SalesOrderId", delivery_date AS "DeliveryDate",
                       buyer_id AS "BuyerId", buyer_name AS "BuyerName", unit_id AS "UnitId", unit_name AS "UnitName",
                       delivered_value_usd AS "DeliveredValueUsd"
                FROM bi.mv_delivery_performance
                ORDER BY delivery_date
                """)
            .ToListAsync(cancellationToken);

        HashSet<Guid> existingDeliveryIds = (await _context.Database
                .SqlQuery<Guid>($"SELECT delivery_id AS \"Value\" FROM bi.mv_sales_invoice_summary")
                .ToListAsync(cancellationToken))
            .ToHashSet();

        int existingCount = await _context.Database
            .SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM bi.mv_sales_invoice_summary")
            .SingleAsync(cancellationToken);

        int nextSequence = existingCount + 1;

        for (int i = 0; i < deliveries.Count; i++)
        {
            DeliverySeedRow delivery = deliveries[i];
            if (existingDeliveryIds.Contains(delivery.DeliveryId))
            {
                continue;
            }

            string invoiceNo = $"INV-2026-{nextSequence:D4}";
            nextSequence++;

            DateOnly invoiceDate = delivery.DeliveryDate.AddDays(3);
            DateOnly dueDate = invoiceDate.AddDays(30);
            decimal paidAmount = i % 3 == 0 ? delivery.DeliveredValueUsd : 0m;

            // invoice_id deliberately reuses delivery.DeliveryId — same 1:1-per-delivery simplification.
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO bi.mv_sales_invoice_summary
                    (invoice_id, invoice_no, invoice_date, delivery_id, sales_order_id, buyer_id, buyer_name, unit_id, unit_name,
                     currency_code, invoice_value_usd, paid_amount_usd, due_date, last_refresh_date)
                VALUES
                    ({delivery.DeliveryId}, {invoiceNo}, {invoiceDate}, {delivery.DeliveryId}, {delivery.SalesOrderId},
                     {delivery.BuyerId}, {delivery.BuyerName}, {delivery.UnitId}, {delivery.UnitName},
                     'USD', {delivery.DeliveredValueUsd}, {paidAmount}, {dueDate}, now())
                """,
                cancellationToken);
        }

        int totalRows = await _context.Database
            .SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM bi.mv_sales_invoice_summary")
            .SingleAsync(cancellationToken);

        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO bi.mv_refresh_log (mv_name, started_at, finished_at, status, rows_affected)
            VALUES ('bi.mv_sales_invoice_summary', now(), now(), 'SUCCESS', {totalRows})
            """,
            cancellationToken);
    }

    private sealed record InvoiceSeedRow(
        Guid InvoiceId, DateOnly InvoiceDate, Guid BuyerId, string BuyerName,
        Guid UnitId, string UnitName, decimal InvoiceValueUsd);

    /// <summary>
    /// Return/Credit Note module (same "plain table, no OLTP source" pattern — discussed with the user).
    /// Deliberately just 2 of the 7 invoices get a return — a low, realistic return rate rather than an
    /// unrealistic one covering half the dataset.
    /// </summary>
    private async Task SeedReturnsAsync(CancellationToken cancellationToken)
    {
        List<InvoiceSeedRow> invoices = await _context.Database
            .SqlQuery<InvoiceSeedRow>($"""
                SELECT invoice_id AS "InvoiceId", invoice_date AS "InvoiceDate", buyer_id AS "BuyerId", buyer_name AS "BuyerName",
                       unit_id AS "UnitId", unit_name AS "UnitName", invoice_value_usd AS "InvoiceValueUsd"
                FROM bi.mv_sales_invoice_summary
                ORDER BY invoice_date
                """)
            .ToListAsync(cancellationToken);

        HashSet<Guid> existingInvoiceIds = (await _context.Database
                .SqlQuery<Guid>($"SELECT invoice_id AS \"Value\" FROM bi.mv_sales_return_summary")
                .ToListAsync(cancellationToken))
            .ToHashSet();

        int existingCount = await _context.Database
            .SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM bi.mv_sales_return_summary")
            .SingleAsync(cancellationToken);

        int nextSequence = existingCount + 1;
        (int Index, string ReasonCode)[] returnPlan = [(1, "Quality Issue"), (4, "Wrong Size")];

        foreach ((int index, string reasonCode) in returnPlan)
        {
            if (index >= invoices.Count)
            {
                continue;
            }

            InvoiceSeedRow invoice = invoices[index];
            if (existingInvoiceIds.Contains(invoice.InvoiceId))
            {
                continue;
            }

            string returnNo = $"CN-2026-{nextSequence:D4}";
            nextSequence++;

            DateOnly returnDate = invoice.InvoiceDate.AddDays(10);
            decimal returnValue = Math.Round(invoice.InvoiceValueUsd * 0.15m, 2);

            // return_id deliberately reuses invoice.InvoiceId — same 1:1-per-invoice simplification (each
            // invoice gets at most one return in this seed set).
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO bi.mv_sales_return_summary
                    (return_id, return_no, return_date, invoice_id, buyer_id, buyer_name, unit_id, unit_name,
                     return_value_usd, return_qty, reason_code, last_refresh_date)
                VALUES
                    ({invoice.InvoiceId}, {returnNo}, {returnDate}, {invoice.InvoiceId}, {invoice.BuyerId}, {invoice.BuyerName},
                     {invoice.UnitId}, {invoice.UnitName}, {returnValue}, 50, {reasonCode}, now())
                """,
                cancellationToken);
        }

        int totalReturnRows = await _context.Database
            .SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM bi.mv_sales_return_summary")
            .SingleAsync(cancellationToken);

        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO bi.mv_refresh_log (mv_name, started_at, finished_at, status, rows_affected)
            VALUES ('bi.mv_sales_return_summary', now(), now(), 'SUCCESS', {totalReturnRows})
            """,
            cancellationToken);
    }

    private async Task<Dictionary<string, Role>> SeedRolesAsync(CancellationToken cancellationToken)
    {
        Dictionary<string, Role> existing = await _context.Roles.ToDictionaryAsync(r => r.Name, cancellationToken);

        foreach ((Guid id, string name) in SeedRoles)
        {
            if (existing.ContainsKey(name))
            {
                continue;
            }

            var role = new Role { Id = id, Name = name };
            _context.Roles.Add(role);
            existing[name] = role;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    private async Task SeedRolePermissionsAsync(Dictionary<string, Role> roles, CancellationToken cancellationToken)
    {
        var existing = (await _context.RolePermissions
                .Select(rp => new { rp.RoleId, rp.PermissionCode })
                .ToListAsync(cancellationToken))
            .Select(rp => (rp.RoleId, rp.PermissionCode))
            .ToHashSet();

        foreach ((string roleName, string[] permissionCodes) in SeedRolePermissions)
        {
            Guid roleId = roles[roleName].Id;

            foreach (string permissionCode in permissionCodes)
            {
                if (existing.Contains((roleId, permissionCode)))
                {
                    continue;
                }

                _context.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionCode = permissionCode });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedUsersAsync(Dictionary<string, Unit> units, Dictionary<string, Role> roles, CancellationToken cancellationToken)
    {
        HashSet<string> existingEmails = (await _context.Users.Select(u => u.Email).ToListAsync(cancellationToken)).ToHashSet();

        foreach ((Guid id, string email, string displayName, string roleName, string[] unitNames) in SeedUsers)
        {
            if (existingEmails.Contains(email))
            {
                continue;
            }

            var user = new User
            {
                Id = id,
                Email = email,
                DisplayName = displayName,
                PasswordHash = _passwordHasher.Hash(SeedUserPassword),
                RoleId = roles[roleName].Id,
            };

            foreach (string unitName in unitNames)
            {
                user.UserUnits.Add(new UserUnit { UserId = id, UnitId = units[unitName].Id });
            }

            _context.Users.Add(user);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<Dictionary<string, Unit>> SeedUnitsAsync(CancellationToken cancellationToken)
    {
        Dictionary<string, Unit> existing = await _context.Units.ToDictionaryAsync(u => u.UnitName, cancellationToken);

        foreach ((Guid id, string name, string type) in SeedUnits)
        {
            if (existing.ContainsKey(name))
            {
                continue;
            }

            var unit = new Unit { Id = id, UnitName = name, UnitType = type };
            _context.Units.Add(unit);
            existing[name] = unit;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    private async Task<Dictionary<string, Buyer>> SeedBuyersAsync(CancellationToken cancellationToken)
    {
        Dictionary<string, Buyer> existing = await _context.Buyers.ToDictionaryAsync(b => b.BuyerName, cancellationToken);

        foreach ((Guid id, string name) in SeedBuyers)
        {
            if (existing.ContainsKey(name))
            {
                continue;
            }

            var buyer = new Buyer { Id = id, BuyerName = name };
            _context.Buyers.Add(buyer);
            existing[name] = buyer;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    private async Task<Dictionary<string, Merchandiser>> SeedMerchandisersAsync(
        Dictionary<string, Unit> units,
        CancellationToken cancellationToken)
    {
        Dictionary<string, Merchandiser> existing =
            await _context.Merchandisers.ToDictionaryAsync(m => m.MerchandiserName, cancellationToken);

        foreach ((Guid id, string name, string homeUnitName) in SeedMerchandisers)
        {
            if (existing.ContainsKey(name))
            {
                continue;
            }

            var merchandiser = new Merchandiser { Id = id, MerchandiserName = name, UnitId = units[homeUnitName].Id };
            _context.Merchandisers.Add(merchandiser);
            existing[name] = merchandiser;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    private async Task SeedFxRatesAsync(List<SeedQuotationRecord> records, CancellationToken cancellationToken)
    {
        HashSet<DateOnly> existingDates = (await _context.FxRates
                .Where(f => f.CurrencyCode == "USD")
                .Select(f => f.RateDate)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        IEnumerable<DateOnly> distinctDates = records
            .Select(r => DateOnly.Parse(r.QuotationDate, CultureInfo.InvariantCulture))
            .Distinct();

        foreach (DateOnly date in distinctDates)
        {
            if (existingDates.Contains(date))
            {
                continue;
            }

            _context.FxRates.Add(new FxRate { CurrencyCode = "USD", RateDate = date, RateToUsd = 1.0000m });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedQuotationsAsync(
        List<SeedQuotationRecord> records,
        Dictionary<string, Unit> units,
        Dictionary<string, Buyer> buyers,
        Dictionary<string, Merchandiser> merchandisers,
        CancellationToken cancellationToken)
    {
        HashSet<string> existingQuotationNos = (await _context.Quotations
                .Select(q => q.QuotationNo)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        foreach (SeedQuotationRecord record in records)
        {
            if (existingQuotationNos.Contains(record.QuotationNo))
            {
                continue;
            }

            Quotation quotation = BuildQuotation(record, units, buyers, merchandisers);
            _context.Quotations.Add(quotation);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static Quotation BuildQuotation(
        SeedQuotationRecord record,
        Dictionary<string, Unit> units,
        Dictionary<string, Buyer> buyers,
        Dictionary<string, Merchandiser> merchandisers)
    {
        DateOnly quotationDate = DateOnly.Parse(record.QuotationDate, CultureInfo.InvariantCulture);
        DateOnly? convertedDate = record.ConvertedDate is null
            ? null
            : DateOnly.Parse(record.ConvertedDate, CultureInfo.InvariantCulture);
        QuotationStatus status = Enum.Parse<QuotationStatus>(record.Status);
        Unit unit = units[record.UnitName];

        var quotation = new Quotation
        {
            QuotationNo = record.QuotationNo,
            QuotationDate = quotationDate,
            BuyerId = buyers[record.BuyerName].Id,
            MerchandiserId = merchandisers[record.MerchandiserName].Id,
            UnitId = unit.Id,
            StyleNo = record.StyleNo,
            Season = record.Season,
            CurrencyCode = "USD",
            Value = record.Value,
            Incoterm = "FOB",
            PaymentTerm = "30 Days",
            ValidUntil = quotationDate.AddDays(30),
            Discount = 0m,
            Status = status,
            StatusDate = ToUtcMidnight(convertedDate ?? quotationDate),
            ConvertedToSoNo = null,
            ConvertedDate = convertedDate is null ? null : ToUtcMidnight(convertedDate.Value),
            LostReason = record.LostReason,
        };

        foreach (QuotationItem item in BuildItems(record.StyleNo, record.Value, unit.UnitType))
        {
            quotation.Items.Add(item);
        }

        DateOnly historyEndDate = status == QuotationStatus.Converted && convertedDate.HasValue
            ? convertedDate.Value
            : quotationDate.AddDays(record.DaysOpen);

        foreach (QuotationStatusHistory entry in BuildStatusHistory(status, quotationDate, historyEndDate))
        {
            quotation.StatusHistory.Add(entry);
        }

        return quotation;
    }

    private static IEnumerable<QuotationItem> BuildItems(string quotationStyleNo, decimal value, string unitType)
    {
        (string description1, string description2) = ItemDescriptionsByUnitType[unitType];

        decimal qtyTotal = Math.Round(value / 6m / 100m) * 100m;
        int qty1 = (int)(Math.Round(qtyTotal * 0.6m / 100m) * 100m);
        decimal amount1 = qty1 * 6.00m;
        decimal amount2 = value - amount1;
        int qty2 = (int)Math.Max(10m, Math.Round(amount2 / 6m / 10m) * 10m);
        decimal unitPrice2 = Math.Round(amount2 / qty2, 2);

        yield return new QuotationItem
        {
            StyleNo = $"{quotationStyleNo}-01",
            ItemDescription = description1,
            Qty = qty1,
            UnitPrice = 6.00m,
            Amount = amount1,
        };

        yield return new QuotationItem
        {
            StyleNo = $"{quotationStyleNo}-02",
            ItemDescription = description2,
            Qty = qty2,
            UnitPrice = unitPrice2,
            Amount = amount2,
        };
    }

    private static IEnumerable<QuotationStatusHistory> BuildStatusHistory(
        QuotationStatus status,
        DateOnly startDate,
        DateOnly endDate)
    {
        QuotationStatus[] stages = BuildHistoryStages(status);

        for (int i = 0; i < stages.Length; i++)
        {
            DateOnly date = stages.Length == 1
                ? startDate
                : startDate.AddDays((endDate.DayNumber - startDate.DayNumber) * i / (stages.Length - 1));

            yield return new QuotationStatusHistory
            {
                Status = stages[i],
                StatusDate = ToUtcMidnight(date),
            };
        }
    }

    private static DateTime ToUtcMidnight(DateOnly date) =>
        DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

    private static QuotationStatus[] BuildHistoryStages(QuotationStatus status)
    {
        if (status is QuotationStatus.Rejected or QuotationStatus.Expired)
        {
            return [QuotationStatus.Draft, QuotationStatus.Submitted, QuotationStatus.Negotiation, status];
        }

        int index = Array.IndexOf(ForwardStatusOrder, status);
        return ForwardStatusOrder[..(index + 1)];
    }

    private static readonly JsonSerializerOptions SeedJsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static List<SeedQuotationRecord> LoadSeedRecords()
    {
        Assembly assembly = typeof(DatabaseSeeder).Assembly;
        const string resourceName = "SalesDeliveryBI.Infrastructure.Persistence.EfCore.Seed.seed-quotations.json";

        using Stream stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");

        List<SeedQuotationRecord>? records = JsonSerializer.Deserialize<List<SeedQuotationRecord>>(stream, SeedJsonOptions);

        return records ?? throw new InvalidOperationException("Seed quotations resource deserialized to null.");
    }
}
