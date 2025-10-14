#if NET
#nullable enable
using Microsoft.Extensions.Logging;
using Polly;

namespace Unify.Configuration;

public static class PollyContextExtensions
{
    private const string Logger = "logger";
    
    public static bool TryGetLogger(this Context context, out ILogger? logger)
    {
        if (context.TryGetValue(Logger, out var loggerObject) && loggerObject is ILogger theLogger)
        {
            logger = theLogger;
            return true;
        }

        logger = null;
        return false;
    }
    
    public static Context AddLogger(this Context context, ILogger logger)
    {
        context[Logger] = logger;
        return context;
    }
}
#endif