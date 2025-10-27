using Serilog;
namespace Unify.Logging;

public class UnifyLogger
{
    public ILogger AsEventLogger(string name)
    {
        var logger = Log.ForContext("event-type", name);
        return logger;
    }
}