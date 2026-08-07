using GranitWebApi.Models.Sabitler.UserDeptUnit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace GranitWebApi.Controllers.Sabitler.UserDeptUnit
{
    [Route("api/[controller]")]
    [ApiController]
    public class PolimekSubDepartmentController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public PolimekSubDepartmentController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("list")]
        public IActionResult List()
        {
            var list = new List<SubDepartmentDto>();

            using SqlConnection conn =
                new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));

            conn.Open();

            string sql = @"
                SELECT
                    SubDepartmentId,
                    DepartmentId,
                    SubDepartmentName
                FROM PolimekSubDepartments
                ORDER BY SubDepartmentName";

            using SqlCommand cmd = new(sql, conn);

            using SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new SubDepartmentDto
                {
                    Id = Convert.ToInt32(reader["SubDepartmentId"]),
                    DepartmentId = Convert.ToInt32(reader["DepartmentId"]),
                    Name = reader["SubDepartmentName"].ToString()!
                });
            }

            return Ok(list);
        }
    }
}
