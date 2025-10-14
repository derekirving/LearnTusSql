using System.IO;

namespace Unify.Extensions
{
    public static class DirectoryInfoExtensions 
    { 
        public static void DeepCopy(this DirectoryInfo directory, string destinationDir) 
        { 
            foreach (var dir in Directory.GetDirectories(directory.FullName, "*", SearchOption.AllDirectories)) 
            {
                var dirToCreate = dir.Replace(directory.FullName, destinationDir); 
                Directory.CreateDirectory(dirToCreate); 
            } 
            
            foreach (var newPath in Directory.GetFiles(directory.FullName, "*.*", SearchOption.AllDirectories)) 
            { 
                File.Copy(newPath, newPath.Replace(directory.FullName, destinationDir), true); 
            } 
        } 
    }
}