using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using EthanTcm.Application.Abstractions;

namespace EthanTcm.Infrastructure.Services;

internal static class CorrespondenceExecutiveReportBuilder
{
    public static DashboardMetricExportDto Build(string title, string definition, CorrespondenceMetric metric,
        DateTimeOffset generatedAt, IReadOnlyCollection<CorrespondenceMetricItem> items, DashboardExportFormat format)
    {
        var stem = $"correspondence_{metric}_{generatedAt:yyyyMMdd_HHmm}".ToLowerInvariant();
        return format == DashboardExportFormat.Pdf
            ? new(stem + ".pdf", "application/pdf", BuildPdf(title, definition, generatedAt, items))
            : new(stem + ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", BuildExcel(title, definition, generatedAt, items));
    }

    private static byte[] BuildExcel(string title, string definition, DateTimeOffset generatedAt, IReadOnlyCollection<CorrespondenceMetricItem> items)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Executive detail");
        sheet.ShowGridLines = false;
        sheet.SheetView.FreezeRows(7);
        sheet.Range("A1:L1").Merge().Value = "ETHAN TCM  |  CORRESPONDENCE EXECUTIVE REPORT";
        sheet.Range("A1:L1").Style.Font.SetBold().Font.SetFontColor(XLColor.White).Font.SetFontSize(15);
        sheet.Range("A1:L1").Style.Fill.SetBackgroundColor(XLColor.FromHtml("#0D385C"));
        sheet.Range("A1:L1").Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
        sheet.Row(1).Height = 30;
        sheet.Range("A3:L3").Merge().Value = title;
        sheet.Range("A3:L3").Style.Font.SetBold().Font.SetFontSize(18).Font.SetFontColor(XLColor.FromHtml("#14324A"));
        sheet.Range("A4:L4").Merge().Value = definition;
        sheet.Range("A4:L4").Style.Font.SetFontColor(XLColor.FromHtml("#516171"));
        sheet.Range("A4:L4").Style.Alignment.WrapText = true;
        sheet.Cell("A5").Value = "Generated"; sheet.Cell("B5").Value = generatedAt.LocalDateTime;
        sheet.Cell("B5").Style.DateFormat.Format = "yyyy-mm-dd hh:mm";
        sheet.Cell("D5").Value = "Records"; sheet.Cell("E5").Value = items.Count;
        sheet.Range("A5:E5").Style.Font.SetBold();
        var headers = new[] { "Reference", "Direction", "Subject", "Counterparty", "Status", "Priority", "Correspondence date", "Due date", "Owner", "Department", "Executive attention", "Days late" };
        for (var column = 0; column < headers.Length; column++) sheet.Cell(7, column + 1).Value = headers[column];
        var header = sheet.Range(7, 1, 7, headers.Length);
        header.Style.Font.SetBold().Font.SetFontColor(XLColor.White);
        header.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#176B91"));
        header.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        header.Style.Alignment.WrapText = true;
        sheet.Row(7).Height = 28;
        var row = 8;
        foreach (var item in items)
        {
            sheet.Cell(row, 1).Value = item.Reference;
            sheet.Cell(row, 2).Value = item.Direction.ToString();
            sheet.Cell(row, 3).Value = item.Subject;
            sheet.Cell(row, 4).Value = item.Counterparty;
            sheet.Cell(row, 5).Value = item.Status.ToString();
            sheet.Cell(row, 6).Value = item.Priority.ToString();
            sheet.Cell(row, 7).Value = item.CorrespondenceDate.ToDateTime(TimeOnly.MinValue);
            if (item.DueDate.HasValue) sheet.Cell(row, 8).Value = item.DueDate.Value.ToDateTime(TimeOnly.MinValue);
            sheet.Cell(row, 9).Value = item.AssignedTo ?? "Unassigned";
            sheet.Cell(row, 10).Value = item.Department ?? "-";
            sheet.Cell(row, 11).Value = item.Issue;
            sheet.Cell(row, 12).Value = item.DaysLate;
            sheet.Range(row, 7, row, 8).Style.DateFormat.Format = "yyyy-mm-dd";
            if (row % 2 == 0) sheet.Range(row, 1, row, 12).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#F3F7FA"));
            if (item.DaysLate > 0) sheet.Cell(row, 12).Style.Font.SetBold().Font.SetFontColor(XLColor.FromHtml("#B42318"));
            row++;
        }
        var lastRow = Math.Max(8, row - 1);
        sheet.Range(7, 1, lastRow, 12).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        sheet.Range(7, 1, lastRow, 12).Style.Border.InsideBorder = XLBorderStyleValues.Hair;
        sheet.Range(8, 1, lastRow, 12).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
        sheet.Range(8, 1, lastRow, 12).Style.Alignment.WrapText = true;
        var widths = new[] { 18d, 12, 30, 24, 20, 12, 18, 14, 22, 22, 30, 11 };
        for (var i = 0; i < widths.Length; i++) sheet.Column(i + 1).Width = widths[i];
        sheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        sheet.PageSetup.FitToPages(1, 0);
        sheet.PageSetup.SetRowsToRepeatAtTop(1, 7);
        sheet.PageSetup.Footer.Center.AddText("ETHAN TCM - Confidential executive report");
        sheet.PageSetup.Footer.Right.AddText(XLHFPredefinedText.PageNumber, XLHFOccurrence.AllPages);
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static byte[] BuildPdf(string title, string definition, DateTimeOffset generatedAt, IReadOnlyCollection<CorrespondenceMetricItem> items)
    {
        var pages = new List<string>(); var page = new StringBuilder(); var y = 510; var number = 0;
        void NewPage()
        {
            if (page.Length > 0) pages.Add(page.ToString());
            page = new StringBuilder(); number++; y = 510;
            Rect(page, 0, 0, 842, 595, "0.98 0.99 1 rg"); Rect(page, 28, 548, 786, 25, "0.05 0.22 0.36 rg");
            Text(page, 40, 556, 13, true, title, "1 1 1 rg");
            Text(page, 40, 530, 8, true, "ETHAN TCM | EXECUTIVE CORRESPONDENCE REPORT");
            Text(page, 520, 530, 8, false, $"Generated {generatedAt.ToLocalTime():yyyy-MM-dd HH:mm} | Page {number}");
            foreach (var line in Wrap(definition, 125).Take(2)) { Text(page, 40, y, 8, false, line, "0.25 0.32 0.39 rg"); y -= 11; }
            y -= 8; Rect(page, 36, y - 17, 770, 28, "0.07 0.31 0.45 rg");
            var labels = new[] { (42,"REFERENCE"), (130,"DIRECTION"), (196,"SUBJECT / COUNTERPARTY"), (388,"DUE DATE"), (452,"STATUS"), (548,"OWNER"), (650,"EXECUTIVE ATTENTION"), (776,"LATE") };
            foreach (var (x,label) in labels) Text(page, x, y - 4, 7, true, label, "1 1 1 rg"); y -= 23;
        }
        NewPage(); var index = 0;
        foreach (var item in items)
        {
            if (y < 55) NewPage();
            var height = 32; Rect(page, 36, y-height+7, 770, height, index++ % 2 == 0 ? "1 1 1 rg" : "0.93 0.96 0.98 rg");
            Text(page,42,y-5,7,true,Truncate(item.Reference,16)); Text(page,130,y-5,7,false,item.Direction.ToString());
            Text(page,196,y-5,7,true,Truncate(item.Subject,31)); Text(page,196,y-16,6,false,Truncate(item.Counterparty,38),"0.30 0.38 0.45 rg");
            Text(page,388,y-5,7,false,item.DueDate?.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)??"-"); Text(page,452,y-5,7,false,Truncate(item.Status.ToString(),20));
            Text(page,548,y-5,7,false,Truncate(item.AssignedTo??"Unassigned",18)); Text(page,650,y-5,7,false,Truncate(item.Issue,24));
            Text(page,786,y-5,7,true,item.DaysLate>0?item.DaysLate.ToString(CultureInfo.InvariantCulture):"-"); y -= height;
        }
        if (items.Count == 0) Text(page, 310, 300, 12, true, "No records in the selected scope.");
        pages.Add(page.ToString()); return Document(pages);
    }
    private static void Text(StringBuilder b,int x,int y,int size,bool bold,string value,string color="0.05 0.10 0.16 rg") => b.Append(color).Append("\nBT /").Append(bold?"F2":"F1").Append(' ').Append(size).Append(" Tf ").Append(x).Append(' ').Append(y).Append(" Td (").Append(Escape(Safe(value))).Append(") Tj ET\n");
    private static void Rect(StringBuilder b,int x,int y,int w,int h,string color) => b.Append(color).Append('\n').Append(x).Append(' ').Append(y).Append(' ').Append(w).Append(' ').Append(h).Append(" re f\n");
    private static string Truncate(string value,int max) => value.Length<=max?value:value[..(max-3)]+"...";
    private static IEnumerable<string> Wrap(string value,int max) { var words=value.Split(' ',StringSplitOptions.RemoveEmptyEntries); var line=new StringBuilder(); foreach(var word in words){if(line.Length>0&&line.Length+word.Length+1>max){yield return line.ToString();line.Clear();}if(line.Length>0)line.Append(' ');line.Append(word);}if(line.Length>0)yield return line.ToString();}
    private static string Escape(string value) => value.Replace("\\","\\\\").Replace("(","\\(").Replace(")","\\)");
    private static string Safe(string value) { var b=new StringBuilder(); foreach(var c in value.Normalize(NormalizationForm.FormD)) if(c<=127&&!char.IsControl(c)) b.Append(c); return b.ToString(); }
    private static byte[] Document(IReadOnlyList<string> contents)
    {
        var objects=new List<string>{"<< /Type /Catalog /Pages 2 0 R >>",""}; var pageIds=new List<int>();
        foreach(var content in contents){var streamId=objects.Count+1; objects.Add($"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}endstream"); var pageId=objects.Count+1; pageIds.Add(pageId); objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 842 595] /Resources << /Font << /F1 << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> /F2 << /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >> >> >> /Contents {streamId} 0 R >>");}
        objects[1]=$"<< /Type /Pages /Kids [{string.Join(' ',pageIds.Select(id=>$"{id} 0 R"))}] /Count {pageIds.Count} >>";
        var output=new StringBuilder("%PDF-1.4\n"); var offsets=new List<int>{0}; for(var i=0;i<objects.Count;i++){offsets.Add(Encoding.ASCII.GetByteCount(output.ToString()));output.Append(i+1).Append(" 0 obj\n").Append(objects[i]).Append("\nendobj\n");}
        var xref=Encoding.ASCII.GetByteCount(output.ToString()); output.Append("xref\n0 ").Append(objects.Count+1).Append("\n0000000000 65535 f \n"); foreach(var offset in offsets.Skip(1))output.Append(offset.ToString("D10")).Append(" 00000 n \n"); output.Append("trailer\n<< /Size ").Append(objects.Count+1).Append(" /Root 1 0 R >>\nstartxref\n").Append(xref).Append("\n%%EOF"); return Encoding.ASCII.GetBytes(output.ToString());
    }
}
