using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace GranitWebApi.Controllers.Sabitler.UserDeptUnit
{
    [Route("api/[controller]")]
    [ApiController]
    public class PolimekSubUnitController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public PolimekSubUnitController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("list")]
        public IActionResult GetList()
        {
            string connStr = _configuration.GetConnectionString("DefaultConnection");

            List<object> list = new();

            using SqlConnection conn = new(connStr);
            conn.Open();

            string sql = @"
                SELECT
                    SubUnitId,
                    UnitId,
                    DepartmentId,
                    SubDepartmentId,
                    SubUnitName,
                    IsActive
                FROM PolimekSubUnits
                ORDER BY SubUnitName";

            using SqlCommand cmd = new(sql, conn);

            using SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new
                {
                    id = reader["SubUnitId"],
                    unitId = reader["UnitId"],
                    departmentId = reader["DepartmentId"],
                    subDepartmentId = reader["SubDepartmentId"],
                    subUnitName = reader["SubUnitName"],
                    isActive = reader["IsActive"]
                });
            }

            return Ok(list);
        }
    }
}