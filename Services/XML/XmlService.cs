using GranitWebApi.Models.XML;
using GranitWebApi.Services.XML;
using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;
using System.Net;

public class XmlService : IXmlService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public XmlService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<List<TechnicianModel>> FetchTechnicians()
    {
        var client = _httpClientFactory.CreateClient();

        var byteArray = Encoding.ASCII.GetBytes("a:a");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

        var response = await client.GetAsync("http://192.168.5.100:2700/drk92/BIRT/TechnicianList?filter=1&machine=");

        var xml = await response.Content.ReadAsStringAsync();

        xml = WebUtility.HtmlDecode(xml);

        var doc = XDocument.Parse(xml);

        var list = new List<TechnicianModel>();

        foreach (var item in doc.Descendants("BITECHNICIAN"))
        {
            list.Add(new TechnicianModel
            {
                RN = int.TryParse(item.Element("RN")?.Value, out var rn) ? rn : 0,
                M = int.TryParse(item.Element("M")?.Value, out var m) ? m : 0,

                MachineCode = item.Element("MACHINECODE")?.Value,
                MachineName = item.Element("MACHINENAME")?.Value,

                Operator = int.TryParse(item.Element("OPERATOR")?.Value, out var op) ? op : 0,
                OperatorName = item.Element("OPERATORNAME")?.Value,
                OperatorCode = item.Element("OPERATORCODE")?.Value,

                Technician = int.TryParse(item.Element("TECHNICIAN")?.Value, out var tech) ? tech : 0,
                TechnicianCode = item.Element("TECHNICANCODE")?.Value,
                TechnicianName = item.Element("TECHNICIANNAME")?.Value,

                TCall = DateTime.TryParse(item.Element("TCALL")?.Value, out var tcall) ? tcall : DateTime.MinValue,
                TLogin = DateTime.TryParse(item.Element("TLOGIN")?.Value, out var tlogin) ? tlogin : DateTime.MinValue,
                TEnd = DateTime.TryParse(item.Element("TEND")?.Value, out var tend) ? tend : DateTime.MinValue,

                Duration = TimeSpan.Parse(item.Element("DURATION")?.Value),
                ReactionTime = TimeSpan.Parse(item.Element("REACTIONTIME")?.Value),

                Shift = int.TryParse(item.Element("SHIFT")?.Value, out var shift) ? shift : 0,
                Status= "Açık",
            });
        }

        return list;
    }
    private TimeSpan? ParseTimeSpan(string? value)
    {
        return TimeSpan.TryParse(value, out var result) ? result : null;
    }

    private DateTime? ParseDateTime(string? value)
    {
        return DateTime.TryParse(value, out var result) ? result : null;
    }
}