using ClosedXML.Excel;
using SalesDeliveryBI.Application.Dtos;

namespace SalesDeliveryBI.Application.Common;

/// <summary>Builds the Pipeline dashboard's "Export to Excel" workbook (docs/requirements §4.1) from the same rows the grid renders.</summary>
public static class QuotationExcelExporter
{
    public static byte[] BuildPipelineWorkbook(IReadOnlyList<OpenQuotationDto> rows)
    {
        using var workbook = new XLWorkbook();
        IXLWorksheet sheet = workbook.Worksheets.Add("Open Quotations");

        string[] headers = ["Quotation No.", "Buyer", "Merchandiser", "Unit", "Value (USD)", "Status", "Days Open"];
        for (int col = 0; col < headers.Length; col++)
        {
            IXLCell cell = sheet.Cell(1, col + 1);
            cell.Value = headers[col];
            cell.Style.Font.Bold = true;
        }

        for (int i = 0; i < rows.Count; i++)
        {
            OpenQuotationDto row = rows[i];
            int excelRow = i + 2;
            sheet.Cell(excelRow, 1).Value = row.QuotationNo;
            sheet.Cell(excelRow, 2).Value = row.BuyerName;
            sheet.Cell(excelRow, 3).Value = row.MerchandiserName;
            sheet.Cell(excelRow, 4).Value = row.UnitName;
            sheet.Cell(excelRow, 5).Value = row.ValueUsd;
            sheet.Cell(excelRow, 6).Value = row.Status;
            sheet.Cell(excelRow, 7).Value = row.DaysOpen;
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
