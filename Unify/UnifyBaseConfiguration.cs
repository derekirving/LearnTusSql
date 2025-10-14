#if NET
using System;

namespace Unify;

public class UnifyBaseConfiguration
{
    public DateTime UnifyConfigLoaded { get; set; }
    public string ApplicationName { get; init; } = default!;
    public string DefaultCulture { get; set; } = "en-GB";
    public Uri BaseUri { get; set; }
    public string BasePath { get; set; }
    public string GitHash { get; set; }
}
#endif