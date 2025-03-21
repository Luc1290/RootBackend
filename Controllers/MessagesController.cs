using Microsoft.AspNetCore.Mvc;
using RootBackend.Data;
using RootBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace RootBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessagesController : ControllerBase
    {
        private readonly MemoryContext _context;

        public MessagesController(MemoryContext context)
        {
            _context = context;
        }

        // POST: api/messages
        [HttpPost]
        public async Task<IActionResult> PostMessage([FromBody] MessageLog message)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();
                return Ok(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ ERREUR MESSAGE DB : " + ex.Message);
                return StatusCode(500, new { error = ex.Message });
            }
        }


        // GET: api/messages
        [HttpGet]
        public async Task<IActionResult> GetMessages()
        {
            var messages = await _context.Messages.OrderByDescending(m => m.Timestamp).Take(100).ToListAsync();
            return Ok(messages);
        }

        // GET: api/messages/source/public
        [HttpGet("source/{source}")]
        public async Task<IActionResult> GetMessagesBySource(string source)
        {
            var messages = await _context.Messages
                .Where(m => m.Source.ToLower() == source.ToLower())
                .OrderByDescending(m => m.Timestamp)
                .Take(100)
                .ToListAsync();
            return Ok(messages);
        }
    }
}