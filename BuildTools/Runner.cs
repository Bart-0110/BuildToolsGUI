using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.IO;
using System.Threading;
using Ionic.Zip;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;

namespace BuildTools
{
    public class Runner
    {
        private readonly BuildTools _form;
        private readonly string gitDir = "PortableGit";
        private JObject _api;
        private JObject _json;

        private readonly List<Process> processes = new List<Process>();
        private readonly List<WebClient> clients = new List<WebClient>(); 

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
            if (File.Exists((string) _json["buildTools"]["name"]))
            {
                int number = (int) _api["number"];
                int currentBuildNumber;
                bool success = GetBuildNumberFromJar(out currentBuildNumber);

                if (success)
                {
                    bool update = currentBuildNumber < number;
                    if (update)
                        _form.AppendText("--- BuildTools is out of date");
                    
                    return update;
                }
            }
            else
            {
                _form.AppendText("--- BuildTools does not exist");
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
                _form.AppendText("--- Retrieving information from server");

                _json = DownloadJson("http://demonwav.com/buildtools.json");

                if (_json == null)
                {
                    _form.AppendText("--- Error retrieving data, canceling");
                    return false;
                }
            }
            if (_api == null)
            {
                _api = DownloadJson((string) _json["buildTools"]["api"]);

                if (_api == null)
                {
                    _form.AppendText("--- Error retrieving data, canceling");
                    return false;
                }
            }
            return true;
        }

        private JObject DownloadJson(string url)
        {
            WebRequest request = WebRequest.Create(url);
            Stream stream = request.GetResponse().GetResponseStream();
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
            _form.AppendText("--- Checking downloaded BuildTools");
            try
            {
                using (ZipFile zip = ZipFile.Read((string) _json["buildTools"]["name"]))
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
                                    _form.AppendText("--- Jar is invalid");
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
                _form.AppendText("--- Jar is invalid");
                i = 0;
                return false;
            }
        }

        /// <summary>
        /// Download a new copy of BuildTools.jar
        /// </summary>
        public void UpdateJar()
        {
            if (!GetJson())
            {
                return;
            }

            if (File.Exists((string) _json["buildTools"]["name"]))
            {
                _form.AppendText("--- Deleting current BuildTools");
                File.Delete((string) _json["buildTools"]["name"]);
            }

            string baseUrl = (string) _api["url"];
            string relativePath = (string) _api["artifacts"][0]["relativePath"];
            string fullUrl = baseUrl + "artifact/" + relativePath;

            _form.AppendText("--- Downloading BuildTools");
            DownloadFile(fullUrl, (string) _json["buildTools"]["name"]);
            
        }

        /// <summary>
        /// Goes through the process of checking for and updating BuildTools.
        /// </summary>
        public void RunUpdate()
        {
            _form.AppendText("--- Checking for update");
            _form.ProgressShow();
            _form.ProgressIndeterminate();
            bool update = CheckUpdate();
            if (update)
            {
                UpdateJar();
                _form.AppendText("--- Download complete");
            }
            else
            {
                _form.AppendText("--- BuildTools is up to date, no need to update");
            }
        }

        /// <summary>
        /// Goes through the process of checking the BuildTools environment and running it. If autoUpate is
        /// true, it will first call <see cref="RunUpdate()"/>.
        /// </summary>
        /// <param name="autoUpdate">If true, this will first call <see cref="RunUpdate()"/> Before continuing.</param>
        public void RunBuildTools(bool autoUpdate)
        {
            _form.ProgressShow();
            _form.ProgressIndeterminate();
            if (autoUpdate)
            {
                RunUpdate();
            }

            // Java check
            if (!CheckJava())
            {
                string javaFile = (string) _json["json"]["name"];
                _form.AppendText("--- Downloading Java Installer");
                if (Environment.Is64BitOperatingSystem)
                {
                    DownloadFile((string) _json["java"]["64"], javaFile);
                }
                else
                {
                    DownloadFile((string)_json["java"]["32"], javaFile);
                }
                
                
                _form.ProgressIndeterminate();
                using (Process installProcess = new Process())
                {
                    _form.AppendText("--- Running Java Installer");
                    processes.Add(installProcess);
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
                        _form.AppendText("--- There was a problem while install Java");
                        return;
                    }
                    finally
                    {
                        processes.Remove(installProcess);
                    }
                    
                }
                Thread.Sleep(100);
                File.Delete(javaFile);

                if (CheckJava())
                {
                    _form.AppendText("--- Java Installed Successfully");
                }
                else
                {
                    _form.AppendText("--- Java could not be installed, canceling");
                    return;
                }
            }

            // Git check
            if (!CheckGit())
            {
                string gitFile = "portable_git.7z.exe";
                _form.AppendText("--- Downloading Portable Git");

                if (Environment.Is64BitOperatingSystem)
                {
                    DownloadFile((string)_json["git"]["64"], gitFile);
                }
                else
                {
                    DownloadFile((string)_json["git"]["32"], gitFile);
                }

                _form.ProgressIndeterminate();
                using (Process extractProcess = new Process())
                {
                    _form.AppendText("--- Extracting Portable Git");
                    processes.Add(extractProcess);
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
                        _form.AppendText("--- There was a problem while extracting git");
                        return;
                    }
                    finally
                    {
                        processes.Remove(extractProcess);
                    }
                }
                Thread.Sleep(100);
                File.Delete(gitFile);

                if (CheckGit())
                {
                    _form.AppendText("--- Portable Git Installed Successfully");
                }
                else
                {
                    _form.AppendText("--- Portable Git could not be installed, canceling");
                    return;
                }
            }

            _form.AppendText("\n--- Running BuildTools.jar\n");
            // Run Build Tools
            using (Process buildProcess = new Process())
            {
                processes.Add(buildProcess);
                try
                {
                    buildProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    buildProcess.StartInfo.CreateNoWindow = true;
                    buildProcess.StartInfo.FileName = gitDir + "/bin/bash.exe";
                    buildProcess.StartInfo.UseShellExecute = false;
                    buildProcess.StartInfo.RedirectStandardOutput = true;
                    buildProcess.StartInfo.RedirectStandardError = true;
                    buildProcess.StartInfo.Arguments =
                        "--login -c \"git config --global --replace-all core.autocrlf true & java -jar " +
                        (string) _json["buildTools"]["name"] + "\"";
                    buildProcess.OutputDataReceived += (sender, args) => _form.AppendText(args.Data);
                    buildProcess.ErrorDataReceived += (sender, args) => _form.AppendText(args.Data);
                    buildProcess.Start();
                    buildProcess.BeginOutputReadLine();
                    buildProcess.BeginErrorReadLine();
                    buildProcess.WaitForExit();
                    if (buildProcess.ExitCode != 0)
                        throw new Exception();
                }
                catch (Exception)
                {
                    _form.AppendText("--- There was an error while running BuildTools");
                }
                finally
                {
                    processes.Remove(buildProcess);
                }
                
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>True if Java is installed correctly</returns>
        private bool CheckJava()
        {
            string path = (string) Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Environment", "Path", "");
            Environment.SetEnvironmentVariable("Path", path, EnvironmentVariableTarget.Process);

            using (Process process = new Process())
            {
                processes.Add(process);
                try
                {
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.FileName = "java";
                    process.StartInfo.UseShellExecute = false;

                    process.StartInfo.Arguments = "-version";
                    process.Start();
                    process.WaitForExit();
                    processes.Remove(process);
                    return true;
                }
                catch (Exception)
                {
                    processes.Remove(process);
                    return false;
                }
            }
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
                    processes.Add(process);
                    try
                    {
                        process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        process.StartInfo.CreateNoWindow = true;
                        process.StartInfo.FileName = gitDir + "/bin/bash.exe";
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.RedirectStandardError = true;
                        process.StartInfo.Arguments = "\"--login\" \"-i\" \"-c\" \"exit\"";
                        process.Start();
                        process.WaitForExit();
                        if (process.ExitCode != 0)
                            throw new Exception();

                        processes.Remove(process);
                        return true;
                    }
                    catch (Exception)
                    {
                        processes.Remove(process);
                        return false;
                    }
                    
                }
            }

            return false;
        }

        private void DownloadFile(string url, string dest)
        {
            using (WebClient client = new WebClient())
            {
                clients.Add(client);
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
                while (client.IsBusy)
                {
                    Thread.Sleep(50);
                }
                clients.Remove(client);
            }
        }

        public void CleanUp()
        {
            foreach (Process process in processes)
            {
                process.Kill();
                process.Dispose();
            }
            foreach (WebClient client in clients)
            {
                client.CancelAsync();
                client.Dispose();
            }
        }
    }
}
