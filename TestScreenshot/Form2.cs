using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using EasyHook;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Remoting;
using System.Runtime.InteropServices;
using System.IO;
using Capture.Interface;
using Capture.Hook;
using Capture;


namespace TestScreenshot
{
    public partial class Form2 : Form
    {
        const int MAX_ATTEMPTS = 1;

        class ProcStatus
        {
            public IntPtr Hwnd;
            public int ThreadId;
            public int ProcessId;
            public bool Hooked;
            public int AttemptCount;
            public Process Process;
            public CaptureProcess CaptureProcess;
            public string Name;

            public ProcStatus()
            {

            }

        }
        Dictionary<IntPtr, ProcStatus> windowInfo = new Dictionary<IntPtr, ProcStatus>();
        Dictionary<int, ProcStatus> hookedProcesses = new Dictionary<int, ProcStatus>();
        HashSet<string> Blacklist = GetBlackList();

        private static HashSet<string> GetBlackList()
        {
            var set = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            set.Add("devenv");
            set.Add("firefox");
            set.Add("palemoon");
            set.Add("nvcplui");
            set.Add("chrome");
            set.Add("skype");
            //set.Add("freecell");
            //set.Add("hearts");
            //set.Add("spidersolitaire");
            //set.Add("solitaire");
            //set.Add("minesweeper");
            return set;
        }

        public Form2()
        {
            InitializeComponent();
            this.Visible = false;
        }

        private void pollingTimer_Tick(object sender, EventArgs e)
        {
            DoPolling();
        }

        private void DoPolling()
        {
            var foregroundWindow = NativeMethods.GetForegroundWindow();
            ProcStatus procStatus;

            if (foregroundWindow == IntPtr.Zero)
            {
                return;
            }

            if (windowInfo.ContainsKey(foregroundWindow))
            {
                procStatus = windowInfo[foregroundWindow];
            }
            else
            {
                int threadId, processId;
                threadId = NativeMethods.GetWindowThreadProcessId(foregroundWindow, out processId);

                if (hookedProcesses.ContainsKey(processId))
                {
                    procStatus = hookedProcesses[processId];
                }
                else
                {
                    procStatus = new TestScreenshot.Form2.ProcStatus();
                    procStatus.Hwnd = foregroundWindow;
                    procStatus.ThreadId = threadId;
                    procStatus.ProcessId = processId;
                    procStatus.Process = Process.GetProcessById(processId);
                    procStatus.Name = procStatus.Process.ProcessName;
                    windowInfo[foregroundWindow] = procStatus;

                    PrintStatus("Found process " + procStatus.Name);
                }
            }

            if (procStatus.Hooked)
            {
                return;
            }

            if (procStatus.AttemptCount >= MAX_ATTEMPTS)
            {
                return;
            }

            bool containsD3d9 = ContainsModule(procStatus.Process, "d3d9.dll");
            bool containsMsCoreE = ContainsModule(procStatus.Process, "mscoree.dll");
            if (containsMsCoreE)
            {
                containsMsCoreE = IsDotNetAssembly(procStatus);
            }

            //List<IntPtr> Modules = new List<IntPtr>();



            //attempt hooking
            //is d3d9.dll loaded?
            if (containsD3d9 && !containsMsCoreE)
            {
                if (!Blacklist.Contains(procStatus.Name))
                {
                    try
                    {
                        if (HookProcess(procStatus))
                        {
                            procStatus.Hooked = true;
                        }
                    }
                    catch (ProcessAlreadyHookedException ex)
                    {
                        procStatus.Hooked = true;
                    }
                }
            }
            procStatus.AttemptCount++;
            if (procStatus.AttemptCount >= MAX_ATTEMPTS)
            {
                if (!containsD3d9)
                {
                    PrintStatus("Process Rejected: " + procStatus.Name + " does not use Direct3D 9.");
                }
                else if (containsMsCoreE)
                {
                    PrintStatus("Process Rejected: " + procStatus.Name + " is a .NET application.");
                }
                else if (Blacklist.Contains(procStatus.Name))
                {
                    PrintStatus("Process Rejected: " + procStatus.Name + " is blacklisted.");
                }
            }
        }

        private static bool IsDotNetAssembly(ProcStatus procStatus)
        {
            bool isDotNet;
            try
            {
                System.Reflection.AssemblyName.GetAssemblyName(procStatus.Process.MainModule.FileName);
                isDotNet = true;
            }
            catch
            {
                isDotNet = false;
            }

            return isDotNet;
        }

