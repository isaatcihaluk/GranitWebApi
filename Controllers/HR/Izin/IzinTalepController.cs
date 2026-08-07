using GranitWebApi.Helpers;
using GranitWebApi.Models.IK.Izin;
using GranitWebApi.Models.Notification;
using GranitWebApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace GranitWebApi.Controllers.HR.Izin
{
    [Route("api/[controller]")]
    [ApiController]
    public class IzinTalepController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly IConfiguration _configuration;
        private readonly UserContext _userContext;
        private readonly PdfService _pdfService;

        public IzinTalepController(
            IConfiguration configuration,
            UserContext userContext,
            INotificationService notificationService,
            PdfService pdfService)
        {
            _configuration = configuration;
            _userContext = userContext;
            _notificationService = notificationService;
            _pdfService = pdfService;
        }

        private SqlCommand CreateCommand(string query, SqlConnection conn, SqlTransaction tran)
        {
            var cmd = new SqlCommand(query, conn);
            if (tran != null)
                cmd.Transaction = tran;
            return cmd;
        }

        // =========================
        // SAVE
        // =========================
        [HttpPost("save")]
        public IActionResult Save(IzinCreateFullDto model)
        {
            string connStr = _configuration.GetConnectionString("DefaultConnection");

            using SqlConnection conn = new(connStr);
            conn.Open();
            using var tran = conn.BeginTransaction();

            var checkCmd1 = CreateCommand(@"
                    SELECT COUNT(1)
                    FROM IzinDetail id
                    INNER JOIN ProcessRequest pr ON pr.Id = id.RequestId
                    WHERE pr.CreatedBy = @UserId
                    AND pr.Status IN ('Taslak','Onay Bekliyor','Onaylandı')
                    AND id.RequestId <> @RequestId

                    -- 🔥 GERÇEK OVERLAP (SAAT DAHİL)
                    AND NOT (
                        @End <= id.Baslangic
                        OR @Start >= id.Bitis
                    )
                ", conn, tran);

            checkCmd1.Parameters.AddWithValue("@UserId", _userContext.UserId);
            checkCmd1.Parameters.AddWithValue("@Start", model.Baslangic);
            checkCmd1.Parameters.AddWithValue("@End", model.Bitis);
            checkCmd1.Parameters.AddWithValue("@RequestId", model.RequestId ?? 0);

            int count = (int)checkCmd1.ExecuteScalar();

            if (count > 0)
            {
                return BadRequest("Bu tarih/saat aralığında çakışan izin bulunmaktadır.");
            }

            try
            {
                int requestId = model.RequestId ?? 0;

                if (requestId == 0)
                {
                    var cmd = CreateCommand(@"
                        INSERT INTO ProcessRequest
                        (ProcessTypeId, CreatedBy, Title, Status)
                        OUTPUT INSERTED.Id
                        VALUES (1, @UserId, 'İzin Talebi', 'Taslak')",
                        conn, tran);

                    cmd.Parameters.AddWithValue("@UserId", _userContext.UserId);
                    requestId = (int)cmd.ExecuteScalar();
                }

                var checkCmd = CreateCommand(
                    "SELECT COUNT(*) FROM IzinDetail WHERE RequestId=@R",
                    conn, tran);

                checkCmd.Parameters.AddWithValue("@R", requestId);

                bool exists = (int)checkCmd.ExecuteScalar() > 0;

                if (exists)
                {
                    var cmd = CreateCommand(@"
                        UPDATE IzinDetail
                        SET Baslangic=@B, Bitis=@E, Aciklama=@A, IzinTuruId=@T
                        WHERE RequestId=@R",
                        conn, tran);

                    cmd.Parameters.AddWithValue("@B", model.Baslangic);
                    cmd.Parameters.AddWithValue("@E", model.Bitis);
                    cmd.Parameters.AddWithValue("@A", model.Aciklama ?? "");
                    cmd.Parameters.AddWithValue("@T", model.IzinTuruId);
                    cmd.Parameters.AddWithValue("@R", requestId);

                    cmd.ExecuteNonQuery();
                }
                else
                {
                    var cmd = CreateCommand(@"
                        INSERT INTO IzinDetail
                        (RequestId, Baslangic, Bitis, Aciklama, IzinTuruId)
                        VALUES (@R,@B,@E,@A,@T)",
                        conn, tran);

                    cmd.Parameters.AddWithValue("@R", requestId);
                    cmd.Parameters.AddWithValue("@B", model.Baslangic);
                    cmd.Parameters.AddWithValue("@E", model.Bitis);
                    cmd.Parameters.AddWithValue("@A", model.Aciklama ?? "");
                    cmd.Parameters.AddWithValue("@T", model.IzinTuruId);

                    cmd.ExecuteNonQuery();
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
        // SUBMIT
        // =========================
        [HttpPost("submit")]
        public IActionResult Submit(int requestId)
        {
            string connStr = _configuration.GetConnectionString("DefaultConnection");

            using SqlConnection conn = new(connStr);
            conn.Open();
            using var tran = conn.BeginTransaction();

            try
            {
                var reqCmd = CreateCommand(@"
                    UPDATE ProcessRequest
                    SET Status='Onay Bekliyor',
                        CurrentStep=1,
                        IsCompleted=0
                    WHERE Id=@R", conn, tran);

                reqCmd.Parameters.AddWithValue("@R", requestId);
                reqCmd.ExecuteNonQuery();

                var delCmd = CreateCommand(@"
                    DELETE FROM ProcessApproval
                    WHERE RequestId=@R", conn, tran);

                delCmd.Parameters.AddWithValue("@R", requestId);
                delCmd.ExecuteNonQuery();

                var chain = GetApprovalChain(_userContext.UserId, conn, tran);

                int step = 1;

                foreach (var approver in chain)
                {
                    var cmd = CreateCommand(@"
                        INSERT INTO ProcessApproval
                        (RequestId, ApproverId, StepOrder, Status, IsActive)
                        VALUES (@R,@A,@S,'Onay Bekliyor',@Active)",
                        conn, tran);

                    cmd.Parameters.AddWithValue("@R", requestId);
                    cmd.Parameters.AddWithValue("@A", approver);
                    cmd.Parameters.AddWithValue("@S", step);
                    cmd.Parameters.AddWithValue("@Active", step == 1);

                    cmd.ExecuteNonQuery();

                    step++;
                }

                tran.Commit();
                return Ok(new { message = "Gönderildi" });
            }
            catch (Exception ex)
            {
                tran.Rollback();
                return BadRequest(ex.Message);
            }
        }

        // =========================
        // APPROVE
        // =========================
        [HttpPost("approve")]
        public async Task<IActionResult> Approve(int requestId)
        {
            string connStr = _configuration.GetConnectionString("DefaultConnection");

            using SqlConnection conn = new(connStr);
            conn.Open();
            using var tran = conn.BeginTransaction();

            try
            {
                var stepCmd = CreateCommand(@"
                    SELECT StepOrder
                    FROM ProcessApproval
                    WHERE RequestId=@R
                      AND ApproverId=@U
                      AND IsActive=1",
                    conn, tran);

                stepCmd.Parameters.AddWithValue("@R", requestId);
                stepCmd.Parameters.AddWithValue("@U", _userContext.UserId);

                var stepObj = stepCmd.ExecuteScalar();

                if (stepObj == null)
                    throw new Exception("Aktif onaycı değil");

                int currentStep = (int)stepObj;

                var closeCmd = CreateCommand(@"
                    UPDATE ProcessApproval
                    SET Status='Onaylandı',
                        IsActive=0,
                        ActionDate=GETDATE()
                    WHERE RequestId=@R AND ApproverId=@U",
                    conn, tran);

                closeCmd.Parameters.AddWithValue("@R", requestId);
                closeCmd.Parameters.AddWithValue("@U", _userContext.UserId);
                closeCmd.ExecuteNonQuery();

                var nextCmd = CreateCommand(@"
                    UPDATE ProcessApproval
                    SET IsActive=1
                    WHERE RequestId=@R AND StepOrder=@S",
                    conn, tran);

                nextCmd.Parameters.AddWithValue("@R", requestId);
                nextCmd.Parameters.AddWithValue("@S", currentStep + 1);
                nextCmd.ExecuteNonQuery();

                var pendingCmd = CreateCommand(@"
                    SELECT COUNT(*)
                    FROM ProcessApproval
                    WHERE RequestId=@R AND Status='Onay Bekliyor'",
                    conn, tran);

                pendingCmd.Parameters.AddWithValue("@R", requestId);

                int pending = (int)pendingCmd.ExecuteScalar();

                if (pending > 0)
                {
                    tran.Commit();
                    return Ok(new { isFinal = false });
                }

                var reqCmd = CreateCommand(@"
                    UPDATE ProcessRequest
                    SET Status='Onaylandı',
                        IsCompleted=1,
                        IsVisibleToHR=1
                    WHERE Id=@R",
                    conn, tran);

                reqCmd.Parameters.AddWithValue("@R", requestId);
                reqCmd.ExecuteNonQuery();

                var mailData = GetRequestMailData(requestId, conn, tran);

                var pdfPath = _pdfService.CreateIzinPdf(
                    requestId,
                    mailData.Sicil,
                    mailData.PersonelAdi,
                    mailData.IzinTuru,
                    mailData.Baslangic,
                    mailData.Bitis
                );

                await _notificationService.SendAsync(
                    mailData.Email,
                    NotificationType.Approved,
                    mailData.PersonelAdi,
                    mailData.Baslangic,
                    mailData.Bitis,
                    "Talep onaylandı",
                    pdfPath
                );

                tran.Commit();
                return Ok(new { isFinal = true });
            }
            catch (Exception ex)
            {
                tran.Rollback();
                return BadRequest(ex.Message);
            }
        }

        // =========================
        // SENDBACK
        // =========================
        [HttpPost("sendback")]
        public IActionResult SendBack(int requestId)
        {
            string connStr = _configuration.GetConnectionString("DefaultConnection");

            using SqlConnection conn = new(connStr);
            conn.Open();
            using var tran = conn.BeginTransaction();

            try
            {
                var reqCmd = CreateCommand(@"
                    UPDATE ProcessRequest
                    SET Status='Revize',
                        CurrentStep=1,
                        IsCompleted=0
                    WHERE Id=@R", conn, tran);

                reqCmd.Parameters.AddWithValue("@R", requestId);
                reqCmd.ExecuteNonQuery();

                var appCmd = CreateCommand(@"
                    UPDATE ProcessApproval
                    SET Status='Onay Bekliyor',
                        IsActive=0,
                        ActionDate=NULL,
                        Comment=NULL
                    WHERE RequestId=@R", conn, tran);

                appCmd.Parameters.AddWithValue("@R", requestId);
                appCmd.ExecuteNonQuery();

                var firstCmd = CreateCommand(@"
                    UPDATE ProcessApproval
                    SET IsActive=1
                    WHERE RequestId=@R AND StepOrder=1", conn, tran);

                firstCmd.Parameters.AddWithValue("@R", requestId);
                firstCmd.ExecuteNonQuery();

                tran.Commit();
                return Ok();
            }
            catch (Exception ex)
            {
                tran.Rollback();
                return BadRequest(ex.Message);
            }
        }

        // =========================
        // REJECT
        // =========================
        [HttpPost("reject")]
        public IActionResult Reject(int requestId)
        {
            string connStr = _configuration.GetConnectionString("DefaultConnection");

            using SqlConnection conn = new(connStr);
            conn.Open();
            using var tran = conn.BeginTransaction();

            try
            {
                var cmd1 = CreateCommand(@"
                    UPDATE ProcessApproval
                    SET Status='Reddedildi',
                        IsActive=0,
                        ActionDate=GETDATE()
                    WHERE RequestId=@R", conn, tran);

                cmd1.Parameters.AddWithValue("@R", requestId);
                cmd1.ExecuteNonQuery();

                var cmd2 = CreateCommand(@"
                    UPDATE ProcessRequest
                    SET Status='Reddedildi',
                        IsCompleted=1,
                        IsVisibleToHR=0
                    WHERE Id=@R", conn, tran);

                cmd2.Parameters.AddWithValue("@R", requestId);
                cmd2.ExecuteNonQuery();

                tran.Commit();
                return Ok();
            }
            catch (Exception ex)
            {
                tran.Rollback();
                return BadRequest(ex.Message);
            }
        }

        // =========================
        // HELPERS (UNCHANGED)
        // =========================
        private List<int> GetApprovalChain(int userId, SqlConnection conn, SqlTransaction? tran = null)
        {
            List<int> chain = new();
            HashSet<int> visited = new();

            int? managerId = GetManager(userId, conn, tran);

            // 1. Manager chain
            while (managerId != null)
            {
                if (visited.Contains(managerId.Value))
                    break;

                visited.Add(managerId.Value);

                if (!chain.Contains(managerId.Value))
                    chain.Add(managerId.Value);

                managerId = GetManager(managerId.Value, conn, tran);
            }

            // 2. 🔥 SADECE MANAGER YOKSA HR EKLE
            if (chain.Count == 0)
            {
                int hr = GetHR(conn, tran);
                chain.Add(hr);
            }
            else
            {
                // optional: son node HR değilse ekle
                int hr = GetHR(conn, tran);

                if (!chain.Contains(hr))
                    chain.Add(hr);
            }

            return chain;
        }

        private int? GetManager(int userId, SqlConnection conn, SqlTransaction tran)
        {
            var cmd = CreateCommand("SELECT ManagerId FROM Users WHERE Id=@Id", conn, tran);
            cmd.Parameters.AddWithValue("@Id", userId);

            var result = cmd.ExecuteScalar();

            return result == null || result == DBNull.Value ? null : Convert.ToInt32(result);
        }

        private int GetHR(SqlConnection conn, SqlTransaction? tran = null)
        {
            string query = "SELECT TOP 1 Id FROM Users WHERE RoleId = 3";

            using SqlCommand cmd = new(query, conn, tran);

            var result = cmd.ExecuteScalar();

            if (result == null)
                throw new Exception("IK (HR) kullanıcı bulunamadı");

            return Convert.ToInt32(result);
        }

        private (string Email, string PersonelAdi, string Sicil, string IzinTuru, DateTime Baslangic, DateTime Bitis)
        GetRequestMailData(int requestId, SqlConnection conn, SqlTransaction tran)
        {
            var cmd = CreateCommand(@"
                SELECT u.Email,
                       (u.FirstName + ' ' + u.LastName),
                       u.Sicil,
                       it.Ad,
                       id.Baslangic,
                       id.Bitis
                FROM ProcessRequest pr
                INNER JOIN Users u ON pr.CreatedBy=u.Id
                INNER JOIN IzinDetail id ON id.RequestId=pr.Id
                INNER JOIN IzinTuru it ON it.Id=id.IzinTuruId
                WHERE pr.Id=@Id", conn, tran);

            cmd.Parameters.AddWithValue("@Id", requestId);

            using var r = cmd.ExecuteReader();

            if (r.Read())
            {
                return (
                    r.GetString(0),
                    r.GetString(1),
                    r.GetString(2),
                    r.GetString(3),
                    r.GetDateTime(4),
                    r.GetDateTime(5)
                );
            }

            throw new Exception("User not found");
        }
    }
}