namespace GranitWebApi.Services.Email
{
    public interface IEmailTemplateService
    {
        Task<string> RenderAsync(
            string templateName,
            Dictionary<string, string> values);
    }
}