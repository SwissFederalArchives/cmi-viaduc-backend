using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CMI.Tools.UrbanCode
{
    /// <summary>
    ///     Findet alle Host.exe Files in einem Verzeichnis und die darin enthaltenen Property Klassen.
    /// 
    /// </summary>
    public class Scanner
    {
        public List<string> Services { get; set; } = new List<string>();

        public Dictionary<Type, List<string>> ServicesByType { get; } = new Dictionary<Type, List<string>>();

        public void Scan(string directory)
        {
            var exeFiles = FindExeFiles(directory);
            ScanExeFiles(exeFiles);
        }

        private static string[] FindExeFiles(string s)
        {
            var dir = new DirectoryInfo(s);
            return dir
                .GetFiles("CMI.Host.*.exe", SearchOption.AllDirectories)
                .Select(fi => fi.FullName)
                .Where(path => !path.Contains(@"\obj\"))
                .ToArray();
        }

        private void ScanExeFiles(string[] exeFiles)
        {
            Services = exeFiles.Select(p => p.ExtractServiceName()).OrderBy(s => s).Distinct().ToList();

            foreach (var path in exeFiles)
            {
                FindParamsForExe(path);
            }
        }

        private void FindParamsForExe(string path)
        {
            var exeAssembly = Assembly.LoadFrom(path);
            var dir = Path.GetDirectoryName(path);
            Add(dir, exeAssembly, path.ExtractServiceName());
        }


        private void Add(string baseDir, Assembly asm, string servicename)
        {
            foreach (var t in asm.GetTypes().Where(t => t.IsSubclassOf(typeof(ApplicationSettingsBase))))
            {
                if (!ServicesByType.ContainsKey(t))
                {
                    ServicesByType.Add(t, new List<string>());
                }

                if (!ServicesByType[t].Contains(servicename))
                {
                    ServicesByType[t].Add(servicename);
                }
            }

            foreach (var assemblyName in asm.GetReferencedAssemblies())
            {
                if (assemblyName.Name.StartsWith("CMI."))
                {
                    var assemblyPath = Path.Combine(baseDir, assemblyName.Name + ".dll");

                    Assembly referencedAssembly = null;
                    try
                    {
                        referencedAssembly = Assembly.LoadFrom(assemblyPath);
                    }
                    catch (FileNotFoundException e)
                    {
                        Console.WriteLine(e);
                        continue;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }

                    Add(baseDir, referencedAssembly, servicename);
                }
            }
        }
    }

    public static class ServiceNameExtension
    {
        public static string ExtractServiceName(this string path)
        {
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
            return fileNameWithoutExtension.Split('.').First(s => s != "CMI" && s != "Host");
        }
    }
}