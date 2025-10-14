using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Unify;

public class BuildInfo
{
    public string AssemblyVersion { get; set; }
    public string GitRepo { get; set; }
    public string GitHash { get; set; }
    public string RunTime { get; set; }

    public static string GitHashOnly([Optional] Assembly assembly)
    {
        if (assembly == null)
        {
            assembly = Assembly.GetCallingAssembly();
        }

        try
        {
            var info = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
                .InformationalVersion;
            
            return info.Split('+')[1].Substring(0,7);
        }
        catch
        {
            return string.Empty;
        }
    }
    public static string ReportAsHtml([Optional] Assembly assembly)
    {
        if (assembly == null)
        {
            assembly = Assembly.GetCallingAssembly();
        }
        
        var buildInfo = Report(assembly);
        
        buildInfo.GitRepo = buildInfo.GitRepo.Replace(".git", "");
                
        var isDirty = "";
        
        if (buildInfo.GitHash.EndsWith("-dirty"))
        {
            isDirty = "(dirty)";
            buildInfo.GitHash = buildInfo.GitHash.Replace("-dirty", "");
        }

        return
            $"Runtime {buildInfo.RunTime}. Version: {buildInfo.AssemblyVersion} Commit: <a href='{buildInfo.GitRepo}/commit/{buildInfo.GitHash}'>{buildInfo.GitHash}</a> {isDirty}";
    }

    public static BuildInfo Report([Optional] Assembly assembly)
    {
        if (assembly == null)
        {
            assembly = Assembly.GetCallingAssembly();
        }

        return new BuildInfo
        {
            AssemblyVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
                .InformationalVersion,
            GitRepo = assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(attr => attr.Key == "GitRepo")
                ?.Value,
            GitHash = assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(attr => attr.Key == "GitHash")
                ?.Value,
            RunTime = RuntimeInformation.FrameworkDescription
        };
    }
}