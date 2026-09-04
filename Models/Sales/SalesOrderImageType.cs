using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace GranitWebApi.Models.Sales
{
    [Table("SalesOrderImageTypes")]
    public class SalesOrderImageType
    {
        [Key]
        public int Id { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public bool IsActive { get; set; }
        public int DisplayOrder { get; set; }

        // Navigation
        [JsonIgnore]
        public ICollection<SalesOrderLineImage> Images { get; set; }= new List<SalesOrderLineImage>();
    }
}