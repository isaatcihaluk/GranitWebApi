using GranitWebApi.Data;
using GranitWebApi.Models.XML;
using GranitWebApi.Services.XML;

namespace GranitWebApi.Services.Background
{
    public class XmlBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public XmlBackgroundService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var xmlService = scope.ServiceProvider.GetRequiredService<IXmlService>();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var data = await xmlService.FetchTechnicians();

                    foreach (var item in data)
                    {
                        var existing = db.TechnicianLogs
                            .FirstOrDefault(x => x.RN == item.RN);

                        if (existing != null)
                        {
                            // 🔄 UPDATE (tracked entity üzerinde çalışıyoruz)

                            existing.M = item.M;
                            existing.MachineCode = item.MachineCode;
                            existing.MachineName = item.MachineName;

                            existing.Operator = item.Operator;
                            existing.OperatorName = item.OperatorName;
                            existing.OperatorCode = item.OperatorCode;

                            existing.Technician = item.Technician;
                            existing.TechnicianCode = item.TechnicianCode;
                            existing.TechnicianName = item.TechnicianName;

                            existing.TCall = item.TCall;
                            existing.TLogin = item.TLogin;
                            existing.TEnd = item.TEnd;

                            existing.Duration = item.Duration;
                            existing.ReactionTime = item.ReactionTime;

                            existing.Shift = item.Shift;

                            // EF zaten tracking yapıyor, ekstra bir şey gerekmez
                        }
                        else
                        {
                            // ➕ INSERT (IMPORTANT: yeni instance oluşturuyoruz)
                            db.TechnicianLogs.Add(new TechnicianModel
                            {
                                RN = item.RN,
                                M = item.M,
                                MachineCode = item.MachineCode,
                                MachineName = item.MachineName,

                                Operator = item.Operator,
                                OperatorName = item.OperatorName,
                                OperatorCode = item.OperatorCode,

                                Technician = item.Technician,
                                TechnicianCode = item.TechnicianCode,
                                TechnicianName = item.TechnicianName,

                                TCall = item.TCall,
                                TLogin = item.TLogin,
                                TEnd = item.TEnd,

                                Duration = item.Duration,
                                ReactionTime = item.ReactionTime,

                                Shift = item.Shift,
                                IsDetailed = false
                            });
                        }
                    }
                    await db.SaveChangesAsync();
                }
            }
        }
    }
}
