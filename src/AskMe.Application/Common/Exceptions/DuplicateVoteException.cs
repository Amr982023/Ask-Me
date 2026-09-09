namespace AskMe.Application.Common.Exceptions;

public class DuplicateVoteException : Exception
{
    public DuplicateVoteException() : base("This IP address has already voted on this question.") { }
}
