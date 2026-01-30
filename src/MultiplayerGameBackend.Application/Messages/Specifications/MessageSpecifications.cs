using System.Linq.Expressions;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Application.Messages.Specifications;

public static class MessageSpecifications
{
    public static Expression<Func<Message, bool>> ContainsUser(Guid userId)
    {
        return m => m.SenderId == userId || m.ReceiverId == userId;
    }
    
    public static Expression<Func<Message, bool>> BetweenUsers(Guid firstUserId, Guid secondUserId)
    {
        return m => (m.SenderId == firstUserId && m.ReceiverId == secondUserId) ||
               (m.SenderId == secondUserId && m.ReceiverId == firstUserId);
    }
    
    public static Expression<Func<Message, Guid>> GetOtherUserId(Guid firstUserId)
    {
        return m => m.SenderId == firstUserId ? m.ReceiverId : m.SenderId;
    }
    
    public static Expression<Func<Message, bool>> UnreadByUser(Guid userId)
    {
        return m => m.ReceiverId == userId && !m.IsRead;
    }
    
    public static Expression<Func<Message, bool>> UnreadBetweenUsers(Guid senderId, Guid receiverId)
    {
        return m => m.SenderId == senderId &&
                    m.ReceiverId == receiverId &&
                    !m.IsRead;
    }
}