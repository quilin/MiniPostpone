using System;

namespace MiniPostpone.MessageProvider
{
    public class PostponedMessageException : Exception
    {
        public PostponedMessageException(string message) : base(message)
        {
        }
    }
}