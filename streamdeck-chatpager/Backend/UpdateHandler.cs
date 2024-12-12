using BarRaider.SdTools;
using Microsoft.Win32.SafeHandles;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Management;
using System.Net.Http;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Permissions;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ChatPager.Backend
{
    internal class UpdateHandler : IUpdateHandler // V2
    {
        #region Private Members

        private const string UPDATE_REQUIRED_IMAGE  = @"data:image/png;base64,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";
        private const string DEVICE_SUPPORTED_IMAGE = @"data:image/png;base64,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";
        private const string PLUGIN_UPDATE_API_URI = "https://api.barraider.com/v3/CheckVersion";
        private readonly int[] PLUGIN_STREAM = new int[]{ 67, 111, 114, 115, 97, 105, 114, 32, 77, 101, 109, 111, 114, 121, 44, 32, 73, 110, 99, 46 };
        private const int AUTO_UPDATE_COOLDOWN_MINUTES = 360;
        private const int VERSION_CHECK_COOLDOWN_SEC = 3600;
        private readonly Random rand = new Random();
        private readonly System.Timers.Timer tmrPeriodicUpdateCheck = new System.Timers.Timer();
        private DateTime lastVersionCheck = DateTime.MinValue;
        private string pluginName;
        private string pluginVersion;
        private string mname;
        private bool shownURL = false;

        #endregion

        #region Public Methods

        public UpdateHandler()
        {
            tmrPeriodicUpdateCheck.Elapsed += TmrPeriodicUpdateCheck_Elapsed;
            tmrPeriodicUpdateCheck.Interval = TimeSpan.FromMinutes(AUTO_UPDATE_COOLDOWN_MINUTES).TotalMilliseconds;
            tmrPeriodicUpdateCheck.Start();
        }

        public bool IsBlockingUpdate { get; private set; } = false;

        public event EventHandler<PluginUpdateInfo> OnUpdateStatusChanged;

        public void SetPluginConfiguration(string pluginName, string pluginVersion)
        {
            this.pluginName = pluginName;
            this.pluginVersion = pluginVersion;
        }


        public void CheckForUpdate()
        {
            Task.Run(async () =>
            {
                // Add delay to reduce checking on plugin startup
                int delay = rand.Next(7, 12) * 1000;
                Thread.Sleep(delay);
                ValidateParent();
                await PerformUpdateCheck();
            });
        }

        public void SetGlobalSettings(object settings)
        {
        }

        public void Dispose()
        {
            tmrPeriodicUpdateCheck.Stop();
        }

        #endregion

        #region Private Methods

        private async Task PerformUpdateCheck()
        {
            // Don't check if already requires update
            if (IsBlockingUpdate)
            {
                return;
            }

            if ((DateTime.Now - lastVersionCheck).TotalSeconds < VERSION_CHECK_COOLDOWN_SEC)
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"{this.GetType()} *UC* in cooldown.");
                return;
            }

            try
            {
                lastVersionCheck = DateTime.Now;

                using HttpClient client = new HttpClient
                {
                    Timeout = new TimeSpan(0, 0, 15)
                };

                Dictionary<string, string> dic = new Dictionary<string, string>
                {
                    ["plugin"] = pluginName,
                    ["version"] = pluginVersion,
                    ["clientId"] = getClientId(),
                    ["requestDate"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    ["mname"] = mname
                };

                var content = new StringContent(JsonConvert.SerializeObject(dic), Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync(PLUGIN_UPDATE_API_URI, content);
                var result = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    Logger.Instance.LogMessage(TracingLevel.INFO, $"{this.GetType()} *UC* Error: {response.StatusCode} Reason: {response.ReasonPhrase} Body: {result}");
                    return;
                }


                Logger.Instance.LogMessage(TracingLevel.INFO, $"[{Thread.CurrentThread.ManagedThreadId}] *UC* complete");
                var updateResponse = JsonConvert.DeserializeObject<UpdateCheckResponse>(result);
                HandleUpdateResponse(updateResponse);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"[{Thread.CurrentThread.ManagedThreadId}] *UC* Exception: {ex}");
                return;
            }
        }

        private static string identifier(string wmiClass, string wmiProperty, string wmiMustBeTrue)
        {
            string result = "";
            System.Management.ManagementClass mc = new System.Management.ManagementClass(wmiClass);
            System.Management.ManagementObjectCollection moc = mc.GetInstances();
            foreach (System.Management.ManagementObject mo in moc)
            {
                if (mo[wmiMustBeTrue].ToString() == "True")
                {
                    //Only get the first one
                    if (result == "")
                    {
                        try
                        {
                            result = mo[wmiProperty].ToString();
                            break;
                        }
                        catch
                        {
                        }
                    }
                }
            }
            return result;
        }
        //Return a hardware identifier
        private static string identifier(string wmiClass, string wmiProperty)
        {
            string result = "";
            System.Management.ManagementClass mc = new System.Management.ManagementClass(wmiClass);
            System.Management.ManagementObjectCollection moc = mc.GetInstances();
            foreach (System.Management.ManagementObject mo in moc)
            {
                //Only get the first one
                if (result == "")
                {
                    try
                    {
                        result = mo[wmiProperty].ToString();
                        break;
                    }
                    catch
                    {
                    }
                }
            }
            return result;
        }

        private string getClientId()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(System.Security.Principal.WindowsIdentity.GetCurrent().Name + "|");
            sb.Append(identifier("Win32_Processor", "ProcessorId"));

            var byteData = Encoding.UTF8.GetBytes(sb.ToString());
            var hashData = SHA256.Create().ComputeHash(byteData);
            StringBuilder resultBuilder = new StringBuilder();

            for (int i = 0; i < hashData.Length; i++)
            {
                resultBuilder.Append(hashData[i].ToString("x2"));
            }
            return resultBuilder.ToString();
        }

        private int GetParentProcessIdEx()
        {
            try
            {
                var myId = Process.GetCurrentProcess().Id;
                PROCESSENTRY32 pe32 = new PROCESSENTRY32 { };
                pe32.dwSize = (uint)Marshal.SizeOf(typeof(PROCESSENTRY32));
                using (var hSnapshot = CreateToolhelp32Snapshot(SnapshotFlags.Process, (uint)myId))
                {
                    if (hSnapshot.IsInvalid)
                        return 0;

                    if (!Process32First(hSnapshot, ref pe32))
                    {
                        return 0;
                    }
                    do
                    {
                        if (pe32.th32ProcessID == (uint)myId)
                            return (int)pe32.th32ParentProcessID;
                    } while (Process32Next(hSnapshot, ref pe32));
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        #region ParentProcessIdEx

        private const int ERROR_NO_MORE_FILES = 0x12;
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern SafeSnapshotHandle CreateToolhelp32Snapshot(SnapshotFlags flags, uint id);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool Process32First(SafeSnapshotHandle hSnapshot, ref PROCESSENTRY32 lppe);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool Process32Next(SafeSnapshotHandle hSnapshot, ref PROCESSENTRY32 lppe);

        [Flags]
        private enum SnapshotFlags : uint
        {
            HeapList = 0x00000001,
            Process = 0x00000002,
            Thread = 0x00000004,
            Module = 0x00000008,
            Module32 = 0x00000010,
            All = (HeapList | Process | Thread | Module),
            Inherit = 0x80000000,
            NoHeaps = 0x40000000
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESSENTRY32
        {
            public uint dwSize;
            public uint cntUsage;
            public uint th32ProcessID;
            public IntPtr th32DefaultHeapID;
            public uint th32ModuleID;
            public uint cntThreads;
            public uint th32ParentProcessID;
            public int pcPriClassBase;
            public uint dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string szExeFile;
        };
        [SuppressUnmanagedCodeSecurity, HostProtection(SecurityAction.LinkDemand, MayLeakOnAbort = true)]
        internal sealed class SafeSnapshotHandle : SafeHandleMinusOneIsInvalid
        {
            internal SafeSnapshotHandle() : base(true)
            {
            }

            [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
            internal SafeSnapshotHandle(IntPtr handle) : base(true)
            {
                base.SetHandle(handle);
            }

            protected override bool ReleaseHandle()
            {
                return CloseHandle(base.handle);
            }

            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success), DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
            private static extern bool CloseHandle(IntPtr handle);
        }

        #endregion


        private void ValidateParent()
        {
            try
            {
                int pId = GetParentProcessIdEx();
                if (pId <= 0)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"[{Thread.CurrentThread.ManagedThreadId}] Unknown Elgato Stream Deck Caller");
                    ForceVersionFail();
                }

                Process parent = Process.GetProcessById(pId);
                if (parent == null)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"[{Thread.CurrentThread.ManagedThreadId}] Unknown Elgato Stream Deck Parent");
                    ForceVersionFail();
                }

                mname = parent.MainModule.ModuleName.ToLowerInvariant();
                if (mname != "streamdeck.exe")
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"[{Thread.CurrentThread.ManagedThreadId}] Unidentified Elgato Stream Deck device");
                    ForceVersionFail();
                }

                if (!VerifyCertificate(parent.MainModule.FileName))
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"[{Thread.CurrentThread.ManagedThreadId}] Unrecognized Elgato Stream Deck device");
                    ForceVersionFail();
                }

                Logger.Instance.LogMessage(TracingLevel.INFO, $"[{Thread.CurrentThread.ManagedThreadId}] Plugin initializing");
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"[{Thread.CurrentThread.ManagedThreadId}] GENERAL EXCEPTION: Could not validate parent! {ex}");
                ForceVersionFail();
            }
        }

        private bool VerifyCertificate(string filename)
        {
            try
            {
                var theSigner = X509Certificate.CreateFromSignedFile(filename);
                var theCertificate = new X509Certificate2(theSigner);

                // ensure it's my application's cerfificate
                var subject = theCertificate.Subject;
                if (String.IsNullOrEmpty(subject))
                {
                    return false;
                }

                var match = Regex.Match(subject, @"CN=""([^""]+)""");
                if (!match.Success)
                {
                    return false;
                }
                // Extract the CN value
                string cnValue = match.Groups[1].Value;

                if (string.IsNullOrEmpty(cnValue) || cnValue.Length != PLUGIN_STREAM.Length)
                {
                    return false;
                }

                for (int idx = 0; idx < cnValue.Length; idx++)
                {
                    if (cnValue[idx] != PLUGIN_STREAM[idx])
                    {
                        return false;
                    }
                }

                if (!theCertificate.Verify())
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        private void ForceVersionFail()
        {
            if (string.IsNullOrEmpty(mname))
            {
                mname = "zx.exe";
            }

            if (OnUpdateStatusChanged != null)
            {
                OnUpdateStatusChanged?.Invoke(this, new PluginUpdateInfo(PluginUpdateStatus.CriticalUpgrade, null, DEVICE_SUPPORTED_IMAGE));
                IsBlockingUpdate = true;
            }
        }

        private void TmrPeriodicUpdateCheck_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            CheckForUpdate();
        }

        private void HandleUpdateResponse(UpdateCheckResponse response)
        {
            string url = null;
            string image = null;

            if (response == null)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"[{Thread.CurrentThread.ManagedThreadId}] *UC* Response is null!");
                return;
            }

            if (response.Status == PluginUpdateStatus.UpToDate)
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"[{Thread.CurrentThread.ManagedThreadId}] Plugin version is up to date");
                return;
            }


            if (response.Status == PluginUpdateStatus.Unknown)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"[{Thread.CurrentThread.ManagedThreadId}] *UC* Response status: {response.Status}");
                return;
            }

            if (response.Status == PluginUpdateStatus.MajorUpgrade || response.Status == PluginUpdateStatus.CriticalUpgrade)
            {
                shownURL = false;
                url = response.UpdateURL;
                image = UPDATE_REQUIRED_IMAGE;
                IsBlockingUpdate = true;
            }
            else if (!shownURL && !String.IsNullOrEmpty(response.UpdateURL))
            {
                shownURL = true; // Only show once when the plugin loads
                url = response.UpdateURL;
            }

            Logger.Instance.LogMessage(TracingLevel.WARN, $"Plugin update is required");
            OnUpdateStatusChanged?.Invoke(this, new PluginUpdateInfo(response.Status, url, image));
        }

        private class UpdateCheckResponse
        {
            /// <summary>
            /// Status
            /// </summary>
            [JsonProperty("status")]
            public PluginUpdateStatus Status { get; private set; }

            /// <summary>
            /// Update URL
            /// </summary>
            [JsonProperty("updateURL")]
            public string UpdateURL { get; private set; }
        }

        #endregion
    }
}
