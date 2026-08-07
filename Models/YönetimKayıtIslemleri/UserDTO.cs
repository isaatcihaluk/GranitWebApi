namespace GranitWebApi.Models.YönetimKayıtIslemleri
{
    public class CreateUserDto
    {
        public string Sicil { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Gsm { get; set; }
        public string Username { get; set; }

        // Eski yapı için kalsın
        public int? roleId { get; set; }

        // Yeni çoklu rol yapısı
        public List<int> RoleIds { get; set; } = new();
        public int? departmentId { get; set; }
        public int? subDepartmentId { get; set; }
        public int? unitId { get; set; }
        public int? subUnitId { get; set; }
        public int? managerId { get; set; }
        public DateTime? startDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string CreatedUser { get; set; }
    }
    public class UpdateUserDto
    {
        public string Sicil { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Gsm { get; set; }
        public string Username { get; set; }
        // Eski kolon için
        public int? roleId { get; set; }

        // Yeni yapı
        public List<int> RoleIds { get; set; } = new();
        public int? departmentId { get; set; }
        public int? subDepartmentId { get; set; }
        public int? unitId { get; set; }
        public int? subUnitId { get; set; }
        public int? managerId { get; set; }
        public DateTime? startDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
    public class ChangePasswordRequest
    {
        public string NewPassword { get; set; } = "";
        public string ConfirmPassword { get; set; } = "";
    }
}