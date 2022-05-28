﻿using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Gw2Sharp.WebApi.V2.Models;
using Kenedia.Modules.QoL.Classes;
using Kenedia.Modules.QoL.SubModules;
using Kenedia.Modules.QoL.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Kenedia.Modules.QoL
{
    [Export(typeof(Module))]
    public class QoL : Module
    {
        internal static QoL ModuleInstance;
        public static readonly Logger Logger = Logger.GetLogger<QoL>();

        #region Service Managers

        internal SettingsManager SettingsManager => this.ModuleParameters.SettingsManager;
        internal ContentsManager ContentsManager => this.ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => this.ModuleParameters.DirectoriesManager;
        internal Gw2ApiManager Gw2ApiManager => this.ModuleParameters.Gw2ApiManager;

        #endregion

        [ImportingConstructor]
        public QoL([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters)
        {
            ModuleInstance = this;
            TextureManager = new TextureManager();
            Ticks = new Ticks();
            Modules = new List<SubModule>()
            {
                new ItemDestruction(),
                new SkipCutscenes(),
                new ZoomOut(),
            };
        }

        public string CultureString;
        public TextureManager TextureManager;
        public Ticks Ticks;

        public WindowBase2 MainWindow;
        public Hotbar Hotbar;

        public List<SubModule> Modules;

        public SettingEntry<Blish_HUD.Input.KeyBinding> ReloadKey;

        private bool _DataLoaded;
        public bool FetchingAPI;
        public bool DataLoaded
        {
            get => _DataLoaded;
            set
            {
                _DataLoaded = value;
                if (value) ModuleInstance.OnDataLoaded();
            }
        }

        public event EventHandler DataLoaded_Event;
        void OnDataLoaded()
        {
            this.DataLoaded_Event?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler LanguageChanged;
        public void OnLanguageChanged(object sender, EventArgs e)
        {
            this.LanguageChanged?.Invoke(this, EventArgs.Empty);
        }

        protected override void DefineSettings(SettingCollection settings)
        {
            foreach (SubModule module in Modules)
            {
                var subSettings = settings.AddSubCollection(module.Name + " - Settings", true, false);
                module.DefineSettings(subSettings);
            }


            var internal_settings = settings.AddSubCollection("Internal Settings", false);
            ReloadKey = internal_settings.DefineSetting(nameof(ReloadKey),
                                                      new Blish_HUD.Input.KeyBinding(Keys.None),
                                                      () => "Reload Button",
                                                      () => "");

            ReloadKey.Value.Enabled = true;
            ReloadKey.Value.Activated += RebuildUI;
        }

        protected override void Initialize()
        {
            Logger.Info($"Starting  {Name} v." + Version.BaseVersion());

            DataLoaded = false;
        }

        private void ToggleWindow_Activated(object sender, EventArgs e)
        {
            MainWindow?.ToggleWindow();
        }

        protected override async Task LoadAsync()
        {
        }

        protected override void OnModuleLoaded(EventArgs e)
        {
            DataLoaded_Event += QoL_DataLoaded_Event;
            OverlayService.Overlay.UserLocale.SettingChanged += UserLocale_SettingChanged;

            // Base handler must be called
            base.OnModuleLoaded(e);

            LoadData();
        }

        private void QoL_DataLoaded_Event(object sender, EventArgs e)
        {
            CreateUI();
        }

        private void ToggleModule(object sender, Blish_HUD.Input.MouseEventArgs e)
        {
            if (MainWindow != null) MainWindow.ToggleWindow();
        }

        protected override void Update(GameTime gameTime)
        {
            if (gameTime.TotalGameTime.TotalMilliseconds - Ticks.global >= 5)
            {
                Ticks.global = gameTime.TotalGameTime.TotalMilliseconds;
                foreach (SubModule module in Modules)
                {
                    if (module.Loaded && module.Active)
                    {
                        module.Update(gameTime);
                    }
                }
            }
        }

        protected override void Unload()
        {
            foreach (SubModule module in Modules)
            {
                module.Dispose();
            }
            Modules.Clear();

            DisposeUI();

            TextureManager.Dispose();
            TextureManager = null;

            DataLoaded_Event -= QoL_DataLoaded_Event;
            OverlayService.Overlay.UserLocale.SettingChanged -= UserLocale_SettingChanged;
            ReloadKey.Value.Activated -= RebuildUI;

            DataLoaded = false;
            ModuleInstance = null;
        }


        public async Task Fetch_APIData(bool force = false)
        {
        }

        async Task LoadData()
        {

            DataLoaded = true;
        }

        private async void UserLocale_SettingChanged(object sender, ValueChangedEventArgs<Gw2Sharp.WebApi.Locale> e)
        {
            await LoadData();

            OnLanguageChanged(null, null);
        }

        private void RebuildUI(object sender, EventArgs e)
        {
            ScreenNotification.ShowNotification("Rebuilding the UI", ScreenNotification.NotificationType.Warning);
            DisposeUI();
            CreateUI();
        }

        private void DisposeUI()
        {
            MainWindow?.Dispose();
            Hotbar?.Dispose();
        }
        private void CreateUI()
        {
            Hotbar = new Hotbar()
            {
                Parent = GameService.Graphics.SpriteScreen,
                Location = new Point(0, 34),
                Size = new Point(36, 36),
                ButtonSize = new Point(28, 28)
            };

            foreach (SubModule module in Modules)
            {
                module.Hotbar_Button = new Hotbar_Button()
                {
                    SubModule = module,
                    BasicTooltipText = string.Format(Strings.common.Toggle, $"{module.Name}"),
                };

                Hotbar.AddButton(module.Hotbar_Button);
            }
        }
    }
}