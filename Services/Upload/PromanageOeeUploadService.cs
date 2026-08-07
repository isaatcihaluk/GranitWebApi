using ClosedXML.Excel;
using GranitWebApi.Data;
using GranitWebApi.Models.Promanage;
using GranitWebApi.Models.Upload;
using GranitWebApi.Services.Upload.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace GranitWebApi.Services.Upload;

public class PromanageOeeUploadService : IPromanageOeeUploadService
{
    private readonly AppDbContext _db;

    public PromanageOeeUploadService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PromanageOeeUploadResult> UploadAsync(
    IFormFile file,
    string createdBy,
    CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            throw new Exception("Dosya bulunamadı.");

        if (!Path.GetExtension(file.FileName)
            .Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            throw new Exception("Sadece .xlsx dosyaları yüklenebilir.");

        using var stream = new MemoryStream();

        await file.CopyToAsync(stream, cancellationToken);

        stream.Position = 0;

        using var workbook = new XLWorkbook(stream);

        var worksheet = workbook.Worksheet(1);

        var lastRowUsed = worksheet.LastRowUsed();

        if (lastRowUsed == null)
            throw new Exception("Excel içerisinde veri bulunamadı.");

        int lastRow = lastRowUsed.RowNumber();

        var now = DateTime.Now;

        var list = new List<PromanageOee>();

        for (int row = 6; row <= lastRow; row++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var machineName = worksheet.Cell(row, 4).GetString().Trim();

            if (string.IsNullOrWhiteSpace(machineName))
                continue;

            var reportDate = GetDate(worksheet.Cell(row, 1));

            var availability = GetDecimal(worksheet.Cell(row, 5));
            var performance = GetDecimal(worksheet.Cell(row, 6));
            var quality = GetDecimal(worksheet.Cell(row, 7));
            var oee = GetDecimal(worksheet.Cell(row, 8));

            if (availability > 1000 ||
                performance > 1000 ||
                quality > 1000 ||
                oee > 1000)
            {
                throw new Exception(
                    $"Satır:{row}  A:{availability} P:{performance} Q:{quality} OEE:{oee}");
            }

            list.Add(new PromanageOee
            {
                ReportDate = reportDate,
                Shift = worksheet.Cell(row, 2).GetString().Trim(),
                MachineGroup = worksheet.Cell(row, 3).GetString().Trim(),
                MachineName = machineName,

                Availability = GetDecimal(worksheet.Cell(row, 5)),
                Performance = GetDecimal(worksheet.Cell(row, 6)),
                Quality = GetDecimal(worksheet.Cell(row, 7)),
                Oee = GetDecimal(worksheet.Cell(row, 8)),

                SourceFileName = file.FileName,
                CreatedBy = createdBy,
                CreatedAt = now,
                ImportDate = reportDate
            });
        }

        if (!list.Any())
            throw new Exception("Excel içerisinde aktarılacak veri bulunamadı.");

        // Excel tek günlük OEE raporu olduğu için ilk tarihi alıyoruz
        var excelReportDate = list.First().ReportDate.Date;

        var nextDate = excelReportDate.AddDays(1);

        // Aynı gün daha önce yüklenmiş mi?
        var exists = await _db.PromanageOee
            .AnyAsync(x => x.ReportDate >= excelReportDate &&
                           x.ReportDate < nextDate,
                           cancellationToken);

        if (exists)
        {
            throw new Exception(
                $"{excelReportDate:dd.MM.yyyy} tarihine ait OEE verileri daha önce yüklenmiş.");
        }

        // Toplu kayıt
        await _db.PromanageOee.AddRangeAsync(list, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return new PromanageOeeUploadResult
        {
            Success = true,
            Message = $"{list.Count} kayıt başarıyla eklendi.",

            TotalRows = list.Count,
            InsertedRows = list.Count,
            SkippedRows = 0,
            ErrorRows = 0
        };
    }
    private static decimal GetDecimal(IXLCell cell)
    {
        if (cell.IsEmpty())
            return 0;

        // Hücre gerçekten sayısal ise direkt oku
        if (cell.DataType == XLDataType.Number)
        {
            return Convert.ToDecimal(cell.GetDouble());
        }

        var value = cell.GetString().Trim();

        if (string.IsNullOrWhiteSpace(value))
            return 0;

        value = value.Replace("%", "");

        if (decimal.TryParse(
            value,
            NumberStyles.Any,
            CultureInfo.GetCultureInfo("tr-TR"),
            out var result))
            return result;

        if (decimal.TryParse(
            value,
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            out result))
            return result;

        throw new Exception(
            $"Sayısal değer okunamadı. Hücre: {cell.Address} Değer: '{value}'");
    }
    private static DateTime GetDate(IXLCell cell)
    {
        if (cell.DataType == XLDataType.DateTime)
            return cell.GetDateTime().Date;

        if (DateTime.TryParse(
            cell.GetString(),
            new System.Globalization.CultureInfo("tr-TR"),
            System.Globalization.DateTimeStyles.None,
            out var date))
        {
            return date.Date;
        }

        throw new Exception($"Geçersiz tarih: {cell.Address}");
    }
}