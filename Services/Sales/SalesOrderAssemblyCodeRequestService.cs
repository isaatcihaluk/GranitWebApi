using GranitWebApi.Data;
using GranitWebApi.Models.Sales;
using Microsoft.EntityFrameworkCore;

namespace GranitWebApi.Services.Sales
{
    public class SalesOrderAssemblyCodeRequestService: ISalesOrderAssemblyCodeRequestService
    {
        private readonly AppDbContext _context;
        public SalesOrderAssemblyCodeRequestService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<SalesOrderAssemblyCodeRequest>>GetBySalesOrderLineIdAsync(long salesOrderLineId)
        {
            return await _context.SalesOrderAssemblyCodeRequests
                .Where(x => x.SalesOrderLineId == salesOrderLineId)
                .OrderByDescending(x => x.RequestedAt)
                .ToListAsync();
        }
        public async Task<SalesOrderAssemblyCodeRequest?>GetByIdAsync(long id)
        {
            return await _context.SalesOrderAssemblyCodeRequests.FirstOrDefaultAsync(x => x.Id == id);
        }
        public async Task<SalesOrderAssemblyCodeRequest>CreateAsync(SalesOrderAssemblyCodeRequest request)
        {
            var lineExists = await _context.SalesOrderLines.AnyAsync(x => x.Id == request.SalesOrderLineId);

            if (!lineExists)
            {
                throw new Exception("Sipariş satırı bulunamadı.");
            }

            var configuration = await _context.SalesOrderLineCKConfigurations.FirstOrDefaultAsync(x => x.Id == request.ConfigurationId);

            if (configuration == null)
            {
                throw new Exception("CK configuration kaydı bulunamadı.");
            }

            if (configuration.SalesOrderLineId != request.SalesOrderLineId)
            {
                throw new Exception("Seçilen configuration ilgili sipariş satırına ait değil.");
            }

            request.Id = 0;

            if (string.IsNullOrWhiteSpace(request.RequestStatus))
            {
                request.RequestStatus = "BEKLIYOR";
            }
            request.RequestedAt = DateTime.Now;
            _context.SalesOrderAssemblyCodeRequests.Add(request);
            await _context.SaveChangesAsync();
            return request;
        }

        public async Task<SalesOrderAssemblyCodeRequest?>UpdateAsync(long id,SalesOrderAssemblyCodeRequest request)
        {
            var existing = await _context.SalesOrderAssemblyCodeRequests.FirstOrDefaultAsync(x => x.Id == id);

            if (existing == null)
            {
                return null;
            }

            existing.MainAssemblyCode =request.MainAssemblyCode;
            existing.ManualAssemblyCode =request.ManualAssemblyCode;
            existing.RequestStatus =request.RequestStatus;
            existing.CompletedAt =request.CompletedAt;
            existing.CompletedBy =request.CompletedBy;
            existing.Note =request.Note;
            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var existing = await _context.SalesOrderAssemblyCodeRequests.FirstOrDefaultAsync(x => x.Id == id);

            if (existing == null)
            {
                return false;
            }

            _context.SalesOrderAssemblyCodeRequests.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
