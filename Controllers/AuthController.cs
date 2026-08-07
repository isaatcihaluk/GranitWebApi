using GranitWebApi.Models;
using GranitWebApi.Models.YönetimKayıtIslemleri;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace GranitWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        private string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        [HttpPost("login")]
        public IActionResult Login(LoginRequest request)
        {
            string connStr = _configuration.GetConnectionString("DefaultConnection");

            using SqlConnection conn = new SqlConnection(connStr);
            conn.Open();

            string query = @"
                SELECT 
                    u.Id,
                    u.Username,
                    u.PasswordHash,
                    u.MustChangePassword,
                    d.Name AS DepartmentName
                FROM Users u
                LEFT JOIN Departments d ON u.DepartmentId = d.Id
                WHERE u.Username = @username AND u.IsDeleted = 0";

            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.Add("@username", System.Data.SqlDbType.NVarChar).Value = request.Username;

            using SqlDataReader reader = cmd.ExecuteReader();

            if (!reader.Read()) return Unauthorized("Kullanıcı bulunamadı");
            string storedHash = reader["PasswordHash"].ToString()!;
            //string role = reader["RoleName"].ToString()!;
            string department = reader["DepartmentName"]?.ToString() ?? "";
            string username = reader["Username"].ToString()!;
            int userId = Convert.ToInt32(reader["Id"]);
            bool mustChangePassword = Convert.ToBoolean(reader["MustChangePassword"]);

            var hasher = new PasswordHasher<string>();
            var result = hasher.VerifyHashedPassword(null,storedHash,request.Password);

            if (result == PasswordVerificationResult.Failed)
                return Unauthorized("Şifre yanlış");

            reader.Close();
            var roles = new List<string>();

            using (SqlCommand roleCmd = new SqlCommand(@"
                SELECT r.Name
                FROM UserRoles ur
                INNER JOIN Roles r
                    ON ur.RoleId = r.Id
                WHERE ur.UserId = @userId", conn))
            {
                roleCmd.Parameters.AddWithValue("@userId", userId);
                using var roleReader = roleCmd.ExecuteReader();

                while (roleReader.Read())
                {
                    roles.Add(roleReader.GetString(0));
                }
            }
            // 🔐 JWT
            var claims = new List<Claim>{
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, username),
                new Claim("Department", department)
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"]!)
            );

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddMinutes(
                    Convert.ToDouble(_configuration["JwtSettings:ExpiryMinutes"])
                ),
                signingCredentials: creds
            );

            string refreshToken = GenerateRefreshToken();

            string update = @"
                UPDATE Users
                SET RefreshToken = @rt,
                    RefreshTokenExpiry = @exp
                WHERE Username = @username";

            using SqlCommand updateCmd = new SqlCommand(update, conn);
            updateCmd.Parameters.Add("@rt", System.Data.SqlDbType.NVarChar).Value = refreshToken;
            updateCmd.Parameters.Add("@exp", System.Data.SqlDbType.DateTime).Value = DateTime.Now.AddDays(7);
            updateCmd.Parameters.Add("@username", System.Data.SqlDbType.NVarChar).Value = username;

            updateCmd.ExecuteNonQuery();

            return Ok(new
            {
                accessToken = new JwtSecurityTokenHandler().WriteToken(token),
                refreshToken,
                userId,
                roles,
                department,
                mustChangePassword
            });
        }

        [HttpPost("refresh")]
        public IActionResult Refresh(TokenRequest request)
        {
            string connStr = _configuration.GetConnectionString("DefaultConnection");

            using SqlConnection conn = new SqlConnection(connStr);
            conn.Open();

            string query = @"
                        SELECT
                            u.Id,
                            u.Username,
                            u.RefreshTokenExpiry,
                            d.Name AS DepartmentName
                        FROM Users u
                        LEFT JOIN Departments d
                            ON u.DepartmentId = d.Id
                        WHERE u.RefreshToken = @rt;";

            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.Add("@rt", System.Data.SqlDbType.NVarChar).Value = request.RefreshToken;

            using SqlDataReader reader = cmd.ExecuteReader();

            if (!reader.Read())
                return Unauthorized("Geçersiz refresh token");

            if (reader["RefreshTokenExpiry"] == DBNull.Value) return Unauthorized("Refresh token bulunamadı.");

            DateTime expiry = (DateTime)reader["RefreshTokenExpiry"];

            if (expiry < DateTime.Now) return Unauthorized("Token süresi dolmuş");

            int userId = Convert.ToInt32(reader["Id"]);
            string username = reader["Username"].ToString()!;
            string department = reader["DepartmentName"]?.ToString() ?? "";

            reader.Close();

            var roles = new List<string>();
            using (SqlCommand roleCmd = new SqlCommand(@"
                SELECT r.Name
                FROM UserRoles ur
                INNER JOIN Roles r
                    ON ur.RoleId = r.Id
                WHERE ur.UserId = @userId", conn))
            {
                roleCmd.Parameters.Add("@userId",System.Data.SqlDbType.Int).Value = userId;
                using var roleReader = roleCmd.ExecuteReader();
                while (roleReader.Read())
                {
                    roles.Add(roleReader.GetString(0));
                }
            }

            if (roles.Count == 0) {return Unauthorized("Kullanıcıya rol atanmamış.");}

            var claims = new List<Claim>{
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, username),
                new Claim("Department", department)
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddMinutes(
                    Convert.ToDouble(_configuration["JwtSettings:ExpiryMinutes"])
                ),
                signingCredentials: creds
            );

            return Ok(new
            {
                accessToken = new JwtSecurityTokenHandler().WriteToken(token)
            });
        }

        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            string username = User.Identity!.Name!;

            string connectionString =
                _configuration.GetConnectionString("DefaultConnection");

            using SqlConnection conn = new SqlConnection(connectionString);
            conn.Open();

            string query = @"UPDATE Users
                     SET RefreshToken = NULL,
                         RefreshTokenExpiry = NULL
                     WHERE Username = @u";

            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@u", username);

            cmd.ExecuteNonQuery();

            return Ok("Başarıyla çıkış yapıldı");
        }

        [HttpPost("generate-hash")]
        public IActionResult GenerateHash([FromBody] string password)
        {
            var hasher = new PasswordHasher<string>();
            string hashed = hasher.HashPassword(null, password);
            return Ok(hashed);
        }

        [HttpGet("my-pages")]
        [Authorize]
        public IActionResult MyPages()
        {
            var roles = User.FindAll(ClaimTypes.Role).Select(x => x.Value)   .ToHashSet(StringComparer.OrdinalIgnoreCase);
            string department = User.FindFirst("Department")?.Value ?? "";

            string connStr = _configuration.GetConnectionString("DefaultConnection");
            using SqlConnection conn = new(connStr);
            conn.Open();

            var pages = new List<(string PageKey, string MenuGroup)>();
            var cmd = new SqlCommand(@"
                        SELECT DISTINCT
                        PageKey,
                        AccessType,
                        RoleName,
                        DepartmentName,
                        MenuGroup
                    FROM PagePermissions
                    ", conn);

            using var r = cmd.ExecuteReader();

            while (r.Read())
            {
                string pageKey = r["PageKey"].ToString()!;
                string accessType = r["AccessType"]?.ToString() ?? "";
                string roleName = r["RoleName"]?.ToString() ?? "";
                string deptName = r["DepartmentName"]?.ToString() ?? "";
                string menuGroup = r["MenuGroup"]?.ToString() ?? "Genel";

                bool allowed =
                        accessType.Equals("Public", StringComparison.OrdinalIgnoreCase)
                        || (!string.IsNullOrWhiteSpace(roleName) && roles.Contains(roleName))
                        || (!string.IsNullOrWhiteSpace(deptName)
                            && deptName.Equals(department, StringComparison.OrdinalIgnoreCase));

                if (allowed)
                {
                    pages.Add((pageKey, menuGroup));
                }
            }

            // duplicate temizleme + grouping
            var result = pages
                .GroupBy(x => x.PageKey)
                .Select(g => new
                {
                    code = g.Key,
                    name = GetDisplayName(g.Key),
                    url = "/" + g.Key,
                    menuGroup = g.First().MenuGroup
                })
                .ToList();

            return Ok(result);
        }

        [Authorize]
        [HttpPost("change-password")]
        public IActionResult ChangePassword(ChangePasswordRequest request)
        {
            if (request.NewPassword.Length < 6) return BadRequest("Şifre en az 6 karakter olmalıdır.");

            if (request.NewPassword != request.ConfirmPassword) return BadRequest("Şifreler uyuşmuyor.");

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            string connStr = _configuration.GetConnectionString("DefaultConnection");

            using SqlConnection conn = new(connStr);
            conn.Open();

            var hasher = new PasswordHasher<string>();
            string newHash = hasher.HashPassword(null, request.NewPassword);

            string sql = @"
                UPDATE Users
                SET PasswordHash=@PasswordHash,
                    MustChangePassword=0
                WHERE Id=@Id";

            using SqlCommand cmd = new(sql, conn);

            cmd.Parameters.AddWithValue("@PasswordHash", newHash);
            cmd.Parameters.AddWithValue("@Id", userId);

            cmd.ExecuteNonQuery();

            return Ok(new
            {
                message = "Şifre başarıyla değiştirildi."
            });
        }
        private string GetDisplayName(string key)
        {
            return key switch
            {
                "bakim-form" => "Bakım Onarım Formu",
                "izin-talep-form" => "İzin Talep Formu",
                "personel-islemleri" => "Personel İşlemleri",
                _ => key
            };
        }


    }
}
