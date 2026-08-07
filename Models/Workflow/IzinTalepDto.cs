namespace GranitWebApi.Models.Workflow
{
    public class IzinTalepDto
    {
        public int UserId { get; set; }
        public int IzinTuruId { get; set; }
        public DateTime Baslangic { get; set; }
        public DateTime Bitis { get; set; }
        public string Aciklama { get; set; }
    }
}
