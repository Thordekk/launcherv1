using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.IO;
using System.Net;
using System.ComponentModel;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace ArcheLauncher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            try { LoadXMLConfigFileOnline(); } catch(Exception ex) { MessageBox.Show(ex.ToString()); }

            LoadSiteNews();

            textBoxUser.Text = Properties.Settings.Default.archewow_username;
        }

        private string Config_SiteUrl = string.Empty;
        private string Config_NodeType = string.Empty;
        private string Config_NodeName = string.Empty;
        private string Config_PatchFilename = string.Empty;
        private string Config_PatchVersion = string.Empty;
        private string Config_VersionFilename = string.Empty;
        private string Config_PatchUrl = string.Empty;
        private string Config_Realmlist = string.Empty;

        public void LoadXMLConfigFileOnline()
        {
            string url = @"http://localhost/archelauncher.xml";
  
            var doc = XDocument.Load(url);

            foreach (var unit in doc.Descendants("launcher"))
            {

                Config_SiteUrl = unit.Element("site_address").Value;
                Config_NodeType = unit.Element("node_type").Value;
                Config_NodeName = unit.Element("node_name").Value;
                Config_PatchFilename = unit.Element("patch_filename").Value;
                Config_PatchVersion = unit.Element("patch_version").Value;
                Config_VersionFilename = unit.Element("version_filename").Value;
                Config_PatchUrl = unit.Element("patch_url").Value;
                Config_Realmlist = unit.Element("realmlist").Value;
            }

            doc = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        public void LoadSiteNews()
        {
            List<String> news = new List<String>();
            List<String> newsLinks = new List<String>();

            var html = "https://na.archerage.to/";

            HtmlWeb web = new HtmlWeb();

            var htmlDoc = web.Load(html);

            int newcount = 0;

            foreach (HtmlNode node in htmlDoc.DocumentNode.SelectNodes("//*[@class='testimonials-name']/a"))
            {
                news.Add(node.InnerText);
                newsLinks.Add(node.GetAttributeValue("href", ""));
            }

            foreach (string inew in news)
            {
                Hyperlink hyperlink = new Hyperlink();
                Run run = new Run { Text = "\n   " + inew };
                hyperlink.NavigateUri = new Uri(newsLinks[newcount]);
                hyperlink.RequestNavigate += Hyperlink_RequestNavigate;
                hyperlink.Inlines.Add(run);
                textBlockNews.Inlines.Add(hyperlink);
                newcount += 1;
            }
        }

        WebClient webClient;               // Our WebClient that will be doing the downloading for us
        Stopwatch sw = new Stopwatch();    // The stopwatch which we will be using to calculate the download speed

        public void DownloadFile(string urlAddress, string location)
        {
            using (webClient = new WebClient())
            {
                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(Completed);
                webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(ProgressChanged);

                // The variable that will be holding the url address (making sure it starts with http://)
                Uri URL = urlAddress.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ? new Uri(urlAddress) : new Uri("http://" + urlAddress);

                // Start the stopwatch which we will be using to calculate the download speed
                sw.Start();

                try
                {
                    // Start downloading the file
                    webClient.DownloadFileAsync(URL, location);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        // The event that will fire whenever the progress of the WebClient is changed
        private void ProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            // Calculate download speed and output it to labelSpeed.
            labelSpeed.Content = string.Format("{0} kb/s", (e.BytesReceived / 1024d / sw.Elapsed.TotalSeconds).ToString("0.00"));

            // Update the progressbar percentage only when the value is not the same.
            pb2.Value = e.ProgressPercentage;

            // Show the percentage on our label.
            labelPerc.Content = e.ProgressPercentage.ToString() + "%";

            // Update the label with how much data have been downloaded so far and the total size of the file we are currently downloading
            labelDownloaded.Content = string.Format("{0} MB's / {1} MB's",
                (e.BytesReceived / 1024d / 1024d).ToString("0.00"),
                (e.TotalBytesToReceive / 1024d / 1024d).ToString("0.00"));
        }

        // The event that will trigger when the WebClient is completed
        private void Completed(object sender, AsyncCompletedEventArgs e)
        {
            // Reset the stopwatch.
            sw.Reset();

            if (e.Cancelled == true)
            {
                labelDownloaded.Content = "Download has been canceled.";
            }
            else
            {
                labelDownloaded.Content = "Download completed.";
                updateButton.IsEnabled = true;
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void Image_MouseUp(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void Image_MouseUp_1(object sender, MouseButtonEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            using (var outputFile = new StreamWriter(@"WTF\Config.WTF", true))
                outputFile.WriteLine("set realmlist " + Config_Realmlist);

            //SET accountName
            if (chb1.IsChecked == true)
            {
                using (var outputFile = new StreamWriter(@"WTF\Config.WTF", true))
                    outputFile.WriteLine("set accountName \"" + textBoxUser.Text + "\"");
            }
            else
            {
                using (var outputFile = new StreamWriter(@"WTF\Config.WTF", true))
                    outputFile.WriteLine("set accountName \"\"");
            }

            Properties.Settings.Default.archewow_username = textBoxUser.Text;
            Properties.Settings.Default.Save();

            if (chb2.IsChecked == true)
            {
                if (Directory.Exists("Cache"))
                {
                    var dir = new DirectoryInfo("Cache");
                    dir.Delete(true); // true => recursive delete
                }
            }

            try
            {
                if (!File.Exists("Wow.exe"))
                {
                    MessageBox.Show("The WoW.exe file could not be located!", "Something went wrong!");
                    return;
                }
                else
                {
                    string programPath = Directory.GetCurrentDirectory() + "\\Wow.exe";
                    Process proc = new Process();
                    proc.StartInfo.FileName = programPath;
                    proc.Start();
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        public bool AddedMissingFiles()
        {
            bool missedFiles = false;

            string path = @"Data\" + Config_VersionFilename;
            if (!File.Exists(@"Data\" + Config_VersionFilename))
            {
                File.Create(path).Dispose();

                using (TextWriter tw = new StreamWriter(path))
                {
                    tw.WriteLine(Config_PatchVersion);
                }

                missedFiles = true;
            }

            string path2 = @"Data\" + Config_PatchFilename;
            if (!File.Exists(path2))
            {
                string DOWNLOADFILE = Config_PatchUrl;
                string filePath = @"Data\" + Config_PatchFilename;

                DownloadFile(DOWNLOADFILE, filePath);

                missedFiles = true;
            }

            return missedFiles;
        }

        public bool RequirePatchUpdate()
        {
            bool requiredUpdate = true;

            string playerPatchVersion = System.IO.File.ReadAllText(@"Data\" + Config_VersionFilename);
            string officialPatchVersion = Config_PatchVersion;
            
            if (Regex.Replace(playerPatchVersion, @"\t|\n|\r", "") == officialPatchVersion)
                requiredUpdate = false;

            return requiredUpdate;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                if (AddedMissingFiles())
                    MessageBox.Show("Fixed missing files! Your patch is up to date.");
                else
                {
                    if (RequirePatchUpdate())
                    {
                        string path = @"Data\" + Config_PatchFilename;
                        if (File.Exists(path))
                        {
                            File.Delete(path);
                        }

                        DownloadFile(Config_PatchUrl, @"Data\" + Config_PatchFilename);

                        string path2 = @"Data\" + Config_VersionFilename;
                        if (File.Exists(path2))
                        {
                            File.Delete(path2);
                            File.Create(path2).Dispose();

                            using (TextWriter tw = new StreamWriter(path2))
                            {
                                tw.WriteLine(Config_PatchVersion);
                            }
                        }

                        MessageBox.Show("Successfully updated! Your patch is up to date.");
                    }
                    else
                        MessageBox.Show("Your patch is up to date!");
                }

            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            //updateButton.IsEnabled = false;
        }
    }
}
