using System.Text;

namespace GranitWebApi.Services.Email
{
    public class EmailTemplateService : IEmailTemplateService
    {
        private readonly IWebHostEnvironment _environment;

        public EmailTemplateService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<string> RenderAsync(
            string templateName,
            Dictionary<string, string> values)
        {
            var templateFolder = Path.Combine(
                _environment.ContentRootPath,
                "Services",
                "Email",
                "Templates");

            var layoutPath = Path.Combine(templateFolder, "Layout.html");
            var templatePath = Path.Combine(templateFolder, $"{templateName}.html");

            var layout = await File.ReadAllTextAsync(layoutPath, Encoding.UTF8);
            var body = await File.ReadAllTextAsync(templatePath, Encoding.UTF8);

            foreach (var item in values)
            {
                body = body.Replace($"{{{{{item.Key}}}}}", item.Value ?? "");
            }

            layout = layout.Replace("{{CONTENT}}", body);

            return layout;
        }
    }
}