using GranitWebApi.Workflow.Models;
using System.ComponentModel.DataAnnotations;

namespace GranitWebApi.Models.IK.Izin
{
    public class IzinTurleri
    {
        public int Id { get; set; }
        public int? PolimekID { get; set; }
        public string Ad { get; set; } = string.Empty;
        public string? Aciklama { get; set; }
        public bool Aktif { get; set; }
        public DateTime OlusturmaTarihi { get; set; }
        public ICollection<IzinTalepleri> IzinTalep { get; set; }
            = new List<IzinTalepleri>();
    }

    public class IzinTalepleri
    {
        public int Id { get; set; }
        public int PersonelId { get; set; }
        public int IzinTuruId { get; set; }
        public DateTime BaslangicTarihi { get; set; }
        public DateTime BitisTarihi { get; set; }
        public decimal GunSayisi { get; set; }
        public string? Aciklama { get; set; }     
        public int? ProcessRequestId { get; set; }
        public string Durum { get; set; } = "Taslak";
        public DateTime OlusturmaTarihi { get; set; }
        public DateTime? GuncellemeTarihi { get; set; }
        public IzinTurleri? IzinTuru { get; set; }
        public ProcessRequest ProcessRequest { get; set; } = null!;
        public bool Aktif { get; set; } = true;
        public bool BakiyeDusuldu { get; set; }
    }
    public class IzinTalebiOlusturModel
    {
        public int IzinTuruId { get; set; }

        public DateTime BaslangicTarihi { get; set; }

        public DateTime BitisTarihi { get; set; }

        public decimal GunSayisi { get; set; }

        public string? Aciklama { get; set; }
    }
    public class PersonelOrganizasyonDto
    {
        public int Id { get; set; }

        public string Sicil { get; set; } = "";

        public string FirstName { get; set; } = "";

        public string LastName { get; set; } = "";

        public string? DepartmentName { get; set; }

        public string? SubDepartmentName { get; set; }

        public string? UnitName { get; set; }

        public string? SubUnitName { get; set; }
    }
    public class IzinYillikBakiyePersonel
    {
        [Key]
        public long PersonelId { get; set; }
        public string? Sicil { get; set; }
        public string? PersonelAd { get; set; }
        public decimal Bakiye { get; set; }
    }
}
