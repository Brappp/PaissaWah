using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.IoC;
using System.IO;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using PaissaWah.Configuration;  
using PaissaWah.Handlers;
using PaissaWah.Windows;
using System;

namespace PaissaWah
{
    public sealed class Plugin : IDalamudPlugin
    {
        [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
        [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
        [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;

        private const string CommandName = "/PaissaWah";

        public PaissaWah.Configuration.Configuration Configuration { get; init; }  
        public CsvManager CsvManager { get; private set; }
        public LifestreamIpcHandler LifestreamIpcHandler { get; private set; }

        public readonly WindowSystem WindowSystem = new("PaissaWah");
        private ConfigWindow ConfigWindow { get; init; } 
        private MainWindow MainWindow { get; init; }

        public Plugin()
        {
            Configuration = PaissaWah.Configuration.Configuration.Load();  
            CsvManager = new CsvManager(Configuration, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "XIVLauncher", "pluginConfigs", "PaissaWah"));
            LifestreamIpcHandler = new LifestreamIpcHandler(PluginInterface);

            ConfigWindow = new ConfigWindow(this);
            MainWindow = new MainWindow(this, Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png"));

            WindowSystem.AddWindow(ConfigWindow);
            WindowSystem.AddWindow(MainWindow);

            CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "A useful message to display in /xlhelp"
            });

            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
            PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;
        }

        public void Dispose()
        {
            WindowSystem.RemoveAllWindows();
            ConfigWindow.Dispose();
            MainWindow.Dispose();
            CommandManager.RemoveHandler(CommandName);
        }

        private void OnCommand(string command, string args)
        {
            ToggleMainUI();
        }

        private void DrawUI() => WindowSystem.Draw();

        public void ToggleConfigUI() => ConfigWindow.Toggle();
        public void ToggleMainUI() => MainWindow.Toggle();
    }
}
