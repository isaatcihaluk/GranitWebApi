using GranitWebApi.Models.Sabitler.UserDeptUnit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace GranitWebApi.Controllers.Sabitler.UserDeptUnit
{
    [Route("api/[controller]")]
    [ApiController]
    public class PolimekUnitController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public PolimekUnitController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("list")]
        public IActionResult List()
        {
            var list = new List<UnitDto>();

            using SqlConnection conn =
                new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));

            conn.Open();

            string sql = @"
                SELECT
                    UnitId,
                    DepartmentId,
                    SubDepartmentId,
                    UnitName
                FROM PolimekUnits
                WHERE IsActive = 1
                ORDER BY UnitName";

            using SqlCommand cmd = new(sql, conn);

            using SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new UnitDto
                {
                    Id = Convert.ToInt32(reader["UnitId"]),
                    DepartmentId = Convert.ToInt32(reader["DepartmentId"]),
                    SubDepartmentId = Convert.ToInt32(reader["SubDepartmentId"]),
                    UnitName = reader["UnitName"].ToString()!
                });
            }

            return Ok(list);
        }
    }
}
