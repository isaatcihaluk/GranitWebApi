using GranitWebApi.Helpers;
using GranitWebApi.Workflow.Interfaces;
using GranitWebApi.Workflow.Models;
using Microsoft.AspNetCore.Mvc;

namespace GranitWebApi.Workflow.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WorkflowController : ControllerBase
    {
        private readonly IWorkflowService _workflowService;
        private readonly UserContext _userContext;

        public WorkflowController(IWorkflowService workflowService, UserContext userContext)
        {
            _workflowService = workflowService;
            _userContext = userContext;
        }

        #region Process

        [HttpPost("start")]
        public async Task<IActionResult> Start(ProcessRequest request)
        {
            try
            {
                var id = await _workflowService.StartAsync(request);

                return Ok(new
                {
                    Success = true,
                    RequestId = id
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = ex.Message,
                    Inner = ex.InnerException?.Message,
                    Stack = ex.StackTrace
                });
            }
        }

        [HttpPost("submit/{requestId}")]
        public async Task<IActionResult> Submit(int requestId,[FromBody] WorkflowActionRequest model)
        {
            try
            {
                await _workflowService.SubmitAsync(
                    requestId,
                    _userContext.UserId,
                    model.Comment);

                return Ok(new
                {
                    Success = true
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

        [HttpPost("approve/{requestId}")]
        public async Task<IActionResult> Approve(int requestId,[FromBody] WorkflowActionRequest model)
        {
            try
            {
                await _workflowService.ApproveAsync(
                    requestId,
                    _userContext.UserId,
                    model.Comment);

                return Ok(new
                {
                    Success = true
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("reject/{requestId}")]
        public async Task<IActionResult> Reject(int requestId,[FromBody] WorkflowActionRequest model)
        {
            try
            {
                await _workflowService.RejectAsync(
                    requestId,
                    _userContext.UserId,
                    model.Comment);

                return Ok(new
                {
                    Success = true
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("return/{requestId}")]
        public async Task<IActionResult> Return(int requestId,[FromBody] WorkflowActionRequest model)
        {
            try
            {
                await _workflowService.ReturnAsync(
                    requestId,
                    _userContext.UserId,
                    model.Comment);

                return Ok(new
                {
                    Success = true
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("resubmit/{requestId}")]
        public async Task<IActionResult> Resubmit(int requestId,[FromBody] WorkflowActionRequest model)
        {
            try
            {
                await _workflowService.ResubmitAsync(
                    requestId,
                    _userContext.UserId,
                    model.Comment);

                return Ok(new
                {
                    Success = true
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("cancel/{requestId}")]
        public async Task<IActionResult> Cancel(int requestId,[FromBody] WorkflowActionRequest model)
        {
            try
            {
                await _workflowService.CancelAsync(
                    requestId,
                    _userContext.UserId,
                    model.Comment);

                return Ok(new
                {
                    Success = true
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        #endregion
    }
}