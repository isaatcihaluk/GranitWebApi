using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GranitWebApi.Models
{
    [Table("BankAccounts")]
    public class BankAccount
    {
        [Key]
        public int ID { get; set; }

        [MaxLength(150)]
        public string? BankName { get; set; }

        [MaxLength(150)]
        public string? AccountOwner { get; set; }

        [MaxLength(50)]
        public string? Branch { get; set; }

        [MaxLength(50)]
        public string? SwiftCode { get; set; }

        [MaxLength(50)]
        public string? IBAN { get; set; }
    }
}