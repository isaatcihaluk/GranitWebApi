using GranitWebApi.Models.Sabitler.UserDeptUnit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace GranitWebApi.Controllers.Sabitler.UserDeptUnit
{
    [Route("api/[controller]")]
    [ApiController]
    public class PolimekDepartmentController : ControllerBase
    {
        private readonly IConfiguration _configuration; 
        
        public PolimekDepartmentController(IConfiguration configuration) { _configuration = configuration; }

        [HttpGet("list")]
        public IActionResult List()
        {
            var list = new List<DepartmentDto>();

            using SqlConnection conn =
                new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));

            conn.Open();

            string sql = @" SELECT DepartmentId, DepartmentName FROM PolimekDepartments ORDER BY DepartmentName";

            using SqlCommand cmd = new(sql, conn);

            using SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new DepartmentDto
                {
                    Id = Convert.ToInt32(reader["DepartmentId"]),
                    Name = reader["DepartmentName"].ToString()!
                });
            }

            return Ok(list);
        }
    }
}