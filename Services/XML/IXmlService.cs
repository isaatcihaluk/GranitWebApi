using GranitWebApi.Models.XML;

namespace GranitWebApi.Services.XML
{
    public interface IXmlService
    {
        Task<List<TechnicianModel>> FetchTechnicians();
    }
}
