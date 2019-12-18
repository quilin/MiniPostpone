using System;
using System.Threading.Tasks;

namespace MiniPostpone.MessageProvider
{
    public interface IPostponedMessageProvider
    {
        Task<Guid> ScheduleMessage(object message, TimeSpan timeout);
        Task<Guid> ScheduleMessage(object message, DateTime dateTime);
        Task CancelSchedule(Guid messageId);
    }
}