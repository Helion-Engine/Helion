using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using NLog;

namespace Helion.Util.Consoles.Commands
{
    public record ConsoleCommandData(Action<ConsoleCommandEventArgs> Action, ConsoleCommandAttribute Info, List<ConsoleCommandArgAttribute> Args);
    
    /// <summary>
    /// A collection of callable console commands.
    /// </summary>
    public class ConsoleCommands : IEnumerable<(string command, ConsoleCommandData data)>
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly Dictionary<string, ConsoleCommandData> m_nameToAction = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Tries to run any console command that has been registered. This is
        /// not case sensitive. Note that this guards against an invalid number
        /// of arguments.
        /// </summary>
        /// <param name="args">The console command arguments.</param>
        /// <returns>True on success, false if unable to run due to either a bad
        /// number of arguments, or no such registered method.</returns>
        public bool Invoke(ConsoleCommandEventArgs args)
        {
            if (m_nameToAction.TryGetValue(args.Command, out ConsoleCommandData? data))
            {
                if (args.Args.Count >= data.Args.Count(a => !a.Optional))
                {
                    data.Action(args);
                    return true;
                }
            }

            return false;
        }

        public bool TryGet(string command, [NotNullWhen(true)] out ConsoleCommandData? data)
        {
            return m_nameToAction.TryGetValue(command, out data);
        }
        
        /// <summary>
        /// Takes an object that has a bunch of annotated methods and registers
        /// them for invocation.
        /// </summary>
        /// <param name="obj">The instance to scan for annotated methods.</param>
        /// <exception cref="Exception">If any attributed method has the improper
        /// parameter count or type. It must have exactly one parameter that is
        /// of type ConsoleCommandEventArgs.</exception>
        public void RegisterMethodsOrThrow<T>(T obj) where T : notnull
        {
            // For some dumb reason, you must have both the binding flags of
            // BindingFlags.NonPublic | BindingFlags.Instance or else it will
            // not find private instance members. I thought none of the flags
            // would give everything, but I was wrong.
            MethodInfo[] methods = typeof(T).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);
            
            foreach (MethodInfo methodInfo in methods)
            {
                object? customAttr = methodInfo.GetCustomAttribute(typeof(ConsoleCommandAttribute), true);
                if (customAttr is not ConsoleCommandAttribute attr)
                    continue;
                
                ParameterInfo[] parameters = methodInfo.GetParameters();
                if (parameters.Length != 1)
                    throw new Exception($"Method {methodInfo.Name} in type {typeof(T).FullName} must only have one parameter");
                if (parameters[0].ParameterType != typeof(ConsoleCommandEventArgs))
                    throw new Exception($"Method {methodInfo.Name} in type {typeof(T).FullName} must accept a {nameof(ConsoleCommandEventArgs)}");
                
                Action<ConsoleCommandEventArgs> action = cmdArgs => methodInfo.Invoke(obj, new object?[] { cmdArgs } );
                List<ConsoleCommandArgAttribute> args = methodInfo
                    .GetCustomAttributes(typeof(ConsoleCommandArgAttribute))
                    .Cast<ConsoleCommandArgAttribute>()
                    .ToList();

                bool exists = m_nameToAction.ContainsKey(attr.Command);
                if (exists)
                    Log.Error($"Replacing existing console command {attr.Command}");
                
                m_nameToAction[attr.Command] = new ConsoleCommandData(action, attr, args);
            }
        }

        public IEnumerator<(string command, ConsoleCommandData data)> GetEnumerator()
        {
            foreach ((string command, ConsoleCommandData data) in m_nameToAction)
                yield return (command, data);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
