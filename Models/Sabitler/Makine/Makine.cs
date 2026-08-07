using System.ComponentModel.DataAnnotations;

namespace GranitWebApi.Models.Sabitler.Makine
{
    public class Makine
    {
        [Key]
        public string MachineCode { get; set; } = null!;

        public string MachineName { get; set; } = null!;

        public string? Marka { get; set; }

        public string? Bölüm { get; set; }

        public string? Opersayon { get; set; }

        public string? SeriNo { get; set; }
    }


    public class MakineBilesen
    {
        [Key]
        public int ComponentId { get; set; }

        public string ComponentCode { get; set; } = null!;

        public string ComponentName { get; set; } = null!;
    }


    public class MakineBilesenHaritasi
    {
        [Key]
        public int Id { get; set; }

        public string MachineCode { get; set; } = null!;

        public int ComponentId { get; set; }
    }
    public class MachineFilterDto
    {
        public string MachineName { get; set; }
        public string MachineGroup { get; set; }
    }
}