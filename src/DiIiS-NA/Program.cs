﻿//Blizzless Project 2022
//Blizzless Project 2022 
using DiIiS_NA.Core.Discord.Modules;
using DiIiS_NA.Core.Logging;
//Blizzless Project 2022 
using DiIiS_NA.Core.MPQ;
//Blizzless Project 2022 
using DiIiS_NA.Core.Storage;
//Blizzless Project 2022 
using DiIiS_NA.Core.Storage.AccountDataBase.Entities;
//Blizzless Project 2022 
using DiIiS_NA.GameServer.AchievementSystem;
//Blizzless Project 2022 
using DiIiS_NA.GameServer.CommandManager;
using DiIiS_NA.GameServer.GSSystem.ActorSystem;
//Blizzless Project 2022 
using DiIiS_NA.GameServer.GSSystem.GameSystem;
//Blizzless Project 2022 
using DiIiS_NA.GameServer.GSSystem.ItemsSystem;
//Blizzless Project 2022 
using DiIiS_NA.LoginServer;
//Blizzless Project 2022 
using DiIiS_NA.LoginServer.AccountsSystem;
//Blizzless Project 2022 
using DiIiS_NA.LoginServer.Base;
//Blizzless Project 2022 
using DiIiS_NA.LoginServer.Battle;
//Blizzless Project 2022 
using DiIiS_NA.LoginServer.GuildSystem;
//Blizzless Project 2022 
using DiIiS_NA.LoginServer.Toons;
//Blizzless Project 2022 
using DiIiS_NA.REST;
//Blizzless Project 2022 
using DiIiS_NA.REST.Manager;
//Blizzless Project 2022 
using DotNetty.Handlers.Logging;
//Blizzless Project 2022 
using DotNetty.Transport.Bootstrapping;
//Blizzless Project 2022 
using DotNetty.Transport.Channels;
//Blizzless Project 2022 
using DotNetty.Transport.Channels.Sockets;
//Blizzless Project 2022 
using Npgsql;
//Blizzless Project 2022 
using System;
//Blizzless Project 2022 
using System.Data.Common;
using System.Diagnostics;
//Blizzless Project 2022 
using System.Globalization;
//Blizzless Project 2022 
using System.Linq;
//Blizzless Project 2022 
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
//Blizzless Project 2022 
using System.Security;
//Blizzless Project 2022 
using System.Security.Permissions;
//Blizzless Project 2022 
using System.Threading;
//Blizzless Project 2022 
using System.Threading.Tasks;
using Spectre.Console;
using Environment = System.Environment;

namespace DiIiS_NA
{

    class Program
    {
        private static readonly Logger Logger = LogManager.CreateLogger("BZ.Net");
        public static readonly DateTime StartupTime = DateTime.Now;
        public static BattleBackend BattleBackend { get; set; }
        public bool GameServersAvailable = true;

        public static int MaxLevel = 70;

        public static GameServer.ClientSystem.GameServer GameServer;
        public static Watchdog Watchdog;

        public static Thread GameServerThread;
        public static Thread WatchdogThread;

        public static string LOGINSERVERIP = DiIiS_NA.LoginServer.Config.Instance.BindIP;
        public static string GAMESERVERIP = DiIiS_NA.GameServer.Config.Instance.BindIP;
        public static string RESTSERVERIP = DiIiS_NA.REST.Config.Instance.IP;
        public static string PUBLICGAMESERVERIP = DiIiS_NA.GameServer.NATConfig.Instance.PublicIP;

        public static int Build = 30;
        private static readonly int Stage = 1;
        private static readonly string TypeBuild = "BETA";
        private static bool D3CoreEnabled = DiIiS_NA.GameServer.Config.Instance.CoreActive;


