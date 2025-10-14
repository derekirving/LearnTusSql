#if NET
using System.Collections.Generic;

namespace Unify.Models;

public class RequiredConnectionStrings
{
    public Dictionary<string,string>  ConnectionStrings { get; set; }
}
#endif