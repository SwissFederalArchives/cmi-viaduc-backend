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

        private readonly Dictionary<string, Assembly> loadedAssembliesByPath = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);

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
            var dir = Path.GetDirectoryName(path);

            ResolveEventHandler assemblyResolver = (sender, args) => ResolveAssembly(dir, args);
            AppDomain.CurrentDomain.AssemblyResolve += assemblyResolver;

            try
            {
                var exeAssembly = LoadAssembly(path);
                Add(dir, exeAssembly, path.ExtractServiceName());
            }
            finally
            {
                AppDomain.CurrentDomain.AssemblyResolve -= assemblyResolver;
            }
        }


        private void Add(string baseDir, Assembly asm, string servicename)
        {
            foreach (var t in GetLoadableTypes(asm, Console.Out).Where(t => t.IsSubclassOf(typeof(ApplicationSettingsBase))))
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
                        referencedAssembly = LoadAssembly(assemblyPath);
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

        private Assembly LoadAssembly(string assemblyPath)
        {
            if (loadedAssembliesByPath.TryGetValue(assemblyPath, out var assembly))
            {
                return assembly;
            }

            assembly = Assembly.LoadFrom(assemblyPath);
            loadedAssembliesByPath[assemblyPath] = assembly;
            return assembly;
        }

        private Assembly ResolveAssembly(string baseDir, ResolveEventArgs args)
        {
            var requestedAssemblyName = new AssemblyName(args.Name);

            var loadedAssembly = AppDomain.CurrentDomain
                .GetAssemblies()
                .FirstOrDefault(a => AssemblyName.ReferenceMatchesDefinition(a.GetName(), requestedAssemblyName));

            if (loadedAssembly != null)
            {
                return loadedAssembly;
            }

            var candidatePaths = new[]
            {
                Path.Combine(baseDir, requestedAssemblyName.Name + ".dll"),
                Path.Combine(baseDir, requestedAssemblyName.Name + ".exe")
            };

            foreach (var candidatePath in candidatePaths.Where(File.Exists))
            {
                return LoadAssembly(candidatePath);
            }

            return null;
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly, TextWriter log = null)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                log?.WriteLine($"Warning: Could not fully load assembly '{assembly.FullName}'.");

                foreach (var loaderEx in ex.LoaderExceptions.Where(e => e != null))
                {
                    log?.WriteLine($"  LoaderException: {loaderEx.Message}");
                }

                return ex.Types.Where(t => t != null);
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