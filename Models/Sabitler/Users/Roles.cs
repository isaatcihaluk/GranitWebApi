using System.ComponentModel.DataAnnotations.Schema;

namespace GranitWebApi.Models.Sabitler.Users
{
    [Table("Roles")]
    public class Roles
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        //public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }

    [Table("UserRoles")]
    public class UserRole
    {
        public int UserRoleId { get; set; }

        public int UserId { get; set; }
        //public User User { get; set; }

        public int RoleId { get; set; }
        //public Roles Role { get; set; }

        public DateTime CreatedDate { get; set; }

        public string? CreatedBy { get; set; }
    }
    public class UserRoleAssignDto
    {
        public int UserId { get; set; }

        public List<int> RoleIds { get; set; } = new();
    }
}