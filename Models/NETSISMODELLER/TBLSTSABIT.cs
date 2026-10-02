using Microsoft.EntityFrameworkCore;

namespace GranitWebApi.Models.NETSISMODELLER
{
    [Keyless]
    public class TBLSTSABIT
    {
        public string STOK_KODU { get; set; }
        public string STOK_ADI { get; set; }
    }
    public class ProductGroupResult
    {
        public string GrupKod { get; set; } = string.Empty;
        public string GrupIsim { get; set; } = string.Empty;
    }
}
