namespace AskMe.Application.Common.Exceptions;

// A normal user must never be able to manipulate another user's
// questions/answers by changing an id in the request - handlers throw this
// whenever the current user isn't the owner (or an admin).
public class ForbiddenException : Exception
{
    public ForbiddenException(string message = "You are not allowed to perform this action.") : base(message) { }
}
