using System;
using System.Linq;
using System.Reflection;
using Baseline;
using Baseline.Reflection;
using BaselineTypeDiscovery;
using Jasper.Attributes;
using Jasper.Configuration;

namespace Jasper
{
    internal static class ExtensionLoader
    {
        private static Assembly[] _extensions;

        internal static Assembly[] FindExtensionAssemblies(Assembly applicationAssembly)
        {
            if (_extensions != null) return _extensions;

            _extensions = AssemblyFinder
                .FindAssemblies(a => a.HasAttribute<JasperModuleAttribute>(), txt => { }, false)
                .Concat(AppDomain.CurrentDomain.GetAssemblies())
                .Distinct()
                .Where(a => a.HasAttribute<JasperModuleAttribute>())
                .ToArray();

            var names = _extensions.Select(x => x.GetName().Name);

            Assembly[] FindDependencies(Assembly a) => _extensions.Where(x => names.Contains(x.GetName().Name)).ToArray();


            _extensions = _extensions.TopologicalSort(FindDependencies, false).ToArray();

            return _extensions;
        }

        internal static void ApplyExtensions(JasperOptions options)
        {
            var assemblies = FindExtensionAssemblies(options.ApplicationAssembly);

            if (!assemblies.Any())
            {
                Console.WriteLine("No Jasper extensions are detected");
                return;
            }

            options.IncludeExtensionAssemblies(assemblies);

            var extensions = assemblies.Select(x => x.GetAttribute<JasperModuleAttribute>().ExtensionType)
                .Where(x => x != null)
                .Select(x => Activator.CreateInstance(x).As<IJasperExtension>())
                .ToArray();

            options.ApplyExtensions(extensions);
        }
    }
}
