using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rolla.Domain.Entities;

namespace Rolla.Application.Interfaces;

public interface IChatService
{
    Task SaveMessageAsync(int tripId, string senderId, string message);
    Task<List<ChatMessage>> GetChatHistoryAsync(int tripId);
}