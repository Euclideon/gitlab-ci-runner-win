using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using gitlab_ci_runner.conf;
using gitlab_ci_runner.helper;
using System.Runtime.InteropServices;

namespace gitlab_ci_runner.setup
{
    class Setup
	{
        /// <summary>
        /// Start the Setup
        /// </summary>
        public static void run()
        {
            Console.WriteLine("This seems to be the first run,");
            Console.WriteLine("please provide the following info to proceed:");
            Console.WriteLine();

            // Read coordinator URL
            String sCoordUrl = "";
            while (sCoordUrl == "")
            {
                Console.WriteLine("Please enter the gitlab-ci coordinator URL (e.g. http(s)://gitlab-ci.org:3000/ )");
                sCoordUrl = Console.ReadLine();
            }
            Config.url = sCoordUrl;
            Console.WriteLine();

            // Generate SSH Keys
            SSHKey.generateKeypair();

            // Register Runner
            registerRunner();
        }

        /// <summary>
        /// Register the runner with the coordinator
        /// </summary>
		private static void registerRunner()
		{
            // Read Token
            string sToken = "";
            while (sToken == "")
            {
                Console.WriteLine("Please enter the gitlab-ci token for this runner:");
                sToken = Console.ReadLine();
            }

			// Get tag list
			string sTaglist = "windows";

			// Append windows version
			Version ver = new Version(Registry.HKLM_GetString(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\", "CurrentVersion"));
			switch (ver.Major)
			{
				case 5:
					switch (ver.Minor)
					{
						case 0:
							sTaglist += ", windows 2000";
							break;
						case 1:
							sTaglist += ", windows xp";
							break;
						case 2:
							sTaglist += ", windows xp 64bit";
							break;
					}
					break;
				case 6:
					switch (ver.Minor)
					{
						case 0:
							sTaglist += ", windows vista";
							break;
						case 1:
							sTaglist += ", windows 7";
							break;
						case 2:
							sTaglist += ", windows 8";
							break;
						case 3:
							sTaglist += ", windows 8.1";
							break;
					}
					break;
			}

			// Append vs versions
			if (Registry.HKCR_PathExists(@"VisualStudio.DTE.8.0"))
				sTaglist += ", vs2005";
			if (Registry.HKCR_PathExists(@"VisualStudio.DTE.9.0"))
				sTaglist += ", vs2008";
			if (Registry.HKCR_PathExists(@"VisualStudio.DTE.10.0"))
				sTaglist += ", vs2010";
			if (Registry.HKCR_PathExists(@"VisualStudio.DTE.11.0"))
				sTaglist += ", vs2012";
			if (Registry.HKCR_PathExists(@"VisualStudio.DTE.12.0"))
				sTaglist += ", vs2013";

			// Get description
			string sDescription = Environment.MachineName;

            // Register Runner
            string sTok = Network.registerRunner(SSHKey.getPublicKey(), sToken, sTaglist, sDescription);
            if (sTok != null)
            {
                // Save Config
                Config.token = sTok;
                Config.saveConfig();

                Console.WriteLine();
                Console.WriteLine("Runner registered successfully. Feel free to start it!");
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("Failed to register this runner. Perhaps your SSH key is invalid or you are having network problems");
            }
        }
    }
}
