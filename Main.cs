﻿using Life;
using Life.BizSystem;
using Life.Network;
using ModKit.Helper;
using ModKit.Interfaces;
using ModKit.Internal;
using ModKit.ORM;
using MODRP_JobBus.Functions;
using SQLite;
using System.Collections.Generic;
using _menu = AAMenu.Menu;

namespace MODRP_JobBus.Main
{

    class Main : ModKit.ModKit
    {
        public LineCreator LineCreator = new LineCreator();
        public LinePlayable LinePlayable = new LinePlayable();

        public Main(IGameAPI api) : base(api)
        {
            PluginInformations = new PluginInformations(AssemblyHelper.GetName(), "1.0.0", "Loicsmith");

            LineCreator.Context = this;
            LinePlayable.Context = this;
        }

        public override void OnPluginInit()
        {
            base.OnPluginInit();
            Logger.LogSuccess($"{PluginInformations.SourceName} v{PluginInformations.Version}", "initialisé");

            InitAAmenu();

            Orm.RegisterTable<OrmManager.JobBus_LineManager>();
            Orm.RegisterTable<OrmManager.JobBus_BusStopManager>();
        }

        public void InitAAmenu()
        {
            _menu.AddAdminPluginTabLine(PluginInformations, 0, "JobBus", (ui) =>
            {
                Player player = PanelHelper.ReturnPlayerFromPanel(ui);
            });

            _menu.AddBizTabLine(PluginInformations, new List<Activity.Type> { Activity.Type.Bus }, null, "Utiliser SAE", (ui) =>
            {
                Player player = PanelHelper.ReturnPlayerFromPanel(ui);
                LinePlayable.MainPanel(player);
            });

            _menu.AddAdminTabLine(PluginInformations, 5, $"{TextFormattingHelper.Color("LineCreator - JobBus", TextFormattingHelper.Colors.Grey)}", (ui) =>
            {
                Player player = PanelHelper.ReturnPlayerFromPanel(ui);
                LineCreator.MainPanel(player);
            });
        }
    }
}
