using System;
using NFluidsynth.Native;

namespace NFluidsynth
{
	public class ConsoleLogger
	{
		public static void LogMessage (Logger.LogLevel level, string message, IntPtr data)
		{
			Console.WriteLine ($"NFluidsynthAndroidLogger ({level}): {message}");
		}
	}
	
	public class Logger
	{
		public delegate void LoggerDelegate (LogLevel level, string message, IntPtr data);

		public enum LogLevel {
			Panic,
			Error,
			Warning,
			Information,
			Debug,
		}
		
		public static void SetLoggerMethod (LoggerDelegate method)
		{
			LibFluidsynth.fluid_set_log_function ((int) LogLevel.Panic, method, IntPtr.Zero);
			LibFluidsynth.fluid_set_log_function ((int) LogLevel.Error, method, IntPtr.Zero);
			LibFluidsynth.fluid_set_log_function ((int) LogLevel.Warning, method, IntPtr.Zero);
			LibFluidsynth.fluid_set_log_function ((int) LogLevel.Information, method, IntPtr.Zero);
			LibFluidsynth.fluid_set_log_function ((int) LogLevel.Debug, method, IntPtr.Zero);
		}
		
	}
}

