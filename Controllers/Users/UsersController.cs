using GranitWebApi.Data;
using GranitWebApi.Models.Sabitler.Users;
using GranitWebApi.Models.YönetimKayıtIslemleri;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace GranitWebApi.Controllers.Users
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _db;
        public UsersController(IConfiguration configuration, AppDbContext db)
        {
            _configuration = configuration;
            _db = db;
        }
        //[Authorize]
        [HttpGet("list")]
        public IActionResult GetUsers()
        {
            string connStr = _configuration.GetConnectionString("DefaultConnection");

            List<object> users = new List<object>();

            using SqlConnection conn = new SqlConnection(connStr);
            conn.Open();


            string query = @"
                    SELECT
                        u.Id,
                        u.Sicil,
                        u.FirstName,
                        u.LastName,
                        u.Email,
                        u.GSM,
                        u.Username,
                        u.StartDate,
                        u.EndDate,
                        u.DepartmentId,
                        u.SubDepartmentId,
                        u.UnitId,
                        u.SubUnitId,
                        u.ManagerId,
                        u.IsManual,
                        u.MustChangePassword,
                        ur.RoleId
                    FROM Users u
                    LEFT JOIN UserRoles ur ON ur.UserId = u.Id
                    WHERE u.IsDeleted = 0
                    ORDER BY u.FirstName, u.LastName";


            using SqlCommand cmd = new SqlCommand(query, conn);
            using SqlDataReader reader = cmd.ExecuteReader();

            var userDict = new Dictionary<int, dynamic>();

            while (reader.Read())
            {
                int id = Convert.ToInt32(reader["Id"]);

                if (!userDict.ContainsKey(id))
                {
                    userDict.Add(id, new
                    {
                        id = id,
                        sicil = reader["Sicil"],
                        firstName = reader["FirstName"],
                        lastName = reader["LastName"],
                        email = reader["Email"] == DBNull.Value? "": reader["Email"].ToString(),
                        gsm = reader["GSM"] == DBNull.Value? "": reader["GSM"].ToString(),
                        username = reader["Username"],
                        roleIds = new List<int>(),
                        departmentID = reader["DepartmentId"] == DBNull.Value? null: reader["DepartmentId"],
                        subDepartmentID = reader["SubDepartmentId"] == DBNull.Value? null: reader["SubDepartmentId"],
                        unitID = reader["UnitId"] == DBNull.Value? null: reader["UnitId"],
                        subUnitID = reader["SubUnitId"] == DBNull.Value? null: reader["SubUnitId"],
                        managerID = reader["ManagerId"] == DBNull.Value? null: reader["ManagerId"],
                        startdate = reader["StartDate"] == DBNull.Value? null: reader["StartDate"],
                        enddate = reader["EndDate"] == DBNull.Value? null: reader["EndDate"],
                        isManual = reader["IsManual"],
                        mustChangePassword = reader["MustChangePassword"]
                    });
                }

                if (reader["RoleId"] != DBNull.Value)
                {
                    userDict[id].roleIds.Add(Convert.ToInt32(reader["RoleId"]));
                }
            }
            return Ok(userDict.Values);
        }

        [HttpPost]
        public async Task<IActionResult> AddUser(CreateUserDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (request.RoleIds == null || !request.RoleIds.Any())
            {
                return BadRequest("En az bir rol seçilmelidir.");
            }

            var hasher = new PasswordHasher<string>();
            // 🔐 Sicil → password hash
            string passwordHash = hasher.HashPassword(null, request.Sicil);
            int userId;
            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                conn.Open();
                using SqlCommand cmd = new SqlCommand("granitSP_User_Insert", conn);

                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Sicil", request.Sicil);
                cmd.Parameters.AddWithValue("@FirstName", request.FirstName);
                cmd.Parameters.AddWithValue("@LastName", request.LastName);
                cmd.Parameters.AddWithValue("@Email", request.Email ?? "");
                cmd.Parameters.AddWithValue("@GSM", request.Gsm ?? "");
                cmd.Parameters.AddWithValue("@Username", request.Sicil);

                // 🔐 Password
                cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
                cmd.Parameters.AddWithValue("@RoleId", request.RoleIds.First());
                cmd.Parameters.AddWithValue("@DepartmentId", (object?)request.departmentId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@SubDepartmentId", (object?)request.subDepartmentId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@UnitId", (object?)request.unitId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@SubUnitId", (object?)request.subUnitId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ManagerId", (object?)request.managerId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@StartDate", (object?)request.startDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@EndDate", (object?)request.EndDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CreatedUser", request.CreatedUser);
                cmd.Parameters.AddWithValue("@IsManual", true);
                userId = Convert.ToInt32(cmd.ExecuteScalar());
            }

            // 🔐 Çoklu roller UserRoles tablosuna yazılır
            foreach (var roleId in request.RoleIds.Distinct())
            {
                _db.UserRoles.Add(new UserRole
                {
                    UserId = userId,
                    RoleId = roleId,
                    CreatedDate = DateTime.Now,
                    CreatedBy = request.CreatedUser
                });
            }
            await _db.SaveChangesAsync();

            return Ok(new
            {
                id = userId,
                message = "Kullanıcı ve roller başarıyla oluşturuldu"
            });
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteUser(int id)
        {
            try
            {
                var deletedUserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");

                using SqlConnection conn = new SqlConnection(
                    _configuration.GetConnectionString("DefaultConnection"));

                conn.Open();

                using SqlCommand cmd = new SqlCommand("granitSP_User_Delete", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@DeletedUserId", deletedUserId);

                cmd.ExecuteNonQuery();

                return Ok(new { message = "Silindi" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public IActionResult UpdateUser(int id, UpdateUserDto request)
        {
            try
            {
                if (request.RoleIds == null || !request.RoleIds.Any())
                {
                    return BadRequest("En az bir rol seçilmelidir.");
                }

                using SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                conn.Open();

                // Kullanıcı bilgileri
                using (SqlCommand cmd = new SqlCommand("granitSP_User_Update",conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@Sicil", request.Sicil);
                    cmd.Parameters.AddWithValue("@FirstName", request.FirstName);
                    cmd.Parameters.AddWithValue("@LastName", request.LastName);
                    cmd.Parameters.AddWithValue("@Username", request.Sicil);
                    cmd.Parameters.AddWithValue("@Email",request.Email ?? "");
                    cmd.Parameters.AddWithValue("@GSM",request.Gsm ?? "");

                    // eski yapıyla uyum için ilk rol
                    cmd.Parameters.AddWithValue("@RoleId",request.RoleIds.First());
                    cmd.Parameters.AddWithValue("@DepartmentId",(object?)request.departmentId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SubDepartmentId",(object?)request.subDepartmentId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@UnitId",(object?)request.unitId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SubUnitId",(object?)request.subUnitId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ManagerId",(object?)request.managerId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@StartDate",(object?)request.startDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@EndDate",(object?)request.EndDate ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                // Roller güncelle
                var roleString = string.Join(",", request.RoleIds.Distinct());

                using (SqlCommand roleCmd = new SqlCommand(
                    "granitSP_UserRole_Update",
                    conn))
                {
                    roleCmd.CommandType = CommandType.StoredProcedure;

                    roleCmd.Parameters.AddWithValue("@UserId", id);
                    roleCmd.Parameters.AddWithValue("@RoleIds", roleString);

                    var affected = roleCmd.ExecuteNonQuery();


                    return Ok(new
                    {
                        message = "Test",
                        userId = id,
                        roles = roleString,
                        affected = affected
                    });
                }

            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpGet("usersbyid/{id}")]
        public IActionResult GetUsersById(int id)
        {
            string connStr = _configuration.GetConnectionString("DefaultConnection");

            List<object> users = new List<object>();

            using SqlConnection conn = new SqlConnection(connStr);
            conn.Open();

            string query = @"
                SELECT 
                    Id,
                    Sicil,
                    FirstName,
                    LastName,
                    Email,
                    GSM,
                    Username,
                    StartDate,
                    EndDate,
                    RoleId,
                    DepartmentId,
                    ManagerId
                FROM Users
                WHERE IsDeleted = 0
                AND Id = @Id
                ORDER BY FirstName, LastName";

            using SqlCommand cmd = new SqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@Id", id);

            using SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                users.Add(new
                {
                    id = reader["Id"],
                    sicil = reader["Sicil"],
                    firstName = reader["FirstName"],
                    lastName = reader["LastName"],
                    fullName = $"{reader["FirstName"]} {reader["LastName"]}",
                    email = reader["Email"],
                    gsm = reader["GSM"],
                    username = reader["Username"],
                    startdate = reader["StartDate"],
                    enddate = reader["EndDate"],
                    roleID = reader["RoleId"],
                    departmentID = reader["DepartmentId"],
                    managerID = reader["ManagerId"],
                });
            }

            return Ok(users);
        }

        [HttpGet("usersbydepartmentid/{departmentId}")]
        public IActionResult GetUsersByDepartmentId(int departmentId)
        {
            string connStr = _configuration.GetConnectionString("DefaultConnection");

            List<object> users = new List<object>();

            using SqlConnection conn = new SqlConnection(connStr);
            conn.Open();

            string query = @"
                SELECT 
                    Id,
                    Sicil,
                    FirstName,
                    LastName,
                    Email,
                    GSM,
                    Username,
                    StartDate,
                    EndDate,
                    RoleId,
                    DepartmentId,
                    ManagerId
                FROM Users
                WHERE IsDeleted = 0
                AND DepartmentId = @DepartmentId
                ORDER BY FirstName, LastName";

            using SqlCommand cmd = new SqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@DepartmentId", departmentId);

            using SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                users.Add(new
                {
                    id = reader["Id"],
                    sicil = reader["Sicil"],
                    firstName = reader["FirstName"],
                    lastName = reader["LastName"],
                    fullName = $"{reader["FirstName"]} {reader["LastName"]}",
                    email = reader["Email"],
                    gsm = reader["GSM"],
                    username = reader["Username"],
                    startdate = reader["StartDate"],
                    enddate = reader["EndDate"],
                    roleID = reader["RoleId"],
                    departmentID = reader["DepartmentId"],
                    managerID = reader["ManagerId"],
                });
            }

            return Ok(users);
        }

        [HttpGet("usersbyroleid/{roleId}")]
        public IActionResult GetUsersByRoleId(int roleId)
        {
            string connStr = _configuration.GetConnectionString("DefaultConnection");

            List<object> users = new List<object>();

            using SqlConnection conn = new SqlConnection(connStr);
            conn.Open();

            string query = @"
        SELECT 
            Id,
            Sicil,
            FirstName,
            LastName,
            Email,
            GSM,
            Username,
            StartDate,
            EndDate,
            RoleId,
            DepartmentId,
            ManagerId
        FROM Users
        WHERE IsDeleted = 0
          AND RoleId = @RoleId
        ORDER BY FirstName, LastName";

            using SqlCommand cmd = new SqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@RoleId", roleId);

            using SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                users.Add(new
                {
                    id = reader["Id"],
                    sicil = reader["Sicil"],
                    firstName = reader["FirstName"],
                    lastName = reader["LastName"],
                    fullName = $"{reader["FirstName"]} {reader["LastName"]}",
                    email = reader["Email"],
                    gsm = reader["GSM"],
                    username = reader["Username"],
                    startDate = reader["StartDate"],
                    endDate = reader["EndDate"],
                    roleId = reader["RoleId"],
                    departmentId = reader["DepartmentId"],
                    managerId = reader["ManagerId"]
                });
            }

            return Ok(users);
        }

        [HttpPost("assign-roles")]
        public async Task<IActionResult> AssignRoles(UserRoleAssignDto dto)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var userExists = await _db.Users.AnyAsync(x => x.Id == dto.UserId);
                if (!userExists) return NotFound("Kullanıcı bulunamadı");

                // Eski roller silinir
                var oldRoles = await _db.UserRoles
                    .Where(x => x.UserId == dto.UserId)
                    .ToListAsync();

                _db.UserRoles.RemoveRange(oldRoles);

                // Yeni roller eklenir
                foreach (var roleId in dto.RoleIds.Distinct())
                {
                    _db.UserRoles.Add(new UserRole
                    {
                        UserId = dto.UserId,
                        RoleId = roleId,
                        CreatedDate = DateTime.Now
                    });
                }

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok(new
                {
                    message = "Kullanıcı rolleri güncellendi"
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }
    }
}
