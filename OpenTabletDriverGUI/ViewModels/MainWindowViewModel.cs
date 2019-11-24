﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using OpenTabletDriverGUI.Models;
using OpenTabletDriverGUI.Views;
using ReactiveUI;
using TabletDriverLib;
using TabletDriverLib.Component;
using TabletDriverLib.Interop;
using TabletDriverLib.Interop.Cursor;
using TabletDriverLib.Interop.Display;
using TabletDriverLib.Output;
using TabletDriverLib.Tablet;

namespace OpenTabletDriverGUI.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public MainWindowViewModel()
        {
            Trace.Listeners.Add(TraceListener);
        }

        public void Initialize()
        {
            // Create new instance of the driver
            Driver = new Driver();
            Driver.TabletSuccessfullyOpened += (sender, tablet) => 
            {
                FullTabletWidth = tablet.Width;
                FullTabletHeight = tablet.Height;
                Driver.OutputMode.TabletProperties = tablet;
                Driver.BindInput(InputHooked);
            };

            // Use platform specific display
            Display = Platform.Display;

            Log.Info($"Current directory is '{Environment.CurrentDirectory}'.");
            
            var settings = new FileInfo(Path.Combine(Environment.CurrentDirectory, "settings.xml"));
            if (settings.Exists)
            {
                Settings = Settings.Deserialize(settings);
                Log.Info("Loaded user settings.");
            }
            else
            {
                UseDefaultSettings();
            }
            
            // Update output mode from settings
            SetMode(Settings.OutputMode);

            // Find tablet configurations and try to open a tablet
            var configurationDir = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, "Configurations"));
            if (configurationDir.Exists)
                OpenConfigurations(configurationDir);
            else
                Tablets = new ObservableCollection<TabletProperties>();
        }

        #region Bindable Properties

        public Settings Settings
        {
            set
            {
                this.RaiseAndSetIfChanged(ref _settings, value);
                ApplySettings();
            }
            get => _settings;
        }
        private Settings _settings;
        
        private ReactiveTraceListener TraceListener
        {
            set => this.RaiseAndSetIfChanged(ref _trace, value);
            get => _trace;
        }
        private ReactiveTraceListener _trace = new ReactiveTraceListener();

        private Driver Driver
        {
            set => this.RaiseAndSetIfChanged(ref _driver, value);
            get => _driver;
        }
        private Driver _driver;

        public bool InputHooked 
        {
            private set => this.RaiseAndSetIfChanged(ref _hooked, value);
            get => _hooked;
        }
        private bool _hooked;

        private IDisplay Display
        {
            set => this.RaiseAndSetIfChanged(ref _disp, value);
            get => _disp;
        }
        private IDisplay _disp;

        private float FullTabletWidth
        {
            set => this.RaiseAndSetIfChanged(ref _fTabW, value);
            get => _fTabW;
        }
        private float _fTabW;

        private float FullTabletHeight
        {
            set => this.RaiseAndSetIfChanged(ref _fTabH, value);
            get => _fTabH;
        }
        private float _fTabH;

        public ObservableCollection<TabletProperties> Tablets
        {
            set => this.RaiseAndSetIfChanged(ref _tablets, value);
            get => _tablets;
        }
        private ObservableCollection<TabletProperties> _tablets;

        public bool Debugging
        {
            set
            {
                this.RaiseAndSetIfChanged(ref _debugging, value);
                Driver.Debugging = value;
            } 
            get => _debugging;
        }
        private bool _debugging;

        #endregion

        #region Buttons

        private void OpenConfigurations(DirectoryInfo directory)
        {
            List<FileInfo> configRepository = directory.EnumerateFiles().ToList();
            foreach (var dir in directory.EnumerateDirectories())
                configRepository.AddRange(dir.EnumerateFiles());

            Tablets = configRepository.ConvertAll(file => TabletProperties.Read(file)).ToObservableCollection();
            Driver.OpenTablet(Tablets);
        }

        public void ApplySettings()
        {
            Log.Info($"Using output mode '{Settings.OutputMode}'");
            if (Driver.OutputMode is OutputMode outputMode)
            {
                outputMode.TabletProperties = Driver.TabletProperties;
            }
            if (Driver.OutputMode is AbsoluteMode absolute)
            {
                absolute.DisplayArea = new Area
                {
                    Width = Settings.DisplayWidth,
                    Height = Settings.DisplayHeight,
                    Position = new Point(Settings.DisplayX, Settings.DisplayY),
                    Rotation = Settings.DisplayRotation
                };
                Log.Info($"Set display area: " + absolute.DisplayArea);
                
                absolute.TabletArea = new Area
                {
                    Width = Settings.TabletWidth,
                    Height = Settings.TabletHeight,
                    Position = new Point(Settings.TabletX, Settings.TabletY),
                    Rotation = Settings.TabletRotation
                };
                Log.Info($"Set tablet area:  " + absolute.TabletArea);
                
                absolute.Clipping = Settings.EnableClipping;
                Log.Info("Clipping is " + (absolute.Clipping ? "enabled" : "disabled"));
                
                absolute.MouseButtonBindings[0] = Settings.TipButton;
                absolute.TipActivationPressure = Settings.TipActivationPressure;
                absolute.BindingsEnabled = !absolute.MouseButtonBindings.All(btn => btn.Value == MouseButton.None);
                Log.Info($"Bindings set: Tip='{absolute.MouseButtonBindings[0]}'@{absolute.TipActivationPressure}%");
            }
            Log.Info("Applied all settings.");
        }

        public void UseDefaultSettings()
        {
            Settings = new Settings()
            {
                DisplayWidth = Display.Width,
                DisplayHeight = Display.Height,
                DisplayX = 0,
                DisplayY = 0
            };

            if (Driver.Tablet != null)
            {
                Settings.TabletWidth = Driver.TabletProperties.Width;
                Settings.TabletHeight = Driver.TabletProperties.Height;
                Settings.TabletX = 0;
                Settings.TabletY = 0;
                ApplySettings();
            }
        }

        public void DetectTablet()
        {
            Driver.OpenTablet(Tablets);
        }

        public async Task OpenConfigurationManager()
        {
            var cfgMgr = new ConfigurationManager()
            {
                DataContext = new ConfigurationManagerViewModel()
                {
                    Configurations = Tablets,
                    Devices = Driver.Devices.ToList().ToObservableCollection()
                }
            };
            await cfgMgr.ShowDialog(App.Current.MainWindow);
            Driver.OpenTablet(Tablets);
        }

        public async Task OpenTabletConfigurationFolder()
        {
            var fd = new OpenFolderDialog();
            var path = await fd.ShowAsync(App.Current.MainWindow);
            if (path != null)
            {
                var directory = new DirectoryInfo(path);
                if (directory.Exists)
                    OpenConfigurations(directory);
            }
        }

        public async Task LoadSettingsDialog()
        {
            var fd = FileDialogs.CreateOpenFileDialog("Open settings", "XML Document", "xml");
            var result = await fd.ShowAsync(App.Current.MainWindow);
            if (result != null)
            {
                var file = new FileInfo(result[0]);
                try
                {
                    Settings = Settings.Deserialize(file);
                    Log.Info("Successfully read settings from file.");
                }
                catch (Exception ex)
                {
                    Log.Exception(ex);
                    Log.Fail("Unable to read settings from file: " + result[0]);
                }
            }
        }

        public async Task SaveSettingsDialog()
        {
            var fd = FileDialogs.CreateSaveFileDialog("Saving settings", "XML Document", "xml");
            var path = await fd.ShowAsync(App.Current.MainWindow);
            if (path != null)
            {
                var file = new FileInfo(path);
                try 
                {
                    Settings.Serialize(file);
                    Log.Info("Wrote settings to file: " + path);
                }
                catch (Exception ex)
                {
                    Log.Exception(ex);
                    Log.Fail("Unable to write settings to file: " + path);
                }
            }
        }

        public void ToggleHook()
        {
            try
            {
                InputHooked = !InputHooked;
                Log.Info("Hooking inputs: " + InputHooked);
                Driver.BindInput(InputHooked);
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                Log.Fail("Unable to hook input.");
                InputHooked = !InputHooked;
            }
        }

        private void SetTheme(string name)
        {
            Settings.Theme = name;
            Log.Info($"Using theme '{name}'.");
            (App.Current as App).Restart(this);
        }

        private void SetMode(string name)
        {
            Settings.OutputMode = name;
            switch (Settings.OutputMode)
            {
                case "Absolute":
                    Driver.OutputMode = new AbsoluteMode();
                    break;
            }
            Driver.OutputMode.TabletProperties = Driver.TabletProperties ?? null;
            ApplySettings();
        }

        #endregion
    }
}