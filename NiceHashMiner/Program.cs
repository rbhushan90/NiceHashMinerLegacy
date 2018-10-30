using Newtonsoft.Json;
using NiceHashMiner.Configs;
using NiceHashMiner.Forms;
using NiceHashMiner.PInvoke;
using NiceHashMiner.Utils;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using NiceHashMiner.Stats;

namespace NiceHashMiner
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] argv)
        {
            // Set working directory to exe
            var pathSet = false;
            var path = Path.GetDirectoryName(Application.ExecutablePath);
            if (path != null)
            {
                Environment.CurrentDirectory = path;
                pathSet = true;
            }

            // Add common folder to path for launched processes
            var pathVar = Environment.GetEnvironmentVariable("PATH");
            pathVar += ";" + Path.Combine(Environment.CurrentDirectory, "common");
            Environment.SetEnvironmentVariable("PATH", pathVar);


            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            //Console.OutputEncoding = System.Text.Encoding.Unicode;
            // #0 set this first so data parsing will work correctly
            Globals.JsonSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                Culture = CultureInfo.InvariantCulture
            };

            // #1 first initialize config
            ConfigManager.InitializeConfig();

            // #2 check if multiple instances are allowed
            var startProgram = true;
            if (ConfigManager.GeneralConfig.AllowMultipleInstances == false)
            {
                try
                {
                    var current = Process.GetCurrentProcess();
                    foreach (var process in Process.GetProcessesByName(current.ProcessName))
                    {
                        if (process.Id != current.Id)
                        {
                            startProgram = false;
                        }
                    }
                }
                catch { }
            }

            if (startProgram)
            {
                if (ConfigManager.GeneralConfig.LogToFile)
                {
                    Logger.ConfigureWithFile();
                }

                if (ConfigManager.GeneralConfig.DebugConsole)
                {
                    PInvokeHelpers.AllocConsole();
                }
                //**
                if (Configs.ConfigManager.GeneralConfig.ForkFixVersion < 4)
                {
                    Helpers.ConsolePrint("NICEHASH", "Old version");
                    if (File.Exists("internals\\MinerOptionPackage_glg.json"))
                        File.Delete("internals\\MinerOptionPackage_glg.json");

                    if (File.Exists("internals\\MinerOptionPackage_ClaymoreDual.json"))
                        File.Delete("internals\\MinerOptionPackage_ClaymoreDual.json");

                    if (File.Exists("bin\\ccminer_klaust\\ccminer.exe"))
                        File.Delete("bin\\ccminer_klaust\\ccminer.exe");
                    ConfigManager.GeneralConfig.ForkFixVersion = 4;
                }

                if (Configs.ConfigManager.GeneralConfig.ForkFixVersion < 4.1)
                {
                    Helpers.ConsolePrint("NICEHASH", "Old version");
                    if (File.Exists("internals\\MinerOptionPackage_ClaymoreDual.json"))
                        File.Delete("internals\\MinerOptionPackage_ClaymoreDual.json");

                    ConfigManager.GeneralConfig.ForkFixVersion = 4.1;
                }
                if (Configs.ConfigManager.GeneralConfig.ForkFixVersion < 5)
                {
                    Helpers.ConsolePrint("NICEHASH", "Old version");
                    if (File.Exists("bin\\xmrig\\xmrig.exe"))
                        File.Delete("bin\\xmrig\\xmrig.exe");

                    ConfigManager.GeneralConfig.ForkFixVersion = 5;
                }
                if (Configs.ConfigManager.GeneralConfig.ForkFixVersion < 6)
                {
                    Helpers.ConsolePrint("NICEHASH", "Old version");
                    if (File.Exists("bin\\xmrig\\xmrig.exe"))
                        File.Delete("bin\\xmrig\\xmrig.exe");

                    ConfigManager.GeneralConfig.ForkFixVersion = 6;
                }
                if (Configs.ConfigManager.GeneralConfig.ForkFixVersion < 7)
                {
                    Helpers.ConsolePrint("NICEHASH", "Old version");
                    if (Directory.Exists("internals"))
                        Directory.Delete("internals", true);

                    ConfigManager.GeneralConfig.ForkFixVersion = 7;
                }

                if (Configs.ConfigManager.GeneralConfig.ForkFixVersion < 8)
                {
                    Helpers.ConsolePrint("NICEHASH", "Old version");
                    if (Directory.Exists("internals"))
                        Directory.Delete("internals", true);

                    ConfigManager.GeneralConfig.ForkFixVersion = 8;
                }
                if (Configs.ConfigManager.GeneralConfig.ForkFixVersion < 8.2)
                {
                    Helpers.ConsolePrint("NICEHASH", "Old version");
                    if (Directory.Exists("internals"))
                        Directory.Delete("internals", true);

                    ConfigManager.GeneralConfig.ForkFixVersion = 8.2;
                }
                if (Configs.ConfigManager.GeneralConfig.ForkFixVersion < 9)
                {
                    Helpers.ConsolePrint("NICEHASH", "Old version");
                    if (Directory.Exists("internals"))
                        Directory.Delete("internals", true);

                    if (File.Exists("bin\\xmrig\\xmrig.exe"))
                        File.Delete("bin\\xmrig\\xmrig.exe");

                    ConfigManager.GeneralConfig.ForkFixVersion = 9;
                }

                if (Configs.ConfigManager.GeneralConfig.ForkFixVersion < 9.1)
                {
                    Helpers.ConsolePrint("NICEHASH", "Old version");
                    if (Directory.Exists("internals"))
                        Directory.Delete("internals", true);

                    if (File.Exists("bin\\xmrig\\xmrig.exe"))
                        File.Delete("bin\\xmrig\\xmrig.exe");

                    if (File.Exists("bin_3rdparty\\t-rex\\t-rex.exe"))
                        File.Delete("bin_3rdparty\\t-rex\\t-rex.exe");

                    ConfigManager.GeneralConfig.ForkFixVersion = 9.1;
                }

                if (Configs.ConfigManager.GeneralConfig.ForkFixVersion < 9.2)
                {
                    Helpers.ConsolePrint("NICEHASH", "Old version");
                    if (Directory.Exists("internals"))
                        Directory.Delete("internals", true);

                    if (File.Exists("bin\\xmrig\\xmrig.exe"))
                        File.Delete("bin\\xmrig\\xmrig.exe");

                    if (File.Exists("bin_3rdparty\\t-rex\\t-rex.exe"))
                        File.Delete("bin_3rdparty\\t-rex\\t-rex.exe");

                    ConfigManager.GeneralConfig.ForkFixVersion = 9.2;
                }

                if (Configs.ConfigManager.GeneralConfig.ForkFixVersion < 9.3)
                {
                    Helpers.ConsolePrint("NICEHASH", "Old version");
                    if (Directory.Exists("internals"))
                        Directory.Delete("internals", true);

                    if (File.Exists("bin\\xmrig\\xmrig.exe"))
                        File.Delete("bin\\xmrig\\xmrig.exe");

                    if (File.Exists("bin_3rdparty\\t-rex\\t-rex.exe"))
                        File.Delete("bin_3rdparty\\t-rex\\t-rex.exe");

                    ConfigManager.GeneralConfig.ForkFixVersion = 9.3;
                }

                if (Configs.ConfigManager.GeneralConfig.ForkFixVersion < 10)
                {
                    Helpers.ConsolePrint("NICEHASH", "Old version");
                    if (Directory.Exists("internals"))
                        Directory.Delete("internals", true);

                    if (File.Exists("bin\\xmrig\\xmrig.exe"))
                        File.Delete("bin\\xmrig\\xmrig.exe");

                    if (File.Exists("bin_3rdparty\\t-rex\\t-rex.exe"))
                        File.Delete("bin_3rdparty\\t-rex\\t-rex.exe");

                    ConfigManager.GeneralConfig.ForkFixVersion = 10;
                }
                
                //**

                // init active display currency after config load
                ExchangeRateApi.ActiveDisplayCurrency = ConfigManager.GeneralConfig.DisplayCurrency;

                // #2 then parse args
                var commandLineArgs = new CommandLineParser(argv);

                Helpers.ConsolePrint("NICEHASH", "Starting up NiceHashMiner v" + Application.ProductVersion);

                if (!pathSet)
                {
                    Helpers.ConsolePrint("NICEHASH", "Path not set to executable");
                }

                var tosChecked = ConfigManager.GeneralConfig.agreedWithTOS == Globals.CurrentTosVer;
                if (!tosChecked || !ConfigManager.GeneralConfigIsFileExist() && !commandLineArgs.IsLang)
                {
                    Helpers.ConsolePrint("NICEHASH",
                        "No config file found. Running NiceHash Miner Legacy for the first time. Choosing a default language.");
                    Application.Run(new Form_ChooseLanguage());
                }

                // Init languages
                International.Initialize(ConfigManager.GeneralConfig.Language);

                if (commandLineArgs.IsLang)
                {
                    Helpers.ConsolePrint("NICEHASH", "Language is overwritten by command line parameter (-lang).");
                    International.Initialize(commandLineArgs.LangValue);
                    ConfigManager.GeneralConfig.Language = commandLineArgs.LangValue;
                }

                // check WMI
                if (Helpers.IsWmiEnabled())
                {
                    if (ConfigManager.GeneralConfig.agreedWithTOS == Globals.CurrentTosVer)
                    {
                        Application.Run(new Form_Main());
                    }
                }
                else
                {
                    MessageBox.Show(International.GetText("Program_WMI_Error_Text"),
                        International.GetText("Program_WMI_Error_Title"),
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
