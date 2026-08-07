using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace GranitWebApi.Controllers.Departments
{
    [Route("api/[controller]")]
    [ApiController]
    public class DepartmentController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        public DepartmentController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        //[Authorize]
        [HttpGet("list")]
        public IActionResult GetDepartments()
        {
            string connStr = _configuration.GetConnectionString("DefaultConnection");

            List<object> departments = new List<object>();

            using SqlConnection conn = new SqlConnection(connStr);
            conn.Open();

            string query = @"SELECT * FROM Departments";

            using SqlCommand cmd = new SqlCommand(query, conn);
            using SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                departments.Add(new
                {
                    id = reader["Id"],
                    name = reader["Name"],
                });
            }

            return Ok(departments);
        }
    }
}
