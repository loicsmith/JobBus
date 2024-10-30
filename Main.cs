using Life;
using Life.BizSystem;
using Life.Network;
using Life.UI;
using Mirror;
using ModKit.Helper;
using ModKit.Interfaces;
using ModKit.Internal;
using ModKit.ORM;
using MODRP_JobBus.Classes;
using MODRP_JobBus.Functions;
using Newtonsoft.Json;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using _menu = AAMenu.Menu;

namespace MODRP_JobBus.Main
{

    class Main : ModKit.ModKit
    {
        public LineCreator LineCreator = new LineCreator();
        public LinePlayable LinePlayable = new LinePlayable();

        public static string ConfigDirectoryPath;
        public static string ConfigJobBusPath;
        public static JobBusConfig _JobBusConfig;

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

            InitConfig();
            _JobBusConfig = LoadConfigFile(ConfigJobBusPath);

            Console.WriteLine(_JobBusConfig.CityHallId);

            Nova.server.OnPlayerDisconnectEvent += (NetworkConnection conn) =>
            { 
                LinePlayable.RemoveNetIDFromList(conn.connectionId);
            };

        }


        private void InitConfig()
        {
            try
            {
                ConfigDirectoryPath = DirectoryPath + "/JobBus";
                ConfigJobBusPath = Path.Combine(ConfigDirectoryPath, "JobBusConfig.json");

                if (!Directory.Exists(ConfigDirectoryPath)) Directory.CreateDirectory(ConfigDirectoryPath);
                if (!File.Exists(ConfigJobBusPath)) InitBusJobConfig();
            }
            catch (IOException ex)
            {
                Logger.LogError("InitDirectory", ex.Message);
            }
        }

        private void InitBusJobConfig()
        {
            JobBusConfig JobBusConfig = new JobBusConfig();
            string json = JsonConvert.SerializeObject(JobBusConfig, Formatting.Indented);
            File.WriteAllText(ConfigJobBusPath, json);
        }

        private JobBusConfig LoadConfigFile(string path)
        {
            if (File.Exists(path))
            {
                string jsonContent = File.ReadAllText(path);
                JobBusConfig JobBusConfig = JsonConvert.DeserializeObject<JobBusConfig>(jsonContent);

                return JobBusConfig;
            }
            else return null;
        }

        private void SaveConfig(string path)
        {
            string json = JsonConvert.SerializeObject(_JobBusConfig, Formatting.Indented);
            File.WriteAllText(path, json);
        }

        public void ConfigEditor(Player player)
        {
            Panel panel = PanelHelper.Create("JobBus | Config JSON", UIPanel.PanelType.TabPrice, player, () => ConfigEditor(player));

            panel.AddTabLine($"{TextFormattingHelper.Color("CityHallId : ", TextFormattingHelper.Colors.Info)}" + $"{TextFormattingHelper.Color($"{_JobBusConfig.CityHallId}", TextFormattingHelper.Colors.Verbose)}", _ =>
            {
                EditLineInConfig(player, "CityHallId");
            });
            panel.AddTabLine($"{TextFormattingHelper.Color("TaxPercentage : ", TextFormattingHelper.Colors.Info)}" + $"{TextFormattingHelper.Color($"{_JobBusConfig.TaxPercentage}", TextFormattingHelper.Colors.Verbose)}", _ =>
            {
                EditLineInConfig(player, "TaxPercentage");
            });
            panel.AddTabLine($"{TextFormattingHelper.Color("PlayerReceivePercentage : ", TextFormattingHelper.Colors.Info)}" + $"{TextFormattingHelper.Color($"{_JobBusConfig.PlayerReceivePercentage}", TextFormattingHelper.Colors.Verbose)}", _ =>
            {
                EditLineInConfig(player, "PlayerReceivePercentage");
            });
            panel.AddTabLine($"{TextFormattingHelper.Color("MinCustomerPerBusStop : ", TextFormattingHelper.Colors.Info)}" + $"{TextFormattingHelper.Color($"{_JobBusConfig.MinCustomerPerBusStop}", TextFormattingHelper.Colors.Verbose)}", _ =>
            {
                EditLineInConfig(player, "MinCustomerPerBusStop");
            });
            panel.AddTabLine($"{TextFormattingHelper.Color("MaxCustomerPerBusStop : ", TextFormattingHelper.Colors.Info)}" + $"{TextFormattingHelper.Color($"{_JobBusConfig.MaxCustomerPerBusStop}", TextFormattingHelper.Colors.Verbose)}", _ =>
            {
                EditLineInConfig(player, "MaxCustomerPerBusStop");
            });
            panel.AddTabLine($"{TextFormattingHelper.Color("MinMoneyPerCustomer : ", TextFormattingHelper.Colors.Info)}" + $"{TextFormattingHelper.Color($"{_JobBusConfig.MinMoneyPerCustomer}", TextFormattingHelper.Colors.Verbose)}", _ =>
            {
                EditLineInConfig(player, "MinMoneyPerCustomer");
            });
            panel.AddTabLine($"{TextFormattingHelper.Color("MaxMoneyPerCustomer : ", TextFormattingHelper.Colors.Info)}" + $"{TextFormattingHelper.Color($"{_JobBusConfig.MaxMoneyPerCustomer}", TextFormattingHelper.Colors.Verbose)}", _ =>
            {
                EditLineInConfig(player, "MaxMoneyPerCustomer");
            });
            panel.AddTabLine($"{TextFormattingHelper.Color("Appliquer la configuration", TextFormattingHelper.Colors.Success)}", _ =>
            {
                SaveConfig(ConfigJobBusPath);
                panel.Refresh();
            });

            panel.NextButton("Sélectionner", () => panel.SelectTab());
            panel.AddButton("Retour", _ => AAMenu.AAMenu.menu.AdminPluginPanel(player));
            panel.CloseButton();
            panel.Display();
        }

