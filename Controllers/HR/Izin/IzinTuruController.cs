using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace GranitWebApi.Controllers.HR.Izin
{
    [Route("api/[controller]")]
    [ApiController]
    public class IzinTuruController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public IzinTuruController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // GET: api/IzinTurleri/list
        [HttpGet("list")]
        public IActionResult GetIzinTurleri()
        {
            string connStr = _configuration.GetConnectionString("DefaultConnection");

            List<object> izinTurleri = new List<object>();

            using SqlConnection conn = new SqlConnection(connStr);
            conn.Open();

            string query = @"
                SELECT 
                    Id,
                    Ad,
                    Aciklama,
                    Aktif
                FROM IzinTuru
                WHERE Aktif = 1";

            using SqlCommand cmd = new SqlCommand(query, conn);
            using SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                izinTurleri.Add(new
                {
                    id = reader["Id"],
                    ad = reader["Ad"],
                    aciklama = reader["Aciklama"],
                    aktif = reader["Aktif"]
                });
            }

            return Ok(izinTurleri);
        }

        // GET: api/IzinTurleri/detail/5
        [HttpGet("detail/{id}")]
        public IActionResult GetIzinTuruById(int id)
        {
            string connStr = _configuration.GetConnectionString("DefaultConnection");

            using SqlConnection conn = new SqlConnection(connStr);
            conn.Open();

            string query = @"
                SELECT 
                    Id,
                    Ad,
                    Aciklama,
                    Aktif
                FROM IzinTuru
                WHERE Id = @Id";

            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Id", id);

            using SqlDataReader reader = cmd.ExecuteReader();

            if (!reader.Read())
                return NotFound();

            var izinTuru = new
            {
                id = reader["Id"],
                ad = reader["Ad"],
                aciklama = reader["Aciklama"],
                aktif = reader["Aktif"]
            };

            return Ok(izinTuru);
        }
    }
}
