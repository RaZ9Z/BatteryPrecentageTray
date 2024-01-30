using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using System.Windows.Forms;
using System.Drawing;
using Application = System.Windows.Forms.Application;
using System.Management;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Runtime.CompilerServices;

namespace BatteryPrecentageTray
{
    
    internal class Program
    {
        private static NotifyIcon notifyIcon = new NotifyIcon();
        private static Timer timer = new Timer();
        private static Startup startup = new Startup();
        private static ToolStripMenuItem startUpButton;
        private static ToolStripMenuItem startUpStatus;


        public class PowerStatusListener
        {
            // Define an event that will be triggered when the charging status changes
            public event EventHandler ChargingStatusChanged;

            // Constructor to subscribe to the PowerStatusChanged event
            public PowerStatusListener()
            {
                SystemEvents.PowerModeChanged += OnPowerModeChanged;
            }

            // Event handler for the PowerModeChanged event
            private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
            {
                if (e.Mode == PowerModes.StatusChange)
                {
                    // Raise the event when the charging status changes
                    OnChargingStatusChanged();
                }
            }

            // Method to raise the ChargingStatusChanged event
            protected virtual void OnChargingStatusChanged()
            {
                ChargingStatusChanged?.Invoke(this, EventArgs.Empty);
            }

            // Method to get the current charging status
            public bool IsLaptopCharging()
            {
                PowerStatus powerStatus = SystemInformation.PowerStatus;
                return powerStatus.PowerLineStatus == PowerLineStatus.Online;
            }
        }


        [STAThread]
        static void Main()
        {  
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
            PowerStatusListener powerStatusListener = new PowerStatusListener();
            powerStatusListener.ChargingStatusChanged += OnChargingStatusChanged;
  
            notifyIcon = new NotifyIcon
            {
                Icon = CreateIcon(GetBatteryPercentage()),
                Visible = true,
                Text = "Battery Percentage App",
            };

            ContextMenuStrip menu = new ContextMenuStrip();

            startUpButton = new ToolStripMenuItem("Enable Run On Startup");
            if (!startup.IsInStartup())
            {
                startUpButton.Click += RunOnStartup;
            }
            else
            {
                startUpButton.Text = "Disable Run On Startup";
                startUpButton.Click += DisableStartup;
            }
            startUpStatus = new ToolStripMenuItem("Running On Startup: " + startup.IsInStartup());
            menu.Items.Add(startUpStatus);
            menu.Items.Add(startUpButton);
            menu.Items.Add("Exit", null, CloseApp);
            notifyIcon.ContextMenuStrip = menu;
            
            Timer timer = new Timer
            {
                Interval = 60000, // Set the interval in milliseconds
                Enabled = true
            };

            timer.Tick += (sender, e) =>
            {
                string batteryPercentage = GetBatteryPercentage();
                notifyIcon.Text = $"Battery Percentage: {batteryPercentage}%";
                notifyIcon.Icon = CreateIcon(batteryPercentage);
                
            };

            
            Application.Run();
        }

        private static void DisableStartup(object sender, EventArgs e)
        {
            startup.RemoveFromStartup();
            startUpButton.Text = "Enable Run On Startup";
            startUpButton.Click += RunOnStartup;
            startUpStatus.Text = "Running On Startup: " + startup.IsInStartup();
        }

        private static void RunOnStartup(object sender, EventArgs e)
        {
            startup.RunOnStartup();
            startUpButton.Text = "Disable Run On Startup";
            startUpButton.Click += DisableStartup;
            startUpStatus.Text = "Running On Startup: " + startup.IsInStartup();

        }

        private static void CloseApp(object sender, EventArgs e)
        {
            Application.Exit();
        }

        
        // Event handler for the ChargingStatusChanged event
        static void OnChargingStatusChanged(object sender, EventArgs e)
        {
            string batteryPercentage = GetBatteryPercentage();
            notifyIcon.Text = $"Battery Percentage: {batteryPercentage}%";
            notifyIcon.Icon = CreateIcon(batteryPercentage);
        }
        private static Icon CreateIcon(string batteryPercentage)
        {
            int percentage = int.TryParse(batteryPercentage, out int result) ? result : 0;
            percentage = Math.Max(0, Math.Min(100, percentage)); // Ensure percentage is within 0-100 range
            PowerStatus powerStatus = SystemInformation.PowerStatus;
            bool isCharging = powerStatus.PowerLineStatus == PowerLineStatus.Online;
            
            using (Bitmap bitmap = new Bitmap(128, 128))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                string text = percentage.ToString();

                // Set a larger font size and use higher DPI
                Font font = new Font("Arial", 76, FontStyle.Bold, GraphicsUnit.Point);
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                Brush color = Brushes.White;
                if (isCharging)
                {
                    font = new Font("Arial", 76, FontStyle.Bold, GraphicsUnit.Point);
                    color = Brushes.Green;
                }
                if (int.Parse(text) == 100)
                {
                    font = new Font("Arial", 62, FontStyle.Bold, GraphicsUnit.Point);
                    color = Brushes.Green;
                }
                // Calculate the position to center the text within the icon
                SizeF textSize = g.MeasureString(text, font);
                float x = (bitmap.Width - textSize.Width) / 2;
                float y = (bitmap.Height - textSize.Height) / 2;

                // Draw the battery percentage on the icon with high contrast forecolor and transparent background
                g.Clear(Color.Transparent);
                g.DrawString(text, font,color , new PointF(x, y));

                // Convert the Bitmap to an Icon
                return Icon.FromHandle(bitmap.GetHicon());
            }
        }

        private static string GetBatteryPercentage()
        {
            ObjectQuery query = new ObjectQuery("Select * From Win32_Battery");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
            ManagementObjectCollection collection = searcher.Get();

            foreach (ManagementObject mo in collection)
            {
                return mo["EstimatedChargeRemaining"].ToString();
            }

            return "N/A";
        }
    }
    

  
}


