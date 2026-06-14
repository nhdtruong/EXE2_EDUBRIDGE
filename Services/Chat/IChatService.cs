using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EduBridge.Models.DTOs.TeacherChat;

namespace EduBridge.Services.Chat
{
    public interface IChatService
    {
        Task<List<ConversationDto>> GetTeacherConversationsAsync(int teacherUserId, CancellationToken cancellationToken = default);
        Task<List<ConversationDto>> GetParentConversationsAsync(int parentUserId, CancellationToken cancellationToken = default);
        Task<List<ChatMessageDto>> GetChatHistoryAsync(int currentUserId, int contactUserId, CancellationToken cancellationToken = default);
        Task<bool> MarkAsReadAsync(int currentUserId, int senderUserId, CancellationToken cancellationToken = default);
    }
}
