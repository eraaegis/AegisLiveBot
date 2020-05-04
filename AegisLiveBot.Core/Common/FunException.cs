using System;
using System.Collections.Generic;
using System.Text;

namespace AegisLiveBot.Core.Common
{
    public class FunException : Exception
    {
        public FunException() { }
        public FunException(string msg) : base(msg) { }
        public FunException(string msg, Exception inner) : base(msg, inner) { }
    }
    public class StringToPointException : FunException
    {
        public StringToPointException() : base("Improper move command format!") { }
    }
    public class PointOutOfBoundsException : FunException
    {
        public PointOutOfBoundsException() : base("Enter a valid location!") { }
    }
    public class InvalidMoveException : FunException
    {
        public InvalidMoveException() : base("Invalid move!") { }
    }
    public class InvalidPieceException : FunException
    {
        public InvalidPieceException() : base("Specify a valid piece!") { }
    }
    public class ParsePointException : FunException
    {
        public ParsePointException() : base("Enter a valid grid!") { }
    }
    public class CheckMoveException : FunException
    {
        public CheckMoveException() : base("This move puts you in check!") { }
    }
    public class InvalidPromotionException : FunException
    {
        public InvalidPromotionException() : base("Please specify promotion choice with: promote q/r/k/b") { }
    }
}