        private void PrintStatus(string line)
        {
            if (!this.Visible)
            {
                return;
            }

            bool scrolledToBottom = textBox1.SelectionStart == textBox1.Text.Length;
            if (this.textBox1.Text != "")
            {
                this.textBox1.Text += "\r\n";
            }
            this.textBox1.Text += line;
            if (scrolledToBottom)
            {
                textBox1.SelectionStart = textBox1.Text.Length;
                textBox1.ScrollToCaret();
            }
        }

        private bool ContainsModule(Process process, string moduleName)
        {
            IntPtr processHandle = IntPtr.Zero;
            try
            {
                processHandle = process.Handle;
            }
            catch
            {
                return false;
            }

            //var modules = NativeMethods.GetModules(pid);
            //foreach (var module in modules)
            //{
            //    if (Path.GetFileName(module.szModule).Equals(moduleName, StringComparison.InvariantCultureIgnoreCase))
            //    {
            //        return true;
            //    }
            //}
            //return false;

            var list = NativeMethods.EnumProcessModulesEx(processHandle);
            StringBuilder sb = new StringBuilder(1024);
            foreach (var module in list)
            {
                NativeMethods.GetModuleFileNameEx(processHandle, module, sb, sb.Capacity);
                string fileName = sb.ToString();
                if (Path.GetFileName(fileName).Equals(moduleName, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private bool HookProcess(ProcStatus procStatus)
        {
            if (procStatus.ProcessId == 0)
            {
                return false;
            }

            Direct3DVersion direct3DVersion = Direct3DVersion.AutoDetect;
            CaptureConfig cc = new CaptureConfig()
            {
                Direct3DVersion = direct3DVersion,
                ShowOverlay = showFPSToolStripMenuItem.Checked
            };

            FileUnblocker.Unblock("EasyHook32Svc.exe");
            FileUnblocker.Unblock("EasyHook64Svc.exe");

            var captureInterface = new CaptureInterface();
            captureInterface.RemoteMessage += CaptureInterface_RemoteMessage;
            procStatus.CaptureProcess = new CaptureProcess(procStatus.Process, cc, captureInterface);
            PrintStatus("* Hooked " + procStatus.Name);
            hookedProcesses.Add(procStatus.ProcessId,procStatus);
            AddExitHook(procStatus);
            return true;
        }

        private void AddExitHook(ProcStatus procStatus)
        {
            var handler = new EventHandler((sender, e) =>
            {
                this.hookedProcesses.Remove(procStatus.ProcessId);
            });
            procStatus.Process.Exited += handler;
        }

        private void CaptureInterface_RemoteMessage(MessageReceivedEventArgs message)
        {
            PrintStatus(message.Message);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            this.showFPSToolStripMenuItem.Checked = RegistryUtility.GetSetting("ShowFPS", this.showFPSToolStripMenuItem.Checked);
        }

        private void Form2_Shown(object sender, EventArgs e)
        {
            this.Visible = false;
        }

        private void Form2_FormClosed(object sender, FormClosedEventArgs e)
        {
            foreach (var item in this.hookedProcesses.Values.ToArray())
            {
                if (item.CaptureProcess != null)
                {
                    if (!item.CaptureProcess.Process.HasExited)
                    {
                        item.CaptureProcess.CaptureInterface.Disconnect();
                    }
                }
            }
            RegistryUtility.SaveSetting("ShowFPS", this.showFPSToolStripMenuItem.Checked);
        }

        private void showFPSToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool showingFps = showFPSToolStripMenuItem.Checked;
            showingFps = !showingFps;
            ShowFPSChanged(showingFps);
        }

        private void ShowFPSChanged(bool showingFps)
        {
            showFPSToolStripMenuItem.Checked = showingFps;
            foreach (var proc in this.hookedProcesses.Values.ToArray())
            {
                if (proc.CaptureProcess != null && !proc.CaptureProcess.Process.HasExited)
                {
                    proc.CaptureProcess.CaptureInterface.Message(MessageType.Information, "ShowFPS=" + (showingFps ? 1 : 0).ToString());
                }
            }

        }
    }

    public static class FileUnblocker
    {
        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteFile(string name);

        public static bool Unblock(string fileName)
        {
            return DeleteFile(fileName + ":Zone.Identifier");
        }
    }
}
