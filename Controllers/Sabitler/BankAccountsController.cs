using GranitWebApi.Data;
using GranitWebApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GranitWebApi.Controllers.Sabitler
{
    [ApiController]
    [Route("api/[controller]")]
    public class BankAccountsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BankAccountsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/BankAccounts
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var accounts = await _context.BankAccounts.AsNoTracking()
                .OrderBy(x => x.BankName).ThenBy(x => x.AccountOwner).ToListAsync();
            return Ok(accounts);
        }

        // GET: api/BankAccounts/1
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var account = await _context.BankAccounts.AsNoTracking().FirstOrDefaultAsync(x => x.ID == id);
            if (account == null) return NotFound(new { message = "Banka hesabı bulunamadı." });
            return Ok(account);
        }

        // POST: api/BankAccounts
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] BankAccount model)
        {
            model.ID = 0;
            _context.BankAccounts.Add(model);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById),new { id = model.ID },model);
        }

        // PUT: api/BankAccounts/1
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id,[FromBody] BankAccount model)
        {
            var account = await _context.BankAccounts.FirstOrDefaultAsync(x => x.ID == id);
            if (account == null) return NotFound(new { message = "Banka hesabı bulunamadı." });

            account.BankName = model.BankName;
            account.AccountOwner = model.AccountOwner;
            account.Branch = model.Branch;
            account.SwiftCode = model.SwiftCode;
            account.IBAN = model.IBAN;

            await _context.SaveChangesAsync();
            return Ok(account);
        }
    }
}
