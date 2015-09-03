using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.IO;
using System.Threading;
using Ionic.Zip;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System.Management;
using System.Security.Permissions;

namespace BuildTools
{
    public class Runner
    {
        private readonly string dir = "BuildTools\\";
        private readonly BuildTools _form;
        private readonly string gitDir = "BuildTools/PortableGit";
        private JObject _api;
        private JObject _json;

        private readonly List<IDisposable> disposables = new List<IDisposable>();

        public Runner(BuildTools form)
        {
            _form = form;
        }

        /// <summary>
        /// Checks if a BuildTools.jar exists, and if it exists, which version it is. It then checks the api on jenkins to see if
        /// there is a newer version available. If there is, or if a BuildTools.jar does not exist, this method will return true.
        /// Returns false if no update is needed.
        /// </summary>
        /// <returns>True if an update is needed.</returns>
        public bool CheckUpdate()
        {
            if (!GetJson())
            {
                return false;
            }
            if (File.Exists(dir + (string) _json["buildTools"]["name"]))
            {
                int number = (int) _api["number"];
                int currentBuildNumber;
                bool success = GetBuildNumberFromJar(out currentBuildNumber);

                if (success)
                {
                    bool update = currentBuildNumber < number;
                    if (update)
                        _form.AppendText("BuildTools is out of date");
                    
                    return update;
                }
            }
            else
            {
                _form.AppendText("BuildTools does not exist");
            }
            return true;
        }

        /// <summary>
        /// Get the JSON response from the jenkins server and save it as a JObject.
        /// </summary>
        private bool GetJson()
        {
            if (_json == null)
            {
                _form.AppendText("Retrieving information from the server");

                _json = DownloadJson("http://demonwav.com/buildtools.json");

                if (_json == null)
                {
                    _form.AppendText("Error retrieving data, canceling");
                    return false;
                }
            }
            if (_api == null)
            {
                _api = DownloadJson((string) _json["buildTools"]["api"]);

                if (_api == null)
                {
                    _form.AppendText("Error retrieving data, canceling");
                    return false;
                }
            }
            return true;
        }

        private JObject DownloadJson(string url)
        {
            WebRequest request = WebRequest.Create(url);
            using (Stream stream = request.GetResponse().GetResponseStream())
            {
                try
                {
                    disposables.Add(stream);
                    if (stream != null)
                    {
                        StreamReader reader = new StreamReader(stream);

                        string line;
                        string finalOutput = "";
                        while ((line = reader.ReadLine()) != null)
                        {
                            finalOutput += line + "\n";
                        }

                        return JObject.Parse(finalOutput);
                    }
                }
                catch (Exception)
                {
                    _form.AppendText("There was an error while trying to receive data from the server");
                    return null;
                }
                finally
                {
                    disposables.Remove(stream);
                }
                
            }
            return null;
        }

        /// <summary>
        /// Check the build number of the BuildTools.jar. Set the given parameter to the build number of the BuildTools.jar.
        /// Returns whether the BuildTools.jar is valid. If this method returns false, the parameter means nothing (and will be 0).
        /// </summary>
        /// <param name="i">The build number of the jar</param>
        /// <returns>Whether the jar file is valid.</returns>
        private bool GetBuildNumberFromJar(out int i)
        {
            _form.AppendText("Checking downloaded BuildTools");
            try
            {
                using (ZipFile zip = ZipFile.Read(dir + (string) _json["buildTools"]["name"]))
                {
                    ZipEntry entry = zip["META-INF/MANIFEST.MF"];
                    using (MemoryStream stream = new MemoryStream())
                    {
                        entry.Extract(stream);
                        stream.Position = 0;

                        StreamReader reader = new StreamReader(stream);

                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (line.StartsWith("Implementation-Version: "))
                            {
                                string version = line.Replace("Implementation-Version: ", "");
                                string[] split = version.Split('-');

                                // at least put some effort into making sure this is a BuildTools jar
                                if (!"git".Equals(split[0]) || !"BuildTools".Equals(split[1]))
                                {
                                    _form.AppendText("Jar is invalid");
                                    i = 0;
                                    return false;
                                }

                                i = int.Parse(split[3]);
                                return true;
                            }
                        }
                    }
                    i = 0;
                    return false;
                }
            }
            catch (Exception)
            {
                _form.AppendText("Jar is invalid");
                i = 0;
                return false;
            }
        }

