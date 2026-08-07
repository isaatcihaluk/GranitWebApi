using GranitWebApi.Data;
using GranitWebApi.Helpers;
using GranitWebApi.Models.IK.Izin;
using GranitWebApi.Workflow.Enums;
using GranitWebApi.Workflow.Interfaces;
using GranitWebApi.Workflow.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace GranitWebApi.Controllers.Izin
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class IzinController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWorkflowService _workflowService;
        private readonly UserContext _userContext;

        public IzinController(AppDbContext context, IWorkflowService workflowService, UserContext userContext)
        {
            _context = context;
            _workflowService = workflowService;
            _userContext = userContext; ;
        }
        /// Aktif izin türlerini getirir.
        [HttpGet("izin-turleri")]
        public async Task<IActionResult> GetIzinTurleri()
        {
            try
            {
                var data = await _context.IzinTurleri
                    .Where(x => x.Aktif)
                    .OrderBy(x => x.Ad)
                    .Select(x => new
                    {
                        x.Id,
                        x.PolimekID,
                        x.Ad,
                        x.Aciklama
                    })
                    .ToListAsync();
                return Ok(data);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Message = ex.Message,
                    Inner = ex.InnerException?.Message
                });
            }
        }

        /// Yeni izin talebi oluşturur (Taslak).        
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] IzinTalebiOlusturModel model)
        {
            try
            {
                var userId = _userContext.UserId;
                if (model.BitisTarihi < model.BaslangicTarihi)
                {
                    return BadRequest(
                        "Bitiş tarihi başlangıç tarihinden küçük olamaz.");
                }

                // 1- İZİN TALEBİ OLUŞTUR

                var izin = new IzinTalepleri
                {
                    PersonelId = userId,
                    IzinTuruId = model.IzinTuruId,
                    BaslangicTarihi = model.BaslangicTarihi,
                    BitisTarihi = model.BitisTarihi,
                    GunSayisi = model.GunSayisi,
                    Aciklama = model.Aciklama,
                    Durum = WorkflowStatus.Taslak.ToString(),
                    OlusturmaTarihi = DateTime.Now,
                    Aktif = true
                };
                _context.IzinTalepleri.Add(izin);
                await _context.SaveChangesAsync();

                // 2- WORKFLOW TASLAK OLUŞTUR
                var processRequest = new ProcessRequest
                {
                    ProcessTypeId = 1,
                    CreatedBy = userId,
                    Title =
                    $"{izin.BaslangicTarihi:dd.MM.yyyy} - {izin.BitisTarihi:dd.MM.yyyy} İzin Talebi"
                };

                var processId =
                    await _workflowService.StartAsync(processRequest);

                // 3- BAĞLANTI KUR
                izin.ProcessRequestId = processId;
                var process = await _context.ProcessRequest.FirstAsync(x => x.Id == processId);
                process.EntityId = izin.Id;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Success = true,
                    Id = izin.Id,
                    ProcessRequestId = processId,
                    Message = "İzin talebi taslak olarak oluşturuldu."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Message = ex.Message,
                    Inner = ex.InnerException?.Message
                });
            }
        }

        /// İzin talebi detayını getirir.        
        [HttpGet("form/{id:int?}")]
        public async Task<IActionResult> Form(int? id)
        {
            try
            {
                var userId = _userContext.UserId;

                IzinTalepleri? izin = null;
                PersonelOrganizasyonDto? personel = null;

                // Mevcut kayıt açılıyorsa
                if (id.HasValue)
                {
                    izin = await _context.IzinTalepleri
                        .FirstOrDefaultAsync(x => x.Id == id.Value);

                    if (izin == null)
                        return NotFound();

                    // Talep sahibinin personel bilgisi
                    personel = await GetPersonelBilgi(izin.PersonelId);
                }
                else
                {
                    // Yeni kayıt açılıyorsa giriş yapan kişinin bilgisi
                    personel = await GetPersonelBilgi(userId);
                }

                if (personel == null) return NotFound("Personel bulunamadı.");

                var izinTurleri = await _context.IzinTurleri
                    .Where(x => x.Aktif)
                    .OrderBy(x => x.Ad)
                    .Select(x => new
                    {
                        x.Id,
                        x.PolimekID,
                        x.Ad,
                        x.Aciklama
                    })
                    .ToListAsync();


                object? talep = null;

                bool canEdit = false;
                bool canSubmit = false;
                bool canCancel = false;

                bool canApprove = false;
                bool canReject = false;
                bool canReturn = false;

                bool isOwner = false;
                bool isApprover = false;


                if (izin != null)
                {

                    var lastReturn = await _context.WorkflowHistory
                        .Where(x =>
                            x.RequestId == izin.ProcessRequestId &&
                            x.ActionType == (int)WorkflowAction.Revize)
                        .OrderByDescending(x => x.ActionDate)
                        .FirstOrDefaultAsync();


                    talep = new
                    {
                        izin.Id,
                        izin.IzinTuruId,
                        izin.BaslangicTarihi,
                        izin.BitisTarihi,
                        izin.GunSayisi,
                        izin.Aciklama,
                        izin.Durum,
                        izin.ProcessRequestId,
                        RevizeNotu = lastReturn?.Comment
                    };


                    isOwner = izin.PersonelId == userId;


                    if (izin.ProcessRequestId.HasValue)
                    {
                        isApprover = await _context.ProcessApproval.AnyAsync(x =>
                            x.RequestId == izin.ProcessRequestId &&
                            x.ApproverId == userId &&
                            x.IsActive);
                    }


                    var firstApprovalStarted =
                        await IsFirstApprovalStarted(izin.ProcessRequestId);



                    // Talep sahibi işlemleri
                    if (isOwner)
                    {
                        if (izin.Durum == WorkflowStatus.Revize.ToString())
                        {
                            canEdit = true;
                            canSubmit = true;
                            canCancel = false; // İstersen true da olabilir
                        }
                        else
                        {
                            canEdit =
                                !firstApprovalStarted &&
                                izin.Durum != WorkflowStatus.Tamamlandi.ToString() &&
                                izin.Durum != WorkflowStatus.Iptal.ToString();

                            canSubmit =
                                izin.Durum == WorkflowStatus.Taslak.ToString();

                            canCancel =
                                !firstApprovalStarted &&
                                izin.Durum != WorkflowStatus.Tamamlandi.ToString() &&
                                izin.Durum != WorkflowStatus.Iptal.ToString();
                        }
                    }

                    // Aktif onaycı işlemleri
                    if (isApprover)
                    {
                        canApprove = true;
                        canReject = true;
                        canReturn = true;
                    }

                }
                else
                {
                    // Yeni kayıt

                    canEdit = true;
                    canSubmit = true;
                    canCancel = true;
                }



                return Ok(new
                {
                    Personel = new
                    {
                        personel.Id,
                        Sicil = personel.Sicil,
                        AdSoyad = $"{personel.FirstName} {personel.LastName}",
                        Departman = personel.DepartmentName ?? "",
                        AltDepartman = personel.SubDepartmentName ?? "",
                        Birim = personel.UnitName ?? "",
                        AltBirim = personel.SubUnitName ?? "",
                        Yonetici = ""
                    },


                    IzinTurleri = izinTurleri,
                    Talep = talep,

                    IsOwner = isOwner,
                    IsApprover = isApprover,

                    CanEdit = canEdit,
                    CanSubmit = canSubmit,
                    CanCancel = canCancel,

                    CanApprove = canApprove,
                    CanReject = canReject,
                    CanReturn = canReturn
                });

            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Message = ex.Message,
                    Inner = ex.InnerException?.Message
                });
            }
        }

        /// Kullanıcının izin taleplerini listeler.
        [HttpGet("list")]
        public async Task<IActionResult> GetList()
        {
            try
            {
                var userId = _userContext.UserId;
                var data = await _context.IzinTalepleri
                    .Include(x => x.IzinTuru)
                    .Include(x => x.ProcessRequest)
                    .Where(x => x.PersonelId == userId)
                    .OrderByDescending(x => x.OlusturmaTarihi)
                    .Select(x => new
                    {
                        x.Id,
                        IzinTuru = x.IzinTuru != null ? x.IzinTuru.Ad : "",
                        x.BaslangicTarihi,
                        x.BitisTarihi,
                        x.GunSayisi,
                        x.Durum,
                        x.ProcessRequestId,
                        ProcessStatus = x.ProcessRequest != null ? x.ProcessRequest.Status : null,
                        x.OlusturmaTarihi,
                        DurumRenk =
                              x.Durum == WorkflowStatus.Taslak.ToString()
                                ? "secondary"
                            : x.Durum == WorkflowStatus.Onayda.ToString()
                                ? "warning"
                            : x.Durum == WorkflowStatus.Onaylandi.ToString()
                                ? "success"
                            : x.Durum == WorkflowStatus.Reddedildi.ToString()
                                ? "danger"
                            : x.Durum == WorkflowStatus.Revize.ToString()
                                ? "info"
                            : x.Durum == WorkflowStatus.Iptal.ToString()
                                ? "dark"
                            : x.Durum == WorkflowStatus.Tamamlandi.ToString()
                                ? "success"
                            : "secondary"

                    })
                    .ToListAsync();

                return Ok(data);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Message = ex.Message,
                    Inner = ex.InnerException?.Message
                });
            }
        }

        /// Taslak izin talebini günceller.
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] IzinTalebiOlusturModel model)
        {
            try
            {
                var userId = _userContext.UserId;
                var izin = await _context.IzinTalepleri.FirstOrDefaultAsync(x => x.Id == id);

                if (izin == null)
                    return NotFound();

                if (izin.PersonelId != userId)
                    return BadRequest(
                        "Bu talebi güncelleme yetkiniz yok.");

                // Sadece taslak ve revize düzenlenebilir
                if (izin.Durum != WorkflowStatus.Revize.ToString())
                {
                    if (await IsApprovalProcessed(izin.ProcessRequestId))
                    {
                        return BadRequest(
                            "Onay sürecinde işlem yapıldığı için talep güncellenemez.");
                    }
                }

                if (model.BitisTarihi < model.BaslangicTarihi)
                {
                    return BadRequest(
                        "Bitiş tarihi başlangıç tarihinden küçük olamaz.");
                }
                izin.IzinTuruId = model.IzinTuruId;
                izin.BaslangicTarihi = model.BaslangicTarihi;
                izin.BitisTarihi = model.BitisTarihi;
                izin.GunSayisi = model.GunSayisi;
                izin.Aciklama = model.Aciklama;
                izin.GuncellemeTarihi = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Success = true,
                    Message = "İzin talebi güncellendi."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Message = ex.Message,
                    Inner = ex.InnerException?.Message
                });
            }
        }

        /// İzin talebini onaya gönderir.        
        [HttpPost("gonder/{id:int}")]
        public async Task<IActionResult> Gonder(int id)
        {
            try
            {
                var userId = _userContext.UserId;

                var izin = await _context.IzinTalepleri.FirstOrDefaultAsync(x => x.Id == id);
                if (izin == null) return NotFound();
                if (izin.PersonelId != userId) return BadRequest("Bu talebi gönderme yetkiniz yok.");

                if (izin.Durum == WorkflowStatus.Tamamlandi.ToString()) return BadRequest("Tamamlanan talep tekrar gönderilemez.");

                if (izin.Durum == WorkflowStatus.Iptal.ToString()) return BadRequest("İptal edilen talep tekrar gönderilemez.");

                if (izin.IzinTuruId <= 0) return BadRequest("İzin türü seçilmelidir.");

                if (izin.BaslangicTarihi == default || izin.BitisTarihi == default)
                {
                    return BadRequest("Tarih bilgileri eksik.");
                }

                if (!izin.ProcessRequestId.HasValue)
                {
                    return BadRequest("Workflow kaydı bulunamadı.");
                }
                // REVİZE DURUMU

                if (izin.Durum == WorkflowStatus.Revize.ToString())
                {
                    await _workflowService.ResubmitAsync(izin.ProcessRequestId.Value, userId, "Revize sonrası tekrar onaya gönderildi.");
                }
                // TASLAK DURUMU
                else if (izin.Durum == WorkflowStatus.Taslak.ToString())
                {
                    await _workflowService.SubmitAsync(izin.ProcessRequestId.Value, userId, "Talep onaya gönderildi.");
                }
                else
                {
                    return BadRequest(
                        "Bu talep gönderilemez.");
                }
                izin.Durum = WorkflowStatus.Onayda.ToString();
                izin.GuncellemeTarihi = DateTime.Now;
                await _context.SaveChangesAsync();
                return Ok(new
                {
                    Success = true,

                    Message = "Talep onaya gönderildi.",

                    ProcessRequestId = izin.ProcessRequestId
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Message = ex.Message,

                    Inner = ex.InnerException?.Message
                });
            }
        }

        /// İzin talebini iptal eder.
        [HttpPost("iptal/{id:int}")]
        public async Task<IActionResult> Iptal(int id)
        {
            try
            {
                var userId = _userContext.UserId;

                var izin = await _context.IzinTalepleri.FirstOrDefaultAsync(x => x.Id == id);

                if (izin == null) return NotFound();
                if (izin.PersonelId != userId) return BadRequest("Bu talebi iptal etme yetkiniz yok.");
                if (izin.Durum == WorkflowStatus.Tamamlandi.ToString())
                {
                    return BadRequest("Tamamlanan talep iptal edilemez.");
                }
                if (izin.Durum == WorkflowStatus.Iptal.ToString())
                {
                    return BadRequest("Talep zaten iptal edilmiş.");
                }
                if (!izin.ProcessRequestId.HasValue)
                {
                    return BadRequest("Workflow kaydı bulunamadı.");
                }
                if (await IsApprovalProcessed(izin.ProcessRequestId))
                {
                    return BadRequest("Onay sürecinde işlem yapıldığı için iptal edilemez.");
                }

                await _workflowService.CancelAsync(
                    izin.ProcessRequestId.Value,
                    userId,
                    "Talep kullanıcı tarafından iptal edildi.");
                izin.Durum = WorkflowStatus.Iptal.ToString();
                izin.GuncellemeTarihi = DateTime.Now;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Success = true,
                    Message = "Talep iptal edildi."
                });

            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Message = ex.Message,
                    Inner = ex.InnerException?.Message
                });
            }
        }
        // YILLIK BAKİYE
        [HttpGet("bakiye")]
        public async Task<IActionResult> GetBakiye()
        {
            var userId = _userContext.UserId;

            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);

            if (user == null || string.IsNullOrWhiteSpace(user.Sicil)) return Ok(new { Bakiye = 0 });

            var bakiye = await GetYillikIzinBakiyeBySicil(user.Sicil);

            return Ok(new
            {
                user.Id,
                user.Sicil,
                Bakiye = bakiye
            });
        }

        [HttpGet("bakiye/{izinId:int}")]
        public async Task<IActionResult> GetBakiye(int izinId)
        {
            var izin = await _context.IzinTalepleri.FirstOrDefaultAsync(x => x.Id == izinId);

            if (izin == null) return NotFound();

            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == izin.PersonelId);

            if (user == null || string.IsNullOrWhiteSpace(user.Sicil)) return Ok(new { Bakiye = 0 });

            var bakiye = await GetYillikIzinBakiyeBySicil(user.Sicil);

            return Ok(new
            {
                Bakiye = bakiye
            });
        }

        [HttpPost("iptal-tamamlanan/{id:int}")]
        [Authorize(Roles = "Admin,IK")]
        public async Task<IActionResult> IptalTamamlanan(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var izin = await _context.IzinTalepleri
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (izin == null)
                    return NotFound();

                if (izin.Durum == WorkflowStatus.Iptal.ToString())
                    return BadRequest("Bu izin zaten iptal edilmiş.");

                if (izin.Durum != WorkflowStatus.Tamamlandi.ToString())
                    return BadRequest("Sadece tamamlanmış izinler iptal edilebilir.");

                await IzinBakiyesiniGeriYukleAsync(izin);

                izin.Durum = WorkflowStatus.Iptal.ToString();
                izin.GuncellemeTarihi = DateTime.Now;

                if (izin.ProcessRequestId.HasValue)
                {
                    var process = await _context.ProcessRequest
                        .FirstOrDefaultAsync(x =>
                            x.Id == izin.ProcessRequestId);

                    if (process != null)
                    {
                        process.Status = WorkflowStatus.Iptal.ToString();
                        process.IsCompleted = true;
                        process.CurrentStep = 0;
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    Success = true,
                    Message = "İzin başarıyla iptal edildi."
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                return BadRequest(new
                {
                    Message = ex.Message,
                    Inner = ex.InnerException?.Message
                });
            }
        }
        private async Task<PersonelOrganizasyonDto?> GetPersonelBilgi(int userId)
        {
            var result = await _context.Database
                .SqlQuery<PersonelOrganizasyonDto>(
                    $"EXEC dbo.granitSP_PersonelOrganizasyonGetir @UserId={userId}"
                )
                .ToListAsync();

            return result.FirstOrDefault();
        }
        private async Task<bool> IsApprovalProcessed(int? processRequestId)
        {
            if (!processRequestId.HasValue)
                return false;

            return await _context.ProcessApproval
                .AnyAsync(x =>
                    x.RequestId == processRequestId.Value &&
                    (
                        x.Status == "Onaylandi" ||
                        x.Status == "Reddedildi"
                    ));
        }
        private async Task<bool> IsFirstApprovalStarted(int? processRequestId)
        {
            if (!processRequestId.HasValue) return false;

            return await _context.ProcessApproval.AnyAsync(x =>
                x.RequestId == processRequestId.Value &&
                x.Status == WorkflowStatus.Onaylandi.ToString());
        }
        private async Task<decimal> GetYillikIzinBakiyeBySicil(string sicil)
        {
            var bakiye = await _context.IzinYillikBakiyePersonel.FirstOrDefaultAsync(x => x.Sicil == sicil);

            return bakiye?.Bakiye ?? 0;
        }
        private async Task IzinBakiyesiniGeriYukleAsync(IzinTalepleri izin)
        {
            // Daha önce bakiye düşülmediyse işlem yapma
            if (!izin.BakiyeDusuldu)
                return;

            // Sadece yıllık izin
            if (izin.IzinTuruId != 1)
                return;


            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == izin.PersonelId);


            if (user == null || string.IsNullOrWhiteSpace(user.Sicil))
                return;


            var bakiye = await _context.IzinYillikBakiyePersonel
                .FirstOrDefaultAsync(x => x.Sicil == user.Sicil);


            if (bakiye == null)
                return;


            // Bakiyeyi geri yükle
            bakiye.Bakiye += izin.GunSayisi;


            // Tekrar iade edilmesini engelle
            izin.BakiyeDusuldu = false;
        }



    }
}