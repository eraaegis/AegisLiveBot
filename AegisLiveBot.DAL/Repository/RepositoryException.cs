using System;
using System.Collections.Generic;
using System.Text;

namespace AegisLiveBot.DAL.Repository
{
    public class RepositoryException : Exception
    {
        public RepositoryException() { }
        public RepositoryException(string msg) : base(msg) { }
        public RepositoryException(string msg, Exception inner) : base(msg, inner) { }
    }
    public class UserNotFoundException : RepositoryException
    {
        public UserNotFoundException() : base("User not found!") { }
    }
    public class UserNotRegisteredException : RepositoryException
    {
        public UserNotRegisteredException() : base("User not registered for streaming role!") { }
    }
    public class NoRoastMsgException : RepositoryException
    {
        public NoRoastMsgException() : base("does not realize that this bot does not roast people.") { }
    }
    public class ZeroOrNegativeRoastException : RepositoryException
    {
        public ZeroOrNegativeRoastException() : base("has got to be trolling.") { }
    }
    public class OutOfRangeRoastException : RepositoryException
    {
        public OutOfRangeRoastException(int index) : base($"is bigger than {index} buddy.") { }
    }
}
