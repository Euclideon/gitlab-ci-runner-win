using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using gitlab_ci_runner.api;
using Microsoft.Experimental.IO;
using gitlab_ci_runner.helper;

namespace gitlab_ci_runner.runner
{
    class Build
    {
        /// <summary>
        /// Build completed?
        /// </summary>
        public bool completed { get; private set; }

        /// <summary>
        /// Command output
        /// Build internal!
        /// </summary>
        private ConcurrentQueue<string> outputList;

        /// <summary>
        /// Command output
        /// </summary>
        public string output
        {
            get
            {
                string t;
                while (outputList.TryPeek(out t) && string.IsNullOrEmpty(t))
                {
                    outputList.TryDequeue(out t);
                }
                return String.Join("\n", outputList.ToArray()) + "\n";
            }
        }

        /// <summary>
        /// Projects Directory
        /// </summary>
        private string sProjectsDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\projects";

        /// <summary>
        /// Project Directory
        /// </summary>
        private string sProjectDir;

        /// <summary>
        /// Build Infos
        /// </summary>
        public BuildInfo buildInfo;

        /// <summary>
        /// Command list
        /// </summary>
        private LinkedList<string> commands;

        /// <summary>
        /// Execution State
        /// </summary>
        public State state = State.WAITING;

        /// <summary>
        /// Command Timeout
        /// </summary>
        public int iTimeout
        {
            get
            {
                return this.buildInfo.timeout;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="buildInfo">Build Info</param>
        public Build(BuildInfo buildInfo)
        {
            this.buildInfo = buildInfo;
            sProjectDir = sProjectsDir + @"\project-" + buildInfo.project_id;
            commands = new LinkedList<string>();
            outputList = new ConcurrentQueue<string>();
            completed = false;
        }

        /// <summary>
        /// Run the Build Job
        /// </summary>
        public void run()
        {
            state = State.RUNNING;

            try {

                // Initialize project dir
                initProjectDir();

                // Add build commands
                foreach (string sCommand in buildInfo.GetCommands ())
                {
                    commands.AddLast(sCommand);
                }

				string batchFile = sProjectsDir + "\\build-" + buildInfo.project_id + ".bat";
				File.WriteAllLines(batchFile, commands);

                // Execute
				if (!exec(batchFile))
                {
                    state = State.FAILED;
                }

                if (state == State.RUNNING)
                {
                    state = State.SUCCESS;
                }

            } catch (Exception rex) {
                outputList.Enqueue("");
                outputList.Enqueue("A runner exception occoured: " + rex.Message);
                outputList.Enqueue("");
                state = State.FAILED;
            }


            completed = true;
        }

        /// <summary>
        /// Initialize project dir and checkout repo
        /// </summary>
        private void initProjectDir()
        {
            // Check if projects directory exists
            if (!Directory.Exists(sProjectsDir))
            {
                // Create projects directory
                Directory.CreateDirectory(sProjectsDir);
            }

            // Check if already a git repo
            if (Directory.Exists(sProjectDir + @"\.git") && buildInfo.allow_git_fetch)
            {
                // Already a git repo, pull changes
                commands.AddLast(fetchCmd());
                commands.AddLast(checkoutCmd());
            }
            else
            {
                // No git repo, checkout
                if (Directory.Exists(sProjectDir))
                    DeleteDirectory(sProjectDir);

                commands.AddLast(cloneCmd());
                commands.AddLast(checkoutCmd());
			}
        }

        /// <summary>
        /// Execute a command
        /// </summary>
		/// <param name="batchFile">Batch file to execute</param>
        private bool exec(string batchFile)
        {
            try
            {
                // Build process
                Process p = new Process();
                p.StartInfo.UseShellExecute = false;
                if (Directory.Exists(sProjectDir))
                {
                    p.StartInfo.WorkingDirectory = sProjectDir; // Set Current Working Directory to project directory
                }
				p.StartInfo.FileName = batchFile;

                // Environment variables
                p.StartInfo.EnvironmentVariables["HOME"] = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile); // Fix for missing SSH Key

                p.StartInfo.EnvironmentVariables["BUNDLE_GEMFILE"] = sProjectDir + @"\Gemfile";
                p.StartInfo.EnvironmentVariables["BUNDLE_BIN_PATH"] = "";
                p.StartInfo.EnvironmentVariables["RUBYOPT"] = "";

                p.StartInfo.EnvironmentVariables["CI_SERVER"] = "yes";
                p.StartInfo.EnvironmentVariables["CI_SERVER_NAME"] = "GitLab CI";
                p.StartInfo.EnvironmentVariables["CI_SERVER_VERSION"] = null; // GitlabCI Version
                p.StartInfo.EnvironmentVariables["CI_SERVER_REVISION"] = null; // GitlabCI Revision

                p.StartInfo.EnvironmentVariables["CI_BUILD_REF"] = buildInfo.sha;
                p.StartInfo.EnvironmentVariables["CI_BUILD_REF_NAME"] = buildInfo.ref_name;
                p.StartInfo.EnvironmentVariables["CI_BUILD_ID"] = buildInfo.id.ToString();

				if (Registry.HKCR_PathExists(@"VisualStudio.DTE.9.0"))
					p.StartInfo.EnvironmentVariables["MSBUILD_2008"] = "\"" + Registry.HKLM_GetString(@"Software\Microsoft\MSBuild\ToolsVersions\4.0", "MSBuildToolsPath") + "\\msbuild.exe\" /pVisualStudioVersion:9.0 /p:PlatformToolset=v90";

				if (Registry.HKCR_PathExists(@"VisualStudio.DTE.10.0"))
					p.StartInfo.EnvironmentVariables["MSBUILD_2010"] = "\"" + Registry.HKLM_GetString(@"Software\Microsoft\MSBuild\ToolsVersions\4.0", "MSBuildToolsPath") + "\\msbuild.exe\" /pVisualStudioVersion:10.0 /p:PlatformToolset=v100";

				if (Registry.HKCR_PathExists(@"VisualStudio.DTE.11.0"))
					p.StartInfo.EnvironmentVariables["MSBUILD_2012"] = "\"" + Registry.HKLM_GetString(@"Software\Microsoft\MSBuild\ToolsVersions\4.0", "MSBuildToolsPath") + "\\msbuild.exe\" /pVisualStudioVersion:11.0 /p:PlatformToolset=v110";

				if (Registry.HKCR_PathExists(@"VisualStudio.DTE.12.0"))
					p.StartInfo.EnvironmentVariables["MSBUILD_2013"] = "\"" + Registry.HKLM_GetString(@"Software\Microsoft\MSBuild\ToolsVersions\12.0", "MSBuildToolsPath") + "\\msbuild.exe\"";

                // Redirect Standard Output and Standard Error
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.OutputDataReceived += new DataReceivedEventHandler(outputHandler);
                p.ErrorDataReceived += new DataReceivedEventHandler(outputHandler);

                try
                {
                    // Run the command
                    p.Start();
                    p.BeginOutputReadLine();
                    p.BeginErrorReadLine();

                    if (!p.WaitForExit(iTimeout * 1000))
                    {
                        p.Kill();
                    }
                    return p.ExitCode == 0;
                }
                finally
                {
                    p.OutputDataReceived -= new DataReceivedEventHandler(outputHandler);
                    p.ErrorDataReceived -= new DataReceivedEventHandler(outputHandler);
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// STDOUT/STDERR Handler
        /// </summary>
        /// <param name="sendingProcess">Source process</param>
        /// <param name="outLine">Output Line</param>
        private void outputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (!String.IsNullOrEmpty(outLine.Data))
            {
                outputList.Enqueue(outLine.Data);
            }
        }

        /// <summary>
        /// Get the Checkout CMD
        /// </summary>
        /// <returns>Checkout CMD</returns>
        private string checkoutCmd()
        {
            String sCmd = "";

            // SSH Key Path Fix

            // Change to drive
            sCmd = sProjectDir.Substring(0, 1) + ":";
            // Change to directory
            sCmd += " && cd " + sProjectDir;
            // Git Reset
            sCmd += " && git reset --hard";
            // Git Checkout
            sCmd += " && git checkout " + buildInfo.sha;

            return sCmd;
        }

        /// <summary>
        /// Get the Clone CMD
        /// </summary>
        /// <returns>Clone CMD</returns>
        private string cloneCmd()
        {
            String sCmd = "";

            // Change to drive
            sCmd = sProjectDir.Substring(0, 1) + ":";
            // Change to directory
            sCmd += " && cd " + sProjectsDir;
            // Git Clone
            sCmd += " && git clone " + buildInfo.repo_url + " project-" + buildInfo.project_id;
            // Change to directory
            sCmd += " && cd " + sProjectDir;
            // Git Checkout
            sCmd += " && git checkout " + buildInfo.sha;

            return sCmd;
        }

        /// <summary>
        /// Get the Fetch CMD
        /// </summary>
        /// <returns>Fetch CMD</returns>
        private string fetchCmd()
        {
            String sCmd = "";

            // Change to drive
            sCmd = sProjectDir.Substring(0, 1) + ":";
            // Change to directory
            sCmd += " && cd " + sProjectDir;
            // Git Reset
            sCmd += " && git reset --hard";
            // Git Clean
            sCmd += " && git clean -f";
            // Git fetch
            sCmd += " && git fetch";

            return sCmd;
        }

        /// <summary>
        /// Delete non empty directory tree
        /// </summary>
        private void DeleteDirectory(string target_dir)
        {
            string[] files = Directory.GetFiles(target_dir);
            string[] dirs = Directory.GetDirectories(target_dir);

            foreach (string file in files)
            {
                try
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                }
                catch (PathTooLongException)
                {
                    LongPathFile.Delete(file);
                }
            }

            foreach (string dir in dirs)
            {
                // Only recurse into "normal" directories
                if ((File.GetAttributes(dir) & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
                    try
                    {
                        Directory.Delete(dir, false);
                    }
                    catch (PathTooLongException)
                    {
                        LongPathDirectory.Delete(dir);
                    }
                else
                    DeleteDirectory(dir);
            }

            try
            {
                Directory.Delete(target_dir, false);
            }
            catch (PathTooLongException)
            {
                LongPathDirectory.Delete(target_dir);
            }
        }
    }
}