    static async Task LoginServer()
        {
#if DEBUG
            D3CoreEnabled = true;
#endif
            DbProviderFactories.RegisterFactory("Npgsql", NpgsqlFactory.Instance);
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            
            string name = $"Blizzless: Build {Build}, Stage: {Stage} - {TypeBuild}";
            SetTitle(name);
            Maximize();
            AnsiConsole.Write(new Rule("[dodgerblue1]Blizz[/][deepskyblue2]less[/]").RuleStyle("steelblue1"));
            AnsiConsole.Write(new Rule($"[dodgerblue3]Build [/][deepskyblue3]{Build}[/]").RightJustified().RuleStyle("steelblue1_1"));
            AnsiConsole.Write(new Rule($"[dodgerblue3]Stage [/][deepskyblue3]{Stage}[/]").RightJustified().RuleStyle("steelblue1_1"));
            AnsiConsole.Write(new Rule($"[deepskyblue3]{TypeBuild}[/]").RightJustified().RuleStyle("steelblue1_1"));
            AnsiConsole.Write(new Rule($"[red3_1]Diablo III[/] [red]RoS 2.7.4.84161[/] - [link=https://github.com/blizzless/blizzless-diiis]https://github.com/blizzless/blizzless-diiis[/]").RuleStyle("red"));
        
            AnsiConsole.MarkupLine("");
            Console.WriteLine();
            Console.Title = name;

            InitLoggers();
            
#pragma warning disable CS4014
            Task.Run(async () =>
#pragma warning restore CS4014
            {
                while (true)
                {
                    try
                    {
                        var uptime = (DateTime.Now - StartupTime);
                        // get total memory from process
                        var totalMemory =
                            (double)((double)Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024 / 1024);
                        // get CPU time
                        using var proc = Process.GetCurrentProcess();
                        var cpuTime = proc.TotalProcessorTime;
                        var text =
                            $"{name} | " +
                            $"{PlayerManager.OnlinePlayers.Count()} onlines in {PlayerManager.OnlinePlayers.Count(s => s.InGameClient?.Player?.World != null)} worlds | " +
                            $"Memory: {totalMemory:0.000} GB | " +
                            $"CPU Time: {cpuTime.ToSmallText()} | " +
                            $"Uptime: {uptime.ToSmallText()}";
                        if (SetTitle(text))
                            await Task.Delay(1000);
                        else
                        {
                            Logger.Info(text);
                            await Task.Delay(TimeSpan.FromMinutes(1));
                        }
                    }
                    catch (Exception e)
                    {
                    }
                }
            });
            AchievementManager.Initialize();
            Core.Storage.AccountDataBase.SessionProvider.RebuildSchema();
            string GeneratePassword(int size) => new string(Enumerable.Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789", size)
                .Select(s => s[new Random().Next(s.Length)]).ToArray());
            void LogAccountCreated(string username, string password)
                => Logger.Success($"Created account: $[springgreen4]${username}$[/]$ with password: $[springgreen4]${password}$[/]$");
#if DEBUG
            if (!DBSessions.SessionQuery<DBAccount>().Any())
            {
                var password1 = GeneratePassword(6);
                var password2 = GeneratePassword(6);
                var password3 = GeneratePassword(6);
                var password4 = GeneratePassword(6);
                Logger.Info($"Initializing account database...");
               var account = AccountManager.CreateAccount("owner@", password1, "owner",  Account.UserLevels.Owner);
                var gameAccount = GameAccountManager.CreateGameAccount(account);
                LogAccountCreated("owner@", password1);
                var account1 = AccountManager.CreateAccount("gm@", password2, "gm", Account.UserLevels.GM);
                var gameAccount1 = GameAccountManager.CreateGameAccount(account1);
                LogAccountCreated("gm@", password2);
                var account2 = AccountManager.CreateAccount("tester@", password3, "tester", Account.UserLevels.Tester);
                var gameAccount2 = GameAccountManager.CreateGameAccount(account2);
                LogAccountCreated("tester@", password3);
                var account3 = AccountManager.CreateAccount("user@", password4, "test3", Account.UserLevels.User);
                var gameAccount3 = GameAccountManager.CreateGameAccount(account3);
                LogAccountCreated("user@", password4);
            }
#else
            if (!Enumerable.Any(DBSessions.SessionQuery<DBAccount>()))
            {
                var password = GeneratePassword(6);
                var account = AccountManager.CreateAccount("iwannatry@", password, "iwannatry", Account.UserLevels.User);
                var gameAccount = GameAccountManager.CreateGameAccount(account);
                LogAccountCreated("iwannatry@", password);
            }
#endif
            
            if (DBSessions.SessionQuery<DBAccount>().Any())
            {
                Logger.Success("Connection with database established.");
            }
            //*/
            StartWatchdog();

            AccountManager.PreLoadAccounts();
            GameAccountManager.PreLoadGameAccounts();
            ToonManager.PreLoadToons();
            GuildManager.PreLoadGuilds();

            Logger.Info("Loading Diablo III - Core..."); 
            if (D3CoreEnabled)
            {
                if (!MPQStorage.Initialized)
                {
                    Logger.Fatal("MPQ archives not found...");
                    Shutdown();
                    return;
                }
                Logger.Info("Loaded - {0} items.", ItemGenerator.TotalItems); 
                Logger.Info("Diablo III Core - Loaded"); 
            }
            else
            {
                Logger.Fatal("Diablo III Core - Disabled");
                Shutdown();
                return;
            }
           
            var restSocketServer = new SocketManager<RestSession>();
            if (!restSocketServer.StartNetwork(RESTSERVERIP, REST.Config.Instance.PORT))
            {
                Logger.Fatal("REST socket server can't start.");
                Shutdown();
                return;
            }
            Logger.Success($"REST server started - {REST.Config.Instance.IP}:{REST.Config.Instance.PORT}");

           
            //BGS
            ServerBootstrap b = new ServerBootstrap();
            IEventLoopGroup boss = new MultithreadEventLoopGroup(1);
            IEventLoopGroup worker = new MultithreadEventLoopGroup();
            b.LocalAddress(DiIiS_NA.LoginServer.Config.Instance.BindIP, DiIiS_NA.LoginServer.Config.Instance.Port);
            Logger.Info(
                $"Blizzless server started - {DiIiS_NA.LoginServer.Config.Instance.BindIP}:{DiIiS_NA.LoginServer.Config.Instance.Port}");
            BattleBackend = new BattleBackend(DiIiS_NA.LoginServer.Config.Instance.BindIP, DiIiS_NA.LoginServer.Config.Instance.WebPort);

            //Diablo 3 Game-Server
            if (D3CoreEnabled) 
                StartGS();

            try
            {
                b.Group(boss, worker)
                    .Channel<TcpServerSocketChannel>()
                    .Handler(new LoggingHandler(LogLevel.DEBUG))
                    .ChildHandler(new ConnectHandler());

                IChannel boundChannel = await b.BindAsync(DiIiS_NA.LoginServer.Config.Instance.Port);

                Logger.Info("$[bold red3_1]$Tip:$[/]$ graceful shutdown with $[red3_1]$CTRL+C$[/]$ or $[red3_1]$!q[uit]$[/]$ or $[red3_1]$!exit$[/]$.");
                Logger.Info("$[bold red3_1]$Tip:$[/]$ SNO breakdown with $[red3_1]$!sno$[/]$ $[red]$<fullSnoBreakdown(true:false)>$[/]$.");
                while (true)
                {
                    var line = Console.ReadLine();
                    if (line is null or "!q" or "!quit" or "!exit")
                        break;
                    if (line is "!cls" or "!clear" or "cls" or "clear")
                    {
                        Console.Clear();
                        continue;
                    }
                    if (line.ToLower().StartsWith("!sno"))
                    {
                        if (IsTargetEnabled("ansi"))
                            Console.Clear();
                        MPQStorage.Data.SnoBreakdown(line.ToLower().Equals("!sno 1") || line.ToLower().Equals("!sno true"));
                        continue;
                    }
                    CommandManager.Parse(line);
                }

                if (PlayerManager.OnlinePlayers.Count > 0)
                {
                    Logger.Info($"Server is shutting down in 1 minute, $[blue]${PlayerManager.OnlinePlayers.Count} players$[/]$ are still online.");
                    PlayerManager.SendWhisper("Server is shutting down in 1 minute.");
                    await Task.Delay(TimeSpan.FromMinutes(1));
                }
                
                Shutdown(delay: 25);
            }
            catch (Exception e)
            {
                Logger.Info(e.Message);
            }
            finally
            {
                await Task.WhenAll(
                    boss.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1)),
                    worker.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1)));
            }
        }

        public static void Shutdown(int delay = 50)
        {
            if (!IsTargetEnabled("ansi"))
            {
                AnsiConsole.Progress().Start(ctx =>
                {
                    var task = ctx.AddTask("[red]Shutting down...[/]");
                    for (int i = 0; i < 100; i++)
                    {
                        task.Increment(1);
                        Thread.Sleep(delay);
                    }
                });
            }
            Environment.Exit(-1);
        }
        
        [HandleProcessCorruptedStateExceptions]
        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlAppDomain)]
        static void Main() 
        {
            LoginServer().Wait(); 
        }
         
        [SecurityCritical]
        [HandleProcessCorruptedStateExceptionsAttribute]
        private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;

            if (e.IsTerminating)
            {
                Logger.Error(ex.StackTrace);
                Logger.FatalException(ex, "A root error of the server was detected, disconnection.");
                Shutdown();
            }
            else
                Logger.ErrorException(ex, "A root error of the server was detected but was handled.");
        }

        static int TargetsEnabled(string target) => LogConfig.Instance.Targets.Count(t => t.Target.ToLower() == target && t.Enabled);
        public static bool IsTargetEnabled(string target) => TargetsEnabled(target) > 0;
        private static void InitLoggers()
        {
            LogManager.Enabled = true;
            
            if (TargetsEnabled("ansi") > 1 || (IsTargetEnabled("console") && IsTargetEnabled("ansi")))
            {
                AnsiConsole.MarkupLine("[underline red on white]Fatal:[/] [red]You can't use both ansi and console targets at the same time, nor have more than one ansi target.[/]");
                AnsiConsole.Progress().Start(ctx =>
                {
                    var sd = ctx.AddTask("[red3_1]Shutting down[/]");
                    for (int i = 0; i < 100; i++)
                    {
                        sd.Increment(1);
                        Thread.Sleep(25);
                    }
                });
                Environment.Exit(-1);
            }
            foreach (var targetConfig in LogConfig.Instance.Targets)
            {
                if (!targetConfig.Enabled)
                    continue;

                LogTarget target = null;
                switch (targetConfig.Target.ToLower())
                {
                    case "ansi":
                        target = new AnsiTarget(targetConfig.MinimumLevel, targetConfig.MaximumLevel, targetConfig.IncludeTimeStamps);
                        break;
                    case "console":
                        target = new ConsoleTarget(targetConfig.MinimumLevel, targetConfig.MaximumLevel,
                                                   targetConfig.IncludeTimeStamps);
                        break;
                    case "file":
                        target = new FileTarget(targetConfig.FileName, targetConfig.MinimumLevel,
                                                targetConfig.MaximumLevel, targetConfig.IncludeTimeStamps,
                                                targetConfig.ResetOnStartup);
                        break;
                }

                if (target != null)
                    LogManager.AttachLogTarget(target);
            }
        }
        public static bool StartWatchdog()
        {
            Watchdog = new Watchdog();
            WatchdogThread = new Thread(Watchdog.Run) { Name = "Watchdog", IsBackground = true };
            WatchdogThread.Start();
            return true;
        }
        public static bool StartGS()
        {
            if (GameServer != null) return false;

            GameServer = new DiIiS_NA.GameServer.ClientSystem.GameServer();
            GameServerThread = new Thread(GameServer.Run) { Name = "GameServerThread", IsBackground = true };
            GameServerThread.Start();
            if (DiIiS_NA.Core.Discord.Config.Instance.Enabled)
            {
                Logger.Info("Starting Discord bot handler..");
                GameServer.DiscordBot = new Core.Discord.Bot();
                GameServer.DiscordBot.MainAsync().GetAwaiter().GetResult();
            }
            else
            {
                Logger.Info("Discord bot Disabled..");
            }
            DiIiS_NA.GameServer.GSSystem.GeneratorsSystem.SpawnGenerator.RegenerateDensity();
            DiIiS_NA.GameServer.ClientSystem.GameServer.GSBackend = new GsBackend(DiIiS_NA.LoginServer.Config.Instance.BindIP, DiIiS_NA.LoginServer.Config.Instance.WebPort);
            return true;
        }

        static bool SetTitle(string text)
        {
            try
            {
                Console.Title = text;
                return true;
            }
            catch (PlatformNotSupportedException)
            {
                return false;
            }
        }
        
        [DllImport("kernel32.dll", ExactSpelling = true)]

        static extern IntPtr GetConsoleWindow();
        static IntPtr ThisConsole = GetConsoleWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]

        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        const int HIDE = 0;
        const int MAXIMIZE = 3;
        const int MINIMIZE = 6;
        const int RESTORE = 9;
        private static void Maximize()
        {
            // if it's running on windows
            try
            {
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    ShowWindow(ThisConsole, MAXIMIZE);
                }
            }
            catch{ /*ignore*/ }
        }
    }
}
