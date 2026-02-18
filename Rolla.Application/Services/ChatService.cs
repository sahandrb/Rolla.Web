using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Rolla.Application.Interfaces;
using Rolla.Domain.Entities;

namespace Rolla.Application.Services;

public class ChatService : IChatService
{
    private readonly IApplicationDbContext _context;

    public ChatService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task SaveMessageAsync(int tripId, string senderId, string message)
    {
        var chatMsg = new ChatMessage
        {
            TripId = tripId,
            SenderId = senderId,
            MessageText = message,
            SentAt = DateTime.UtcNow
        };

        _context.ChatMessages.Add(chatMsg); // ✅ اصلاح نام جدول
        await _context.SaveChangesAsync();
    }

    public async Task<List<ChatMessage>> GetChatHistoryAsync(int tripId)
    {
        // ✅ نام جدول باید ChatMessages (جمع) باشد، نه ChatMessage (مفرد)
        return await _context.ChatMessages
            .Where(m => m.TripId == tripId)
            .OrderBy(m => m.SentAt)
            .ToListAsync();
    }
}