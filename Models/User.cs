using GranitWebApi.Models.Sabitler.Users;

namespace GranitWebApi.Models
{
    public class User
    {
        public int Id { get; set; }
        public string? Sicil { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? GSM { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public int? RoleId { get; set; }
        public int? DepartmentId { get; set; }
        public int? ManagerId { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiry { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? CreatedUser { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedDate { get; set; }
        public int? DeletedUserId { get; set; }
        public int? PersonelId { get; set; }
        public int? SubDepartmentId { get; set; }
        public int? UnitId { get; set; }
        public int? SubUnitId { get; set; }
        public DateTime? LastSync { get; set; }
        public bool MustChangePassword { get; set; }
        public bool IsManual { get; set; }
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}