        public void EditLineInConfig(Player player, string Param)
        {
            Panel panel = PanelHelper.Create("JobBus | Edit JSON", UIPanel.PanelType.Input, player, () => EditLineInConfig(player, Param));
            panel.TextLines.Add($"Modification de la valeur de : \"{Param}\"");
            panel.SetInputPlaceholder("Veuillez saisir une valeur");
            panel.AddButton("Valider", (ui) =>
            {
                string input = ui.inputText;

                switch (Param)
                {
                    case "CityHallId":
                        // int
                        if (int.TryParse(input, out int valueCity))
                        {
                            _JobBusConfig.CityHallId = valueCity;
                        }
                        else
                        {
                            player.Notify("JobBus", "Veuillez saisir un nombre entier.", NotificationManager.Type.Error);
                        }
                        break;
                    case "TaxPercentage":
                        // double
                        if (float.TryParse(input, out float valueTax))
                        {
                            _JobBusConfig.TaxPercentage = valueTax;
                        }
                        else
                        {
                            player.Notify("JobBus", "Veuillez saisir un nombre valide.", NotificationManager.Type.Error);
                        }
                        break;
                    case "PlayerReceivePercentage":
                        // double
                        if (float.TryParse(input, out float valueTaxPlayer))
                        {
                            _JobBusConfig.PlayerReceivePercentage = valueTaxPlayer;
                        }
                        else
                        {
                            player.Notify("JobBus", "Veuillez saisir un nombre valide.", NotificationManager.Type.Error);
                        }
                        break;
                    case "MinCustomerPerBusStop":
                        // int
                        if (int.TryParse(input, out int valueMinCustomer))
                        {
                            _JobBusConfig.MinCustomerPerBusStop = valueMinCustomer;
                        }
                        else
                        {
                            player.Notify("JobBus", "Veuillez saisir un nombre entier.", NotificationManager.Type.Error);
                        }
                        break;
                    case "MaxCustomerPerBusStop":
                        // int
                        if (int.TryParse(input, out int valueMaxCustomer))
                        {
                            _JobBusConfig.MaxCustomerPerBusStop = valueMaxCustomer;
                        }
                        else
                        {
                            player.Notify("JobBus", "Veuillez saisir un nombre entier.", NotificationManager.Type.Error);
                        }
                        _JobBusConfig.MaxCustomerPerBusStop = int.Parse(input);
                        break;
                    case "MinMoneyPerCustomer":
                        //float
                        if (float.TryParse(input, out float valueMinMoney))
                        {
                            _JobBusConfig.MinMoneyPerCustomer = valueMinMoney;
                        }
                        else
                        {
                            player.Notify("JobBus", "Veuillez saisir un nombre valide .", NotificationManager.Type.Error);
                        }
                        break;
                    case "MaxMoneyPerCustomer":
                        //float
                        if (float.TryParse(input, out float valueMaxMoney))
                        {
                            _JobBusConfig.MaxMoneyPerCustomer = valueMaxMoney;
                        }
                        else
                        {
                            player.Notify("JobBus", "Veuillez saisir un nombre valide.", NotificationManager.Type.Error);
                        }
                        break;
                }
                panel.Previous();
            });
            panel.PreviousButton();
            panel.CloseButton();
            panel.Display();
        }

        public void InitAAmenu()
        {
            _menu.AddAdminPluginTabLine(PluginInformations, 0, "JobBus", (ui) =>
            {
                Player player = PanelHelper.ReturnPlayerFromPanel(ui);
                ConfigEditor(player);
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
