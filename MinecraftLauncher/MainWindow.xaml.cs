using CmlLib.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CmlLib.Core.Auth;
using CmlLib.Core.ProcessBuilder;
using CmlLib.Core.VersionMetadata;
using CmlLib.Core.Version;
using System.IO;

namespace MinecraftLauncher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private CmlLib.Core.MinecraftLauncher launcher;
        private bool isMinecraftRunning = false;

        public MainWindow()
        {
            InitLauncher();

            InitializeComponent();
            //addIconFromResource();
        }

        private void addIconFromResource()
        {
            // Ensure the Resources file exists in the Properties namespace and contains the required resource.  
            // Replace 'YourIconResourceName' with the actual name of the resource in your project.  
            

            // Additional logic to use the iconData can be added here.  
        }

        private void InitLauncher()
        {
            var path = new MinecraftPath();
            launcher = new CmlLib.Core.MinecraftLauncher(path);

            launcher.ByteProgressChanged += _launcher_progress_changed;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            usernametbox.Text = Properties.Settings.Default.Username;

            if (string.IsNullOrEmpty(usernametbox.Text))
            {
                usernametbox.Text = Environment.UserName;
            }

            await ListVersions();
        }

        private void _launcher_progress_changed(object sender, ByteProgress e)
        {
            progressPb.Maximum = 100;

            progressPb.Value = (int)(e.ToRatio() * 100);

            progresslb.Content = $"{(e.ProgressedBytes / (float)e.TotalBytes) * 100:F2}%";
        }

        private async Task ListVersions()
        {
            var versions = await launcher.GetAllVersionsAsync();
            MVersionType versionType; // Explicitly declare the type to fix CS0818  

            foreach (var version in versions)
            {
                versionType = version.GetVersionType(); // Assuming 'Type' is the correct property for MVersionType  
                if (versionType == MVersionType.Release)
                {
                    versioncb.Items.Add(version.Name);
                }
            }

            if (string.IsNullOrEmpty(versioncb.Text))
            {
                versioncb.Text = versions.LatestReleaseName;
            }
        }

        private async Task InstallVersion(string version)
        {
            await launcher.InstallAsync(version);
        }

        private void SaveUsername(string username)
        {
            Properties.Settings.Default.Username = username;
            Properties.Settings.Default.Save();
        }

        private void MinecraftProcess_Exited(object sender, EventArgs e)
        {
            // Ensure the code runs on the UI thread  
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => MinecraftProcess_Exited(sender, e));
                return;
            }
            // This runs when Minecraft closes  
            button1.Content = "Launch Minecraft";
            progressPb.Value = 0;
            progresslb.Content = "0%";
            isMinecraftRunning = false;
        }

        private async Task LaunchGame()
        {
            string username = usernametbox.Text;
            string version = versioncb.Text;
            await InstallVersion(version);

            var process = await launcher.BuildProcessAsync(version, new MLaunchOption
            {
                Session = MSession.CreateOfflineSession(username),
                MaximumRamMb = 4096, // 4GB  
            });
            process.EnableRaisingEvents = true;
            process.Exited += MinecraftProcess_Exited;
            process.Start();
            isMinecraftRunning = true;

        }

        private async void button1_Click(object sender, EventArgs e)
        {
            if (!isMinecraftRunning)
            {
                button1.Content = "Launching..."; // Fixed: Use 'Content' instead of 'Text'  
                try
                {
                    await LaunchGame();
                    SaveUsername(usernametbox.Text);
                    button1.Content = "RUNNING!"; // Fixed: Use 'Content' instead of 'Text'  
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error launching game: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("Minecraft is already running.");
            }
        }
    }
}
