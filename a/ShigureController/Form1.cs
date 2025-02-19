using System.Collections;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

/*
Command example:
set PumpAP57 30
move PumpAP57 out 10 200
move PumpAP57 in 10 200
pause PumpAP57 0.1
move PumpAP57 out 10 200
*/
namespace ShigureController
{
    public partial class Form1 : Form
    {
        private Dictionary<string, Pump> Pumps = new Dictionary<string, Pump>();
        private const int MaxDegreeOfParallelism = 50; 

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            SearchDevices();
        }

        private async void SearchDevices()
        {
            AppendLog("Searching pumps... please wait.\n", Color.Black);
            lstDevices.Items.Clear();
            Pumps.Clear();
            string localIP = GetLocalIPAddress();
            if (string.IsNullOrEmpty(localIP))
            {
                throw new Exception("Failed to get your IP address.");
            }
            string subnet = localIP.Substring(0, localIP.LastIndexOf('.') + 1);
            List<string> onlineDevices = new List<string>();
            List<Task> pingTasks = new List<Task>();
            SemaphoreSlim semaphore = new SemaphoreSlim(MaxDegreeOfParallelism);

            for (int i = 1; i < 255; i++)
            {
                string ip = subnet + i;
                await semaphore.WaitAsync();
                pingTasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        if (await IsDeviceOnline(ip))
                        {
                            lock (onlineDevices)
                            {
                                onlineDevices.Add(ip);
                            }
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }

            await Task.WhenAll(pingTasks);

            List<Task> checkTasks = new List<Task>();
            foreach (string ip in onlineDevices)
            {
                AppendLog("Online device detected: " + ip + "\n", Color.Black);
                checkTasks.Add(CheckDevice(ip));
            }

            await Task.WhenAll(checkTasks);
            AppendLog("Searching finished.\n", Color.Blue);
        }

        private string GetLocalIPAddress()
        {
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus == OperationalStatus.Up)
                {
                    foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip.Address))
                        {
                            return ip.Address.ToString();
                        }
                    }
                }
            }
            return null;
        }

        private async Task<bool> IsDeviceOnline(string ip)
        {
            try
            {
                Ping ping = new Ping();
                PingReply reply = await ping.SendPingAsync(ip, 200);
                return reply.Status == IPStatus.Success;
            }
            catch
            {
                return false;
            }
        }

        private async Task CheckDevice(string ip)
        {
            int maxRetries = 5;
            int delayBetweenRetries = 50; 

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    using (HttpClient client = new HttpClient())
                    {
                        client.Timeout = TimeSpan.FromSeconds(1);
                        HttpResponseMessage response = await client.GetAsync($"http://{ip}");
                        string responseBody = await response.Content.ReadAsStringAsync();
                        if (responseBody.Contains("Pump Control"))
                        {
                            var match = Regex.Match(responseBody, @"<input type=""text"" id=""name"" name=""name"" value=([^><]*)><br>");
                            if (match.Success)
                            {
                                string hostname = match.Groups[1].Value;
                                var pump = new Pump(ip, hostname);
                                Pumps[hostname] = pump;
                                AppendLog("Pump found on " + ip + "\n", Color.Green);
                                Invoke(new Action(() => lstDevices.Items.Add($"{ip} - {hostname}")));
                            }
                            return; // 成功后退出方法
                        }
                    }
                }
                catch (TaskCanceledException ex) when (ex.CancellationToken == CancellationToken.None)
                {
                    // 仅在超时时重试
                    AppendLog($"ERROR: Timeout connecting to {ip} on attempt {attempt + 1}: {ex.Message}\n", Color.Orange);
                }
                catch (Exception ex)
                {
                    // 记录其他错误日志，但不重试
                    AppendLog($"{ip} seems not a pump.\n", Color.Orange);
                    break;
                }

                // 等待一段时间后重试
                await Task.Delay(delayBetweenRetries);
            }
        }


        private void btnRefreshDevices_Click_1(object sender, EventArgs e)
        {
            SearchDevices();
        }

        private void UpdateLists()
        {
            lstPlanned.Items.Clear();
            lstProcessing.Items.Clear();
            lstDone.Items.Clear();

            var plannedEvents = new List<string>();
            var processingEvents = new List<string>();
            var doneEvents = new List<string>();

            bool hasMoreEvents;
            int index = 0;

            do
            {
                hasMoreEvents = false;
                foreach (var pump in Pumps.Values)
                {
                    if (pump.EventPlanned.Count > index)
                    {
                        plannedEvents.Add(pump.EventPlanned.ElementAt(index).ToString());
                        hasMoreEvents = true;
                    }
                    if (pump.EventProcessing.Count > index)
                    {
                        processingEvents.Add(pump.EventProcessing.ElementAt(index).ToString());
                        hasMoreEvents = true;
                    }
                    if (pump.EventDone.Count > index)
                    {
                        doneEvents.Add(pump.EventDone.ElementAt(index).ToString());
                        hasMoreEvents = true;
                    }
                }
                index++;
            } while (hasMoreEvents);

            lstPlanned.Items.AddRange(plannedEvents.ToArray());
            lstProcessing.Items.AddRange(processingEvents.ToArray());
            lstDone.Items.AddRange(doneEvents.ToArray());
        }



        private void ParseCommands()
        {
            foreach (var pump in Pumps.Values)
            {
                pump.EventPlanned.Clear();
                pump.EventProcessing.Clear();
                pump.EventDone.Clear();
            }

            string[] lines = txtCmd.Text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                string[] parts = line.Trim().Split(' ');
                if (parts.Length == 3 && parts[0].Equals("set", StringComparison.OrdinalIgnoreCase))
                {
                    string key = parts[1];
                    if (Pumps.ContainsKey(key) && double.TryParse(parts[2], out double volume))
                    {
                        Pumps[key].SyringeType = volume % 1 == 0 ? $"{(int)volume}" : $"{volume:F1}";
                    }
                    else
                    {
                        AppendLog($"Invalid command: {line}\n", Color.Red);
                    }
                }
                else if (parts.Length == 5 && parts[0].Equals("move", StringComparison.OrdinalIgnoreCase))
                {
                    string key = parts[1];
                    if (Pumps.ContainsKey(key) && Enum.TryParse(parts[2], true, out EventType eventType) && int.TryParse(parts[3], out int volume) && int.TryParse(parts[4], out int speed))
                    {
                        Pumps[key].AddMotionEvent(eventType, volume, speed);
                    }
                    else
                    {
                        AppendLog($"Invalid command: {line}\n", Color.Red);
                    }
                }
                else if (parts.Length == 3 && parts[0].Equals("pause", StringComparison.OrdinalIgnoreCase))
                {
                    string key = parts[1];
                    if (Pumps.ContainsKey(key) && double.TryParse(parts[2], out double time))
                    {
                        Pumps[key].AddPauseEvent(time);
                    }
                    else
                    {
                        AppendLog($"Invalid command: {line}\n", Color.Red);
                    }
                }
                else
                {
                    AppendLog($"Invalid command: {line}\n", Color.Red);
                }
            }
        }

        private void AppendLog(string text, Color color)
        {
            txtLogs.SelectionStart = txtLogs.TextLength;
            txtLogs.SelectionLength = 0;
            txtLogs.SelectionColor = color;
            txtLogs.AppendText(text);
            txtLogs.SelectionColor = txtLogs.ForeColor;
        }


        private void btnParse_Click(object sender, EventArgs e)
        {
            ParseCommands();
            UpdateLists();
        }

        private void btnClearLog_Click(object sender, EventArgs e)
        {
            txtLogs.Text = "";
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            timer.Start();
        }

        private bool AllDone()
        {
            foreach (var pump in Pumps.Values)
            {
                if (pump.EventPlanned.Count > 0 || pump.EventProcessing.Count > 0)
                {
                    return false;
                }
            }
            return true;
        }

        private async void timer_Tick(object sender, EventArgs e)
        {
            foreach (var pump in Pumps.Values)
            {
                try
                {
                    await pump.Check();
                    UpdateLists();
                    if (AllDone())
                    {
                        timer.Stop();
                        AppendLog("All events done. \n", Color.Green);
                    }

                }
                catch (Exception ex)
                {
                    AppendLog("ERROR: " + ex.Message + "\n", Color.Red);
                }

            }
        }

        private async Task StopPumpAsync(Pump pump)
        {
            try
            {
                await pump.Stop();
                AppendLog($"Pump {pump.Name} stopped successfully.\n", Color.Green);
            }
            catch (Exception ex)
            {
                AppendLog($"ERROR: Failed to stop pump {pump.Name}: {ex.Message}\n", Color.Red);
            }
        }
        private async void btnStop_Click(object sender, EventArgs e)
        {
            timer.Stop();
            foreach(var pump in Pumps)
            {
                pump.Value.EventPlanned.Clear();
                pump.Value.EventProcessing.Clear();
                pump.Value.EventDone.Clear();
            }
            var stopTasks = new List<Task>();
            foreach (var pump in Pumps.Values)
            {
                stopTasks.Add(StopPumpAsync(pump));
            }
            await Task.WhenAll(stopTasks);
            UpdateLists();
        }
    }
}
