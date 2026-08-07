using GranitWebApi.Services.Email;
using GranitWebApi.Services.Notifications;
using Microsoft.AspNetCore.Mvc;

namespace GranitWebApi.Controllers.TESTLER
{
    [ApiController]
    [Route("api/[controller]")]
    public class MailController : ControllerBase
    {
        private readonly IEmailTemplateService _templateService;
        private readonly IEmailService _emailService;
        private readonly WorkflowNotificationService _workflowNotificationService;

        public MailController(
            IEmailTemplateService templateService,
            IEmailService emailService,
            WorkflowNotificationService workflowNotificationService)
        {
            _templateService = templateService;
            _emailService = emailService;
            _workflowNotificationService = workflowNotificationService;
        }

        [HttpPost("test")]
        public async Task<IActionResult> Test()
        {
            try
            {
                await _emailService.SendAsync(
                    "mailadresin@gmail.com",
                    "Test Mail",
                    "<h2>Mail servisi çalışıyor.</h2>");

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    ex.Message,
                    Inner = ex.InnerException?.Message,
                    Stack = ex.StackTrace
                });
            }
        }

        [HttpPost("template-test")]
        public async Task<IActionResult> TemplateTest()
        {
            try
            {
                var html = await _templateService.RenderAsync(
                "WorkflowSubmitted",
                new Dictionary<string, string>
                {
                    ["ApproverName"] = "Ahmet Yılmaz",
                    ["PersonelAdi"] = "Haluk Saatçi",
                    ["ProcessName"] = "Yıllık İzin",
                    ["RequestNo"] = "25",
                    ["StartDate"] = "01.08.2026",
                    ["EndDate"] = "05.08.2026",
                    ["GunSayisi"] = "5 Gün"
                });

            await _emailService.SendAsync(
                "haluk.saatci@granithome.com",
                "Şablon Testi",
                html);

            return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    ex.Message,
                    Inner = ex.InnerException?.Message,
                    Stack = ex.StackTrace
                });
            }
        }

        [HttpPost("workflow-test/{id}")]
        public async Task<IActionResult> WorkflowTest(int id)
        {
            try
            {
                await _workflowNotificationService
                    .SendWorkflowSubmittedAsync(id);

                return Ok(new
                {
                    Message = "Workflow mail testi başarılı."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Message = ex.Message,
                    Inner = ex.InnerException?.Message,
                    Stack = ex.StackTrace
                });
            }
        }
    }
}
