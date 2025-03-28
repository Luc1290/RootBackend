﻿using Microsoft.AspNetCore.Mvc;
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
        public async Task<IActionResult> GetMessages([FromHeader(Name = "ADMIN_API_TOKEN")] string? adminToken)
        {
            try
            {
                // Notez que j'ai modifié X-Admin-Token en ADMIN_API_TOKEN pour correspondre à votre frontend
                var expectedToken = Environment.GetEnvironmentVariable("ADMIN_API_TOKEN");

                if (string.IsNullOrEmpty(adminToken) || adminToken != expectedToken)
                {
                    return StatusCode(401, new { error = "Accès refusé : Token invalide !" });
                }

                var messages = await _context.Messages.ToListAsync();
                return Ok(messages);
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ ERREUR GET MESSAGES : " + ex.Message);
                return StatusCode(500, new { error = ex.Message });
            }
        }

    }
}