        /// <summary>
        /// Download a new copy of BuildTools.jar
        /// </summary>
        public bool UpdateJar()
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            _form.AppendText("Checking for update");
            _form.ProgressShow();
            _form.ProgressIndeterminate();
            bool update = CheckUpdate();
            if (update)
            {
                _form.AppendText("Update needed for BuildTools");
                if (!GetJson())
                {
                    return false;
                }

                if (File.Exists(dir + (string) _json["buildTools"]["name"]))
                {
                    _form.AppendText("Deleting current BuildTools");
                    File.Delete(dir + (string) _json["buildTools"]["name"]);
                }

                string baseUrl = (string) _api["url"];
                string relativePath = (string) _api["artifacts"][0]["relativePath"];
                string fullUrl = baseUrl + "artifact/" + relativePath;

                _form.AppendText("Downloading BuildTools");
                if (!DownloadFile(fullUrl, dir + (string) _json["buildTools"]["name"]))
                {
                    _form.AppendText("BuildTools failed to download");
                    return false;
                }
                _form.AppendText("Download complete");
            }
            else
            {
                _form.AppendText("BuildTools is up to date, no need to update");
            }
            return true;
        }

        /// <summary>
        /// Goes through the process of checking the BuildTools environment and running it. If autoUpate is
        /// true, it will first call <see cref="RunUpdate()"/>.
        /// </summary>
        /// <param name="autoUpdate">If true, this will first call <see cref="RunUpdate()"/> Before continuing.</param>
        public void RunBuildTools(bool autoUpdate)
        {
            if (!Environment.Is64BitOperatingSystem)
            {
                _form.AppendText("BuildTools does not reliably work on 32 bit systems, if you have problems please re-run this on a 64 bit system.");
            }

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            _form.ProgressShow();
            _form.ProgressIndeterminate();
            if (autoUpdate)
            {
                if (!UpdateJar())
                    return;
            }

            // Java check
            bool javaInstalled;
            if (!CheckJava(out javaInstalled))
            {
                string javaFile = dir + (string) _json["java"]["name"];
                if (!javaInstalled)
                {
                    // Java is not installed
                    _form.AppendText("Downloading Java installer");

                    bool success = DownloadJava(javaFile);
                    if (!success)
                    {
                        _form.AppendText("Java could not be downloaded, canceling");
                    }

                    _form.ProgressIndeterminate();
                    success = InstallJava(javaFile);
                    if (!success)
                    {
                        _form.AppendText("Java could not be installed, canceling");
                    }

                    if (CheckJava())
                    {
                        _form.AppendText("Java installed successfully");
                    }
                    else
                    {
                        _form.AppendText("Java could not be installed, canceling");
                        return;
                    }
                }
                else
                {
                    // Java is installed
                    _form.AppendText("This is a 64 bit operating system, but the 32 bit JRE is installed. Downloading 64 bit JRE");
                    bool success = DownloadJava(javaFile);
                    if (!success)
                    {
                        _form.AppendText("Unable to download the 64 bit Java installer, will continue with the 32 bit JRE. This may cause the build to fail");
                    }
                    else
                    {
                        _form.AppendText("Uninstalling current 32 bit JRE");
                        success = UninstallJava();
                        if (!success)
                        {
                            _form.AppendText("There was an error while attempting to uninstall the 32 bit JRE");
                            CheckJava(out javaInstalled);
                            if (javaInstalled)
                            {
                                _form.AppendText("Java still seems to be installed, though. Will continue with the 32 bit JRE. This may cause the build to fail");
                            }
                            else
                            {
                                _form.AppendText("In spite of the error, it seems Java has been uninstalled. Will now install the 64 bit JRE");
                                success = InstallJava(javaFile);
                                if (!success)
                                {
                                    _form.AppendText("Java failed to install, canceling");
                                    return;
                                }
                                if (!FullJavaCheck())
                                {
                                    return;
                                }
                            }
                        }
                        else
                        {
                            _form.AppendText("Installing the 64 bit JRE");
                            success = InstallJava(javaFile);
                            if (!success)
                            {
                                _form.AppendText("Java failed to install, canceling");
                                return;
                            }
                            if (!FullJavaCheck())
                            {
                                return;
                            }
                        }
                    }

                }
            }

            // Git check
            if (!CheckGit())
            {
                string gitFile = dir + "portable_git.7z.exe";
                _form.AppendText("Downloading portable Git");

                bool success;
                if (Environment.Is64BitOperatingSystem)
                {
                    success = DownloadFile((string)_json["git"]["64"], gitFile);
                }
                else
                {
                    success = DownloadFile((string)_json["git"]["32"], gitFile);
                }
                if (!success)
                {
                    _form.AppendText("Portable Git could not be downloaded, canceling");
                    return;
                }

                _form.ProgressIndeterminate();
                using (Process extractProcess = new Process())
                {
                    _form.AppendText("Extracting portable Git");
                    disposables.Add(extractProcess);
                    try
                    {
                        extractProcess.StartInfo.FileName = gitFile;
                        extractProcess.StartInfo.UseShellExecute = true;
                        extractProcess.StartInfo.Arguments = "-gm1 -nr -y";
                        extractProcess.Start();
                        extractProcess.WaitForExit();
                        if (extractProcess.ExitCode != 0)
                            throw new Exception();
                    }
                    catch (Exception)
                    {
                        _form.AppendText("Portable Git could not be installed, canceling");
                        return;
                    }
                    finally
                    {
                        disposables.Remove(extractProcess);
                    }
                }
                Thread.Sleep(100);
                File.Delete(gitFile);

                _form.AppendText("Checking portable Git installation (this may take a while)");
                if (CheckGit())
                {
                    _form.AppendText("Portable Git installed Successfully");
                }
                else
                {
                    _form.AppendText("Portable Git could not be installed, canceling");
                    return;
                }
            }

            _form.AppendRawText("");
            _form.AppendText("Running BuildTools.jar\n");
            // Run Build Tools
            using (Process buildProcess = new Process())
            {
                disposables.Add(buildProcess);
                try
                {
                    buildProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    buildProcess.StartInfo.CreateNoWindow = true;
                    buildProcess.StartInfo.FileName = gitDir + "/bin/bash.exe";
                    buildProcess.StartInfo.UseShellExecute = false;
                    buildProcess.StartInfo.RedirectStandardOutput = true;
                    buildProcess.StartInfo.RedirectStandardError = true;
                    buildProcess.StartInfo.WorkingDirectory = Path.GetFullPath(dir);
                    buildProcess.StartInfo.Arguments =
                        "--login -c \"git config --global --replace-all core.autocrlf true & java -jar " +
                        (string) _json["buildTools"]["name"] + "\"";
                    buildProcess.OutputDataReceived += (sender, args) => _form.AppendRawText(args.Data);
                    buildProcess.ErrorDataReceived += (sender, args) => _form.AppendRawText(args.Data);
                    buildProcess.Start();
                    buildProcess.BeginOutputReadLine();
                    buildProcess.BeginErrorReadLine();
                    buildProcess.WaitForExit();
                    if (buildProcess.ExitCode != 0)
                        throw new Exception();
                }
                catch (Exception)
                {
                    _form.AppendText("There was an error while running BuildTools");
                }
                finally
                {
                    disposables.Remove(buildProcess);
                }
                
            }
        }

        private bool UninstallJava()
        {
            string file = dir + (string) _json["java"]["uninstaller"]["name"];
            // We can't elevate the priviledges of this process to uninstall java, so create a new process to do so
            bool success = DownloadFile((string) _json["java"]["uninstaller"]["url"], file);
            _form.ProgressIndeterminate();
            if (!success)
            {
                return false;
            }

            using (Process process = new Process())
            {
                disposables.Add(process);
                try
                {
                    process.StartInfo.FileName = file;
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                        throw new Exception();
                }
                catch (Exception)
                {
                    return false;
                }
                finally
                {
                    disposables.Remove(process);
                }

            }
            Thread.Sleep(100);
            File.Delete(file);
            return true;
        }

        private bool CheckJava()
        {
            bool what;
            return CheckJava(out what);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>True if Java is installed correctly</returns>
        private bool CheckJava(out bool javaInstalled)
        {
            string path = (string) Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Environment", "Path", "");
            Environment.SetEnvironmentVariable("Path", path, EnvironmentVariableTarget.Process);

            using (Process process = new Process())
            {
                disposables.Add(process);
                try
                {
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.FileName = "java";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.Arguments = "-version";
                    process.Start();
                    process.WaitForExit();

                    javaInstalled = true;

                    // Make sure a 64 bit JVM is installed on a 64 bit system
                    if (Environment.Is64BitOperatingSystem)
                    {
                        process.StartInfo.Arguments = "-d64 -version";
                        process.Start();
                        process.WaitForExit();

                        if (process.ExitCode == 0)
                        {
                            return true;
                        }
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                catch (Exception)
                {
                    javaInstalled = false;
                    return false;
                }
                finally
                {
                    disposables.Remove(process);
                }
            }
        }

        private bool DownloadJava(string javaFile)
        {
            bool success;
            if (Environment.Is64BitOperatingSystem)
            {
                success = DownloadFile((string)_json["java"]["64"], javaFile);
            }
            else
            {
                success = DownloadFile((string)_json["java"]["32"], javaFile);
            }
            return success;
        }

        private bool InstallJava(string javaFile)
        {
            using (Process installProcess = new Process())
            {
                _form.AppendText("Running Java installer");
                disposables.Add(installProcess);
                try
                {
                    installProcess.StartInfo.FileName = javaFile;
                    installProcess.Start();
                    installProcess.WaitForExit();

                    if (installProcess.ExitCode != 0)
                        throw new Exception();
                }
                catch (Exception)
                {
                    return false;
                }
                finally
                {
                    disposables.Remove(installProcess);
                }

            }
            Thread.Sleep(100);
            File.Delete(javaFile);
            return true;
        }

        private bool FullJavaCheck()
        {
            bool javaInstalled;
            bool correct = CheckJava(out javaInstalled);
            if (correct)
            {
                _form.AppendText("Java installed successfully");
                return true;
            }
            if (javaInstalled)
            {
                _form.AppendText("Java was installed, but it is still 32 bit for some reason.");
                _form.AppendText("Will continue with the 32 bit JRE. This may cause the build to fail");
                return true;
            }
            _form.AppendText("Java failed to install, canceling");
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>True if portable git is installed correctly</returns>
        private bool CheckGit()
        {
            if (Directory.Exists(gitDir))
            {
                using (Process process = new Process())
                {
                    disposables.Add(process);
                    try
                    {
                        process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        process.StartInfo.CreateNoWindow = true;
                        process.StartInfo.FileName = gitDir + "/bin/bash.exe";
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.RedirectStandardError = true;
                        process.StartInfo.Arguments = "\"--login\" \"-c\" \"exit\"";
                        process.Start();
                        process.WaitForExit();
                        if (process.ExitCode != 0)
                            throw new Exception();

                        disposables.Remove(process);
                        return true;
                    }
                    catch (Exception)
                    {
                        disposables.Remove(process);
                        return false;
                    }
                    
                }
            }

            return false;
        }

        private bool DownloadFile(string url, string dest)
        {
            using (WebClient client = new WebClient())
            {
                disposables.Add(client);
                try
                {
                    client.DownloadProgressChanged += (sender, e) =>
                    {
                        double bytesIn = e.BytesReceived;
                        double totalBytes = e.TotalBytesToReceive;
                        if (totalBytes < 0)
                        {
                            _form.ProgressIndeterminate();
                        }
                        else
                        {
                            _form.Progress((int) bytesIn, (int) totalBytes);
                        }
                    };
                    client.DownloadFileAsync(new Uri(url), dest);
                    // Make this thread wait for the download to finish
                    while (client.IsBusy)
                    {
                        Thread.Sleep(50);
                    }
                }
                catch (Exception)
                {
                    return false;
                }
                finally
                {
                    disposables.Remove(client);
                }
            }
            return true;
        }

        public void CleanUp()
        {
            foreach (IDisposable disposable in disposables)
            {
                if (disposable is Process)
                {
                    Process process = (Process) disposable;
                    process.Kill();
                }
                else if (disposable is WebClient)
                {
                    WebClient client = (WebClient) disposable;
                    client.CancelAsync();
                }
                disposable.Dispose();
            }
        }
    }
}
