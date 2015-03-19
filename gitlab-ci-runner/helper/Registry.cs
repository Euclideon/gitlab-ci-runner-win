using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace gitlab_ci_runner.helper
{
	class Registry
	{
		/// <summary>
		/// Gets the value of a registry key as a string in HKEY_LOCAL_MACHINE
		/// </summary>
		/// <param name="path">Path where key is located</param>
		/// <param name="key">Key containing required value</param>
		public static string HKLM_GetString(string path, string key)
		{
			try
			{
				RegistryKey rk = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(path);
				if (rk == null) return "";
				return (string)rk.GetValue(key);
			}
			catch { return ""; }
		}

		/// <summary>
		/// Checks if registry path exists in HKEY_LOCAL_MACHINE
		/// </summary>
		/// <param name="path">path to check</param>
		public static bool HKCR_PathExists(string path)
		{
			try
			{
				RegistryKey rk = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(path);
				return rk != null;
			}
			catch { return false; }
		}
	}
}
