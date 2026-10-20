namespace Arts.Application.Exceptions;

public class ForbiddenException : Exception
{
    public ForbiddenException(string message = "Доступ запрещен") : base(message)
    {
    }
}