using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rolla.Domain.Common;

namespace Rolla.Domain.Entities;

public class ChatMessage : BaseEntity
{
    public int TripId { get; set; }
    public string SenderId { get; set; } = default!;
    public string MessageText { get; set; } = default!;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}