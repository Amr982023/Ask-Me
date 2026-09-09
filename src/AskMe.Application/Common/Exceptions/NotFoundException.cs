namespace AskMe.Application.Common.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string entity, object key) : base($"{entity} with id '{key}' was not found.") { }
}
