using Microsoft.EntityFrameworkCore;
using RootBackend.Data;
using RootBackend.Models;

namespace RootBackend.Services
{
    public class MessageService
    {
        private readonly MemoryContext _context;

        public MessageService(MemoryContext context)
        {
            _context = context;
        }

        public async Task<MessageLog> SaveUserMessageAsync(string content, string source = "chat")
        {
            var message = new MessageLog
            {
                Id = Guid.NewGuid(),
                Content = content,
                Sender = "user",
                Timestamp = DateTime.UtcNow,
                Type = "text",
                Source = source
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();
            return message;
        }

        public async Task<MessageLog> SaveBotMessageAsync(string content, string source = "ai")
        {
            var message = new MessageLog
            {
                Id = Guid.NewGuid(),
                Content = content,
                Sender = "bot",
                Timestamp = DateTime.UtcNow,
                Type = "text",
                Source = source
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();
            return message;
        }

        public async Task<List<MessageLog>> GetRecentMessagesAsync(int count = 50)
        {
            return await _context.Messages
                .OrderByDescending(m => m.Timestamp)
                .Take(count)
                .ToListAsync();
        }
    }
}