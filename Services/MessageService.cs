using Microsoft.EntityFrameworkCore;
using RootBackend.Data;
using RootBackend.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RootBackend.Services
{
    public class MessageService
    {
        private readonly MemoryContext _context;
        private static readonly Dictionary<string, DateTime> _lastUserMessages = new Dictionary<string, DateTime>();
        private static readonly object _lockObj = new object();

        public MessageService(MemoryContext context)
        {
            _context = context;
        }

        public async Task<MessageLog> SaveUserMessageAsync(string content, string source = "chat", string userId = "anonymous")
        {
            // Vérifier s'il s'agit d'un double message (même contenu dans un court laps de temps pour le même utilisateur)
            bool isDuplicate = false;
            lock (_lockObj)
            {
                string key = $"{userId}:{content}";
                if (_lastUserMessages.TryGetValue(key, out DateTime lastTime))
                {
                    // Si le même message a été envoyé dans les 3 dernières secondes par le même utilisateur
                    if ((DateTime.UtcNow - lastTime).TotalSeconds < 3)
                    {
                        isDuplicate = true;
                        Console.WriteLine($"Double message détecté pour l'utilisateur {userId}: {content.Substring(0, Math.Min(20, content.Length))}...");
                    }
                }

                // Mettre à jour ou ajouter l'horodatage du message
                _lastUserMessages[key] = DateTime.UtcNow;

                // Nettoyer les anciens messages
                if (_lastUserMessages.Count > 1000)
                {
                    var keysToRemove = new List<string>();
                    foreach (var entry in _lastUserMessages)
                    {
                        if ((DateTime.UtcNow - entry.Value).TotalMinutes > 10)
                        {
                            keysToRemove.Add(entry.Key);
                        }
                    }
                    foreach (var removeKey in keysToRemove)
                    {
                        _lastUserMessages.Remove(removeKey);
                    }
                }
            }


            // Si c'est un double message, on peut marquer la source ou ignorer
            if (isDuplicate)
            {
                // Option 1: Marquer le message comme doublon
                source = $"{source}-duplicate";

                // Option 2: Ignorer complètement le message (décommenter pour activer)
                // return await GetLastBotMessageAsync(userId);
            }

            var message = new MessageLog
            {
                Content = content,
                Sender = "user",
                Timestamp = DateTime.UtcNow,
                Type = "text",
                Source = source,
                UserId = userId
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();
            return message;
        }

        public async Task<MessageLog> SaveBotMessageAsync(string content, string source = "ai", string userId = "anonymous")
        {
            var message = new MessageLog
            {
                Content = content,
                Sender = "bot",
                Timestamp = DateTime.UtcNow,
                Type = "text",
                Source = source,
                UserId = userId
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();
            return message;
        }

        // Récupérer la dernière réponse du bot pour un utilisateur spécifique
        public async Task<MessageLog?> GetLastBotMessageAsync(string userId)
        {
            return await _context.Messages
                .Where(m => m.Sender == "bot" && m.UserId == userId)
                .OrderByDescending(m => m.Timestamp)
                .FirstOrDefaultAsync();
        }


        // Récupérer les messages récents, globalement ou par utilisateur
        public async Task<List<MessageLog>> GetRecentMessagesAsync(int count = 50, string? userId = null)

        {
            var query = _context.Messages.AsQueryable();

            // Si un userId est spécifié, filtrer par cet utilisateur
            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(m => m.UserId == userId);
            }

            return await query
                .OrderByDescending(m => m.Timestamp)
                .Take(count)
                .ToListAsync();
        }

        // Récupérer les conversation d'un utilisateur spécifique
        public async Task<List<MessageLog>> GetUserConversationAsync(string userId, int count = 50)
        {
            return await _context.Messages
                .Where(m => m.UserId == userId)
                .OrderByDescending(m => m.Timestamp)
                .Take(count)
                .ToListAsync();
        }
    }
}