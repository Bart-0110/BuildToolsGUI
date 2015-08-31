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
        private readonly BuildTools form;
        private readonly string fileName = "core.jar";
        private readonly string apiUrl = "https://hub.spigotmc.org/jenkins/job/BuildTools/lastSuccessfulBuild/api/json";
        private JObject _json;

        public Runner(BuildTools form)
        {
            this.form = form;
        }

        /// <summary>
        /// Checks if a BuildTools.jar exists, and if it exists, which version it is. It then checks the api on jenkins to see if
        /// there is a newer version available. If there is, or if a BuildTools.jar does not exist, this method will return true.
        /// Returns false if no update is needed.
        /// </summary>
        /// <returns>True if an update is needed.</returns>
        public bool CheckUpdate()
        {
            if (File.Exists(fileName))
            {
                GetJson();

                int number = (int) _json["number"];
                int currentBuildNumber;
                bool success = GetBuildNumberFromJar(out currentBuildNumber);

                if (success)
                {
                    bool update = currentBuildNumber < number;
                    if (update)
                        form.AppendText("--- BuildTools is out of date");
                    
                    return update;
                }
            }
            else
            {
                form.AppendText("--- BuildTools does not exist");
            }
            return true;
        }

        /// <summary>
        /// Get the JSON response from the jenkins server and save it as a JObject.
        /// </summary>
        private void GetJson()
        {
            if (_json == null)
            {
                form.AppendText("--- Retrieving information from server");
                WebRequest request = WebRequest.Create(apiUrl);
                Stream stream = request.GetResponse().GetResponseStream();
                if (stream != null)
                {
                    StreamReader reader = new StreamReader(stream);

                    string line = "";
                    string finalOutput = "";
                    while ((line = reader.ReadLine()) != null)
                    {
                        finalOutput += line + "\n";
                    }

                    _json = JObject.Parse(finalOutput);
                }
            }
        }

        /// <summary>
        /// Check the build number of the BuildTools.jar. Set the given parameter to the build number of the BuildTools.jar.
        /// Returns whether the BuildTools.jar is valid. If this method returns false, the parameter means nothing (and will be 0).
        /// </summary>
        /// <param name="i">The build number of the jar</param>
        /// <returns>Whether the jar file is valid.</returns>
        private bool GetBuildNumberFromJar(out int i)
        {
            form.AppendText("--- Checking downloaded BuildTools");
            try
            {
                using (ZipFile zip = ZipFile.Read(fileName))
                {
                    ZipEntry entry = zip["META-INF/MANIFEST.MF"];
                    using (MemoryStream stream = new MemoryStream())
                    {
                        entry.Extract(stream);
                        stream.Position = 0;

                        StreamReader reader = new StreamReader(stream);

                        string line = "";
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (line.StartsWith("Implementation-Version: "))
                            {
                                string version = line.Replace("Implementation-Version: ", "");
                                string[] split = version.Split('-');

                                // at least put some effort into making sure this is a BuildTools jar
                                if (!"git".Equals(split[0]) || !"BuildTools".Equals(split[1]))
                                {
                                    form.AppendText("--- Jar is invalid");
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
                form.AppendText("--- Jar is invalid");
                i = 0;
                return false;
            }
        }

        /// <summary>
        /// Download a new copy of BuildTools.jar
        /// </summary>
        public void UpdateJar()
        {
            if (File.Exists(fileName))
            {
                form.AppendText("--- Deleting current BuildTools");
                File.Delete(fileName);
            }

            GetJson();

            string baseUrl = (string) _json["url"];
            string relativePath = (string) _json["artifacts"][0]["relativePath"];
            string fullUrl = baseUrl + "artifact/" + relativePath;

            using (WebClient client = new WebClient())
            {
                form.AppendText("--- Downloading BuildTools");
                client.DownloadFile(fullUrl, fileName);
            }
        }

        /// <summary>
        /// Goes through the process of checking for and updating BuildTools.
        /// </summary>
        public void RunUpdate()
        {
            form.AppendText("--- Checking for update");
            bool update = CheckUpdate();
            if (update)
            {
                UpdateJar();
                form.AppendText("--- Download complete");
            }
            else
            {
                form.AppendText("--- BuildTools is up to date, no need to update");
            }
        }

        /// <summary>
        /// Goes through the process of checking the BuildTools environment and running it. If autoUpate is
        /// true, it will first call <see cref="RunUpdate()"/>.
        /// </summary>
        /// <param name="autoUpdate">If true, this will first call <see cref="RunUpdate()"/> Before continuing.</param>
        public void RunBuildTools(bool autoUpdate)
        {
            if (autoUpdate)
            {
                RunUpdate();
            }

            // Java check
            if (!CheckJava())
            {
                string javaFile = "latest_java.exe";
                using (WebClient client = new WebClient())
                {
                    form.AppendText("--- Downloading Java Installer");
                    
                    client.DownloadFile("http://javadl.sun.com/webapps/download/AutoDL?BundleId=109715", javaFile);
                }
                using (Process installProcess = new Process())
                {
                    form.AppendText("--- Running Java Installer");
                    installProcess.StartInfo.FileName = javaFile;
                    installProcess.Start();
                    installProcess.WaitForExit();
                }
                Thread.Sleep(100);
                File.Delete(javaFile);

                if (CheckJava())
                {
                    form.AppendText("--- Java Installed Successfully");
                }
                else
                {
                    form.AppendText("--- Java could not be installed, canceling");
                    return;
                }
            }

            // Git check

        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>True if Java is installed correctly</returns>
        private bool CheckJava()
        {
            using (Process process = new Process())
            {
                try
                {
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.FileName = "java";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.Arguments = "-version";
                    process.Start();
                    process.WaitForExit();
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }
    }
}
