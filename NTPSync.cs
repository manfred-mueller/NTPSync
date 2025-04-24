using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using System.Reflection;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace NTPSync
{
    public partial class NTPSync : Form
    {
        public Icon appIcon = Properties.Resources.logo;
        private NotifyIcon notifyIcon;
        private ContextMenuStrip contextMenuStrip;
        private ImageList menuImageList;
        private ToolStripLabel titleMenuLabel;
        public string pubTime;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetSystemTime(ref SYSTEMTIME st);

        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEMTIME
        {
            public ushort Year;
            public ushort Month;
            public ushort DayOfWeek;
            public ushort Day;
            public ushort Hour;
            public ushort Minute;
            public ushort Second;
            public ushort Milliseconds;
        }
        public NTPSync()
        {
            // Check if there is a working network connection
            if (!CheckNetworkConnection())
            {
                // Display an error message with an error icon and exit the application
                MessageBox.Show(Properties.Resources.NoWorkingNetworkConnectionExitingApplication, Properties.Resources.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
                return; // Exit the constructor
            }
            else
            {
                InitializeComponent();

                this.ShowInTaskbar = false;
                this.WindowState = FormWindowState.Minimized;
                this.Visible = false;
                this.Hide(); // Hide the form
                this.Load += NTPSync_Load; // Subscribe to the Load event
            }
        }
        public static Bitmap PngFromIcon(Icon icon)
        {
            Bitmap png = null;
            using (var iconStream = new MemoryStream())
            {
                icon.Save(iconStream);
                var decoder = new IconBitmapDecoder(iconStream,
                    BitmapCreateOptions.PreservePixelFormat,
                    BitmapCacheOption.None);

                using (var pngSteam = new MemoryStream())
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(decoder.Frames[0]);
                    encoder.Save(pngSteam);
                    png = (Bitmap)Image.FromStream(pngSteam);
                }
            }
            return png;
        }

        private bool CheckNetworkConnection()
        {
            try
            {
                using (var ping = new Ping())
                {
                    PingReply reply = ping.Send("8.8.8.8", 2000); // Ping Google's DNS server
                    return reply.Status == IPStatus.Success;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }


        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                // Minimize the form instead of closing
                e.Cancel = true;
                this.WindowState = FormWindowState.Minimized;
                this.Hide();
            }
        }

        private async void InitializeApp()
        {
            // Call the asynchronous initialization method
            await InitializeAsync();
        }

        public async Task InitializeAsync()
        {

            // Create and configure the NotifyIcon FIRST
            notifyIcon = new NotifyIcon
            {
                Icon = appIcon,
                Visible = true
            };

            // Get the public Network Time
            pubTime = await GetNetworkTimeAsync();
            Version shortVersion = Assembly.GetExecutingAssembly().GetName().Version;

            notifyIcon.MouseClick += NotifyIcon_MouseClick;

            // Create and configure the ContextMenuStrip
            contextMenuStrip = new ContextMenuStrip();
            ToolStripMenuItem resetSystemTime = new ToolStripMenuItem(Properties.Resources.ResyncSystemTime);
            titleMenuLabel = new ToolStripLabel(string.Format(System.Reflection.Assembly.GetExecutingAssembly().GetName().Name + $" {shortVersion.Major}.{shortVersion.Minor}.{shortVersion.Build}"));
            ToolStripMenuItem exitMenuItem = new ToolStripMenuItem(Properties.Resources.Exit);

            // Load the icons into the ImageList
            menuImageList = new ImageList();
            menuImageList.Images.Add(Properties.Resources.exit_icon);

            // Set ImageList for the context menu
            contextMenuStrip.ImageList = menuImageList;

            // Set ImageIndex for menu items
            exitMenuItem.ImageIndex = 0;

            resetSystemTime.Click += ResetTime_Click;
            exitMenuItem.Click += ExitMenuItem_Click;

            contextMenuStrip.Items.Add(titleMenuLabel);
            contextMenuStrip.Items.Add(resetSystemTime);
            contextMenuStrip.Items.Add(exitMenuItem);

            notifyIcon.ContextMenuStrip = contextMenuStrip;
        }

        private async void NotifyIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // Zeit synchronisieren
                pubTime = await GetNetworkTimeAsync();
            }
        }
        private async void ResetTime_Click(object sender, EventArgs e)
        {
            // Zeit synchronisieren
            pubTime = await GetNetworkTimeAsync();
        }
        private void ExitButton_Click(object sender, EventArgs e)
        {
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
            Application.Exit();
        }

        private async Task<string> GetNetworkTimeAsync()
        {
            string ntpServer = Properties.Settings.Default.ntpServer;
            const int ntpPort = 123;
            const int ntpDataLength = 48;
            const byte ntpVersion = 3 << 3;

            try
            {
                byte[] ntpData = new byte[ntpDataLength];
                ntpData[0] = ntpVersion | 3; // LI, Version, Mode

                using (var udpClient = new UdpClient())
                {
                    udpClient.Connect(ntpServer, ntpPort);
                    await udpClient.SendAsync(ntpData, ntpData.Length);

                    var result = await udpClient.ReceiveAsync();
                    byte[] receivedData = result.Buffer;

                    const byte offsetTransmitTime = 40;
                    ulong intPart = BitConverter.ToUInt32(receivedData.Skip(offsetTransmitTime).Take(4).Reverse().ToArray(), 0);
                    ulong fracPart = BitConverter.ToUInt32(receivedData.Skip(offsetTransmitTime + 4).Take(4).Reverse().ToArray(), 0);

                    ulong milliseconds = (intPart * 1000) + ((fracPart * 1000) / 0x100000000L);
                    var networkDateTimeUtc = new DateTime(1900, 1, 1).AddMilliseconds((long)milliseconds);

                    // SYSTEMTIME erwartet UTC-Zeit
                    DateTime networkUtc = networkDateTimeUtc;

                    SYSTEMTIME st = new SYSTEMTIME
                    {
                        Year = (ushort)networkUtc.Year,
                        Month = (ushort)networkUtc.Month,
                        Day = (ushort)networkUtc.Day,
                        DayOfWeek = (ushort)networkUtc.DayOfWeek,
                        Hour = (ushort)networkUtc.Hour,
                        Minute = (ushort)networkUtc.Minute,
                        Second = (ushort)networkUtc.Second,
                        Milliseconds = (ushort)networkUtc.Millisecond
                    };

                    if (!SetSystemTime(ref st))
                    {
                        throw new InvalidOperationException(Properties.Resources.FailedToSetSystemTimeYouMightNeedAdministrativePrivileges);
                    } else
                    {
                        ShowNotificationToast("None", String.Format(Properties.Resources.SystemTimeSetTo, networkUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss")), 5);
                        return networkUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss");
                    }
                }
            }
            catch (Exception ex)
            {
                HandleError(ex);
                return String.Format(Properties.Resources.Error01,ex.GetType().Name, ex.Message);
            }
        }

        private void ExitMenuItem_Click(object sender, EventArgs e)
        {
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
            Application.Exit();
        }
        private void ShowNotificationToast(string iconType, string message, int seconds)
        {
            ToolTipIcon tooltipIcon;

            switch (iconType.ToLower())
            {
                case "info":
                    tooltipIcon = ToolTipIcon.Info;
                    break;
                case "warning":
                    tooltipIcon = ToolTipIcon.Warning;
                    break;
                case "error":
                    tooltipIcon = ToolTipIcon.Error;
                    break;
                default:
                    tooltipIcon = ToolTipIcon.None;
                    break;
            }
            notifyIcon.BalloonTipIcon = tooltipIcon;
            notifyIcon.BalloonTipText = message;
            notifyIcon.ShowBalloonTip(seconds * 1000);
        }
        private void HandleError(Exception ex)
        {
            ShowNotificationToast(Properties.Resources.Error, ex.Message, 5);
        }
        private void NTPSync_Load(object sender, EventArgs e)
        {
            this.Hide(); // Sicherheitshalber nochmals verstecken
            InitializeApp(); // NotifyIcon usw. initialisieren
        }
    }
}
