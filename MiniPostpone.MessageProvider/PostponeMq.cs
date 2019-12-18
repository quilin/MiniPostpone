using System;

namespace MiniPostpone.MessageProvider
{
    public static class PostponeMq
    {
        public const string OutputExchangeName = "mpp.output";
        public const string OutputClearQueueName = "mpp.clear";
        public static string InputExchangeName(Guid messageId) => $"mpp.input.{messageId}";
        public static string InputQueueName(Guid messageId) => InputExchangeName(messageId);
    }
}