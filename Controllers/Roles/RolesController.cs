using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace GranitWebApi.Controllers.Roles
{
    [Route("api/[controller]")]
    [ApiController]
    public class RolesController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        public RolesController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        //[Authorize]
        [HttpGet("list")]
        public IActionResult GetRoles()
        {
            string connStr = _configuration.GetConnectionString("DefaultConnection");

            List<object> roles = new List<object>();

            using SqlConnection conn = new SqlConnection(connStr);
            conn.Open();

            string query = @"SELECT * FROM Roles";

            using SqlCommand cmd = new SqlCommand(query, conn);
            using SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                roles.Add(new
                {
                    id = reader["Id"],
                    name = reader["Name"],
                });
            }

            return Ok(roles);
        }
    }
}
