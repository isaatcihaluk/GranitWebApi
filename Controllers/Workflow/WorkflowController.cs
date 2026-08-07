using GranitWebApi.Helpers;
using GranitWebApi.Models.Workflow;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace GranitWebApi.Controllers.Workflow
{
    [Route("api/[controller]")]
    [ApiController]
    public class WorkflowController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly UserContext _userContext;

        private const int HR_ROLE_ID = 3;

        public WorkflowController(IConfiguration configuration, UserContext userContext)
        {
            _configuration = configuration;
            _userContext = userContext;
        }

        // =========================
        // CREATE
        // =========================
        [HttpPost("create")]
        public IActionResult Create(CreateRequestDto model)
        {
            using SqlConnection conn = new(_configuration.GetConnectionString("DefaultConnection"));
            conn.Open();

            using var tran = conn.BeginTransaction();

            try
            {
                // 1. REQUEST
                var cmd = new SqlCommand(@"
                    INSERT INTO ProcessRequest 
                    (ProcessTypeId, CreatedBy, Title, Status)
                    OUTPUT INSERTED.Id
                    VALUES (@TypeId, @UserId, @Title, 'Taslak')", conn, tran);

                cmd.Parameters.AddWithValue("@TypeId", model.ProcessTypeId);
                cmd.Parameters.AddWithValue("@UserId", model.CreatedBy);
                cmd.Parameters.AddWithValue("@Title", model.Title);

                int requestId = (int)cmd.ExecuteScalar();

                // 2. CHAIN
                var approvers = GetApprovalChain(model.CreatedBy, conn, tran);

                int step = 1;

                foreach (var approver in approvers)
                {
                    var c = new SqlCommand(@"
                        INSERT INTO ProcessApproval
                        (RequestId, ApproverId, StepOrder, Status, IsActive)
                        VALUES (@R, @A, @S, 'Onay Bekliyor', @Active)", conn, tran);

                    c.Parameters.AddWithValue("@R", requestId);
                    c.Parameters.AddWithValue("@A", approver);
                    c.Parameters.AddWithValue("@S", step);
                    c.Parameters.AddWithValue("@Active", step == 1 ? 1 : 0);

                    c.ExecuteNonQuery();
                    step++;
                }

                tran.Commit();

                return Ok(new { requestId });
            }
            catch (Exception ex)
            {
                tran.Rollback();
                return BadRequest(ex.Message);
            }
        }

        // =========================
        // INBOX
        // =========================
        [HttpGet("inbox")]
        public IActionResult Inbox()
        {
            int userId = _userContext.UserId;

            using SqlConnection conn = new(_configuration.GetConnectionString("DefaultConnection"));
            conn.Open();

            var list = new List<object>();

            var cmd = new SqlCommand(@"
                    SELECT DISTINCT
                        pr.Id,
                        pr.Title,
                        pr.Status,
                        pt.Name AS ProcessType,

                        CASE
                            WHEN EXISTS (
                                SELECT 1
                                FROM ProcessApproval pa
                                WHERE pa.RequestId = pr.Id
                                    AND pa.ApproverId = @UserId
                                    AND pa.IsActive = 1
                            )
                            THEN 'Onay Bekliyor'

                            WHEN pr.CreatedBy = @UserId
                                 AND pr.Status IN ('Taslak', 'Revize')
                            THEN 'Taslak / Revize'

                            WHEN pr.CreatedBy = @UserId
                                 AND pr.Status = 'Onay Bekliyor'
                            THEN 'Gönderildi'

                            WHEN @IsHR = 1
                                 AND pr.Status = 'Onaylandı'
                                 AND pr.IsVisibleToHR = 1
                            THEN 'HR Görüntü'

                            ELSE NULL
                        END AS ViewType

                    FROM ProcessRequest pr
                    INNER JOIN ProcessType pt ON pt.Id = pr.ProcessTypeId

                    WHERE
                    (
                        -- 🔥 SADECE AKTİF ONAYCI GÖRÜR
                        EXISTS (
                            SELECT 1
                            FROM ProcessApproval pa
                            WHERE pa.RequestId = pr.Id
                                AND pa.ApproverId = @UserId
                                AND pa.IsActive = 1
                        )

                        OR

                        -- 👤 SADECE SAHİBİ TASLAK/REVİZE GÖRÜR
                        (
                            pr.CreatedBy = @UserId
                            AND pr.Status IN ('Taslak', 'Revize')
                        )

                        OR

                        -- 📦 HR SADECE FINAL GÖRÜR
                        (
                            @IsHR = 1
                            AND pr.Status = 'Onaylandı'
                            AND pr.IsVisibleToHR = 1
                        )
                    )
                ", conn);

            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@IsHR", IsHR(userId, conn));

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new
                {
                    id = reader["Id"],
                    title = reader["Title"],
                    status = reader["Status"],
                    type = reader["ProcessType"]
                });
            }

            return Ok(list);
        }

        // =========================
        // DETAIL (FIXED)
        // =========================
        [HttpGet("detail/{requestId}")]
        public IActionResult Detail(int requestId)
        {
            using SqlConnection conn = new(_configuration.GetConnectionString("DefaultConnection"));
            conn.Open();

            var cmd = new SqlCommand(@"
                                SELECT 
                                    pr.Id,
                                    pr.Title,
                                    pr.Status,
                                    pr.CreatedBy,
                                    pr.ProcessTypeId,
                                    pt.Name AS ProcessType,
                                    u.FirstName + ' ' + u.LastName AS PersonelAdi,
                                    id.Baslangic,
                                    id.Bitis,
                                    id.Aciklama,
                                    id.IzinTuruId
                                FROM ProcessRequest pr
                                INNER JOIN Users u ON u.Id = pr.CreatedBy
                                LEFT JOIN IzinDetail id ON id.RequestId = pr.Id
                                LEFT JOIN ProcessType pt ON pt.Id = pr.ProcessTypeId
                                WHERE pr.Id = @Id", conn);

            cmd.Parameters.AddWithValue("@Id", requestId);

            using var r = cmd.ExecuteReader();

            if (!r.Read())
                return NotFound();

            return Ok(new
            {
                id = r["Id"],
                title = r["Title"],
                status = r["Status"],
                createdBy = r["CreatedBy"],
                processTypeId = r["ProcessTypeId"],
                processType = r["ProcessType"],

                personelAdi = r["PersonelAdi"],

                izinTuruId = r["IzinTuruId"] == DBNull.Value ? null : r["IzinTuruId"],
                baslangic = r["Baslangic"] == DBNull.Value ? null : r["Baslangic"],
                bitis = r["Bitis"] == DBNull.Value ? null : r["Bitis"],
                aciklama = r["Aciklama"] == DBNull.Value ? "" : r["Aciklama"]
            });
        }

        // =========================
        // APPROVAL CHAIN
        // =========================
        private List<int> GetApprovalChain(int userId, SqlConnection conn, SqlTransaction tran)
        {
            List<int> chain = new();
            HashSet<int> visited = new();

            int? managerId = GetManager(userId, conn, tran);

            while (managerId != null)
            {
                if (visited.Contains(managerId.Value))
                    break;

                visited.Add(managerId.Value);
                chain.Add(managerId.Value);

                managerId = GetManager(managerId.Value, conn, tran);
            }

            if (!chain.Contains(HR_ROLE_ID))
                chain.Add(HR_ROLE_ID);

            return chain;
        }

        private int? GetManager(int userId, SqlConnection conn, SqlTransaction tran)
        {
            var cmd = new SqlCommand(
                "SELECT ManagerId FROM Users WHERE Id=@Id",
                conn, tran);

            cmd.Parameters.AddWithValue("@Id", userId);

            var result = cmd.ExecuteScalar();

            return result == null || result == DBNull.Value
                ? null
                : Convert.ToInt32(result);
        }

        private bool IsHR(int userId, SqlConnection conn)
        {
            var cmd = new SqlCommand(
                "SELECT CASE WHEN RoleId = @HR THEN 1 ELSE 0 END FROM Users WHERE Id=@Id",
                conn);

            cmd.Parameters.AddWithValue("@HR", HR_ROLE_ID);
            cmd.Parameters.AddWithValue("@Id", userId);

            return (int)cmd.ExecuteScalar() == 1;
        }
    }
}