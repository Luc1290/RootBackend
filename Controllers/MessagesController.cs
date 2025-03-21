using Microsoft.AspNetCore.Mvc;
using RootBackend.Data;
using RootBackend.Models;

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
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();
            return Ok(message);
        }

        // GET: api/messages
        [HttpGet]
        public IActionResult GetMessages()
        {
            return Ok(_context.Messages.OrderByDescending(m => m.Timestamp).Take(100));
        }

        // GET: api/messages/source/public
        [HttpGet("source/{source}")]
        public IActionResult GetMessagesBySource(string source)
        {
            return Ok(_context.Messages
                .Where(m => m.Source.ToLower() == source.ToLower())
                .OrderByDescending(m => m.Timestamp)
                .Take(100));
        }
    }
}
