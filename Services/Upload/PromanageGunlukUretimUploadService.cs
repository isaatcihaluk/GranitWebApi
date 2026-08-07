using ClosedXML.Excel;
using GranitWebApi.Data;
using GranitWebApi.Models.Promanage;
using GranitWebApi.Models.Upload;
using GranitWebApi.Services.Upload.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GranitWebApi.Services.Upload;

public class PromanageGunlukUretimUploadService
    : IPromanageGunlukUretimUploadService
{
    private readonly AppDbContext _db;

    public PromanageGunlukUretimUploadService(AppDbContext db)
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

        var list = new List<PromanageGunlukUretim>();


        for (int row = 6; row <= lastRow; row++)
        {
            cancellationToken.ThrowIfCancellationRequested();


            var machineName = worksheet.Cell(row, 4)
                .GetString()
                .Trim();


            if (string.IsNullOrWhiteSpace(machineName))
                continue;


            var reportDate = GetDate(
                worksheet.Cell(row, 1));


            list.Add(new PromanageGunlukUretim
            {
                ReportDate = reportDate,


                Shift = worksheet.Cell(row, 2)
                    .GetString()
                    .Trim(),


                MachineGroup = worksheet.Cell(row, 3)
                    .GetString()
                    .Trim(),


                MachineName = machineName,


                Operator = GetNullableString(
                    worksheet.Cell(row, 5)),


                JobOrderNo = GetNullableString(
                    worksheet.Cell(row, 6)),


                StockName = GetNullableString(
                    worksheet.Cell(row, 7)),


                TotalProduced = GetDecimal(
                    worksheet.Cell(row, 8)),


                GoodProduced = GetDecimal(
                    worksheet.Cell(row, 9)),


                SourceFileName = file.FileName,

                CreatedBy = createdBy,

                CreatedAt = now,

                ImportDate = reportDate
            });
        }


        if (!list.Any())
        {
            return new PromanageOeeUploadResult
            {
                Success = false,
                Message = "Excel içerisinde aktarılacak veri bulunamadı.",
                TotalRows = 0,
                InsertedRows = 0,
                SkippedRows = 0,
                ErrorRows = 0
            };
        }



        // Aynı kayıtları tekrar ekleme kontrolü

        var reportDates = list
            .Select(x => x.ReportDate.Date)
            .Distinct()
            .ToList();


        var minDate = reportDates.Min();
        var maxDate = reportDates.Max().AddDays(1);


        var existingKeys = await _db.PromanageGunlukUretim
            .Where(x => x.ReportDate >= minDate &&
                        x.ReportDate < maxDate)
            .Select(x => new
            {
                x.ReportDate,
                x.Shift,
                x.MachineName,
                x.JobOrderNo
            })
            .ToListAsync(cancellationToken);



        var keySet = existingKeys
            .Select(x =>
                $"{x.ReportDate:yyyyMMdd}|{x.Shift}|{x.MachineName}|{x.JobOrderNo}")
            .ToHashSet();



        var newRows = list
            .Where(x =>
                !keySet.Contains(
                    $"{x.ReportDate:yyyyMMdd}|{x.Shift}|{x.MachineName}|{x.JobOrderNo}"))
            .ToList();



        if (newRows.Any())
        {
            await _db.PromanageGunlukUretim
                .AddRangeAsync(newRows, cancellationToken);


            await _db.SaveChangesAsync(cancellationToken);
        }



        return new PromanageOeeUploadResult
        {
            Success = true,

            Message = $"{newRows.Count} kayıt başarıyla eklendi.",

            TotalRows = list.Count,

            InsertedRows = newRows.Count,

            SkippedRows = list.Count - newRows.Count,

            ErrorRows = 0
        };
    }



    private static string? GetNullableString(IXLCell cell)
    {
        var value = cell.GetString().Trim();

        return string.IsNullOrWhiteSpace(value)
            ? null
            : value;
    }



    private static decimal GetDecimal(IXLCell cell)
    {
        var value = cell.GetString().Trim();


        if (string.IsNullOrWhiteSpace(value))
            return 0;


        value = value.Replace("%", "");



        if (decimal.TryParse(
            value,
            System.Globalization.NumberStyles.Any,
            new System.Globalization.CultureInfo("tr-TR"),
            out var result))
        {
            return result;
        }



        if (decimal.TryParse(
            value,
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out result))
        {
            return result;
        }


        return 0;
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