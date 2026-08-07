using Microsoft.EntityFrameworkCore;

namespace GranitWebApi.Models.NETSISMODELLER
{
    [Keyless]
    public class TBLSTSABIT
    {
        public string STOK_KODU { get; set; }
        public string STOK_ADI { get; set; }
    }
}
