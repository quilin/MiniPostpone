using System;
using System.Threading.Tasks;

namespace MiniPostpone.MessageProvider
{
    public interface IPostponedMessageProvider
    {
        Task<Guid> ScheduleMessage(object message, string routingKey, TimeSpan timeout);
        Task<Guid> ScheduleMessage(object message, string routingKey, DateTime dateTime);
        Task CancelSchedule(Guid messageId);
    }
}