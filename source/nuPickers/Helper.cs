﻿namespace nuPickers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Web.Hosting;

    internal static class Helper
    {
        internal static IEnumerable<string> GetAssemblyNames()
        {
            List<string> assemblyNames = new List<string>();

            // check if the App_Code directory exists and has any files
            DirectoryInfo appCode = new DirectoryInfo(HostingEnvironment.MapPath("~/App_Code"));
            if (appCode.Exists && appCode.GetFiles().Length > 0)
            {
                // safety check to see if an assembly can be got from AppCode
                if (Helper.GetAssembly(appCode.Name) != null)
                {
                    assemblyNames.Add(appCode.Name);
                }
            }

            // add assemblies from the /bin directory
            assemblyNames.AddRange(Directory.GetFiles(HostingEnvironment.MapPath("~/bin"), "*.dll").Select(x => x.Substring(x.LastIndexOf('\\') + 1)));

            return assemblyNames;
        }

        /// <summary>
        /// attempts to get an assembly by it's name
        /// </summary>
        /// <param name="assemblyName"></param>
        /// <returns>an Assembly or null</returns>
        internal static Assembly GetAssembly(string assemblyName)
        {
            if (string.Equals(assemblyName, "App_Code", StringComparison.InvariantCultureIgnoreCase))
            {
				try
				{
					return Assembly.Load(assemblyName);
				}
				catch (FileNotFoundException)
				{
					return null;
				}
            }

            string assemblyFilePath = HostingEnvironment.MapPath(string.Concat("~/bin/", assemblyName));
            if (!string.IsNullOrEmpty(assemblyFilePath))
            {
                try
                {
                    // HACK: http://stackoverflow.com/questions/1031431/system-reflection-assembly-loadfile-locks-file
                    return Assembly.Load(File.ReadAllBytes(assemblyFilePath));
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        /// <summary>
        /// extension method on Assembly to handle reflection loading exceptions
        /// </summary>
        /// <param name="assembly">the assembly to get types from</param>
        /// <returns>a collection of types found</returns>
        internal static IEnumerable<Type> GetLoadableTypes(this Assembly assembly)
        {       
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(x => x != null);
            }
            catch
            {
                return Enumerable.Empty<Type>();
            }       
        }
    }
}
