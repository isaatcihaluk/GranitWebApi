using GranitWebApi.Models.Trendyol;

namespace GranitWebApi.Services.Trendyol
{
    public interface ITrendyolService
    {
        Task<List<TrendyolOrder>> GetOrdersAsync();
        Task ImportOrdersAsync();
    }
}