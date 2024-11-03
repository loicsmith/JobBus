using Life;
using Life.CheckpointSystem;
using Life.Network;
using Life.UI;
using Life.VehicleSystem;
using Mirror;
using ModKit.Helper;
using ModKit.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace MODRP_JobBus.Functions
{
    internal class LinePlayable
    {
        public DataManager DataManager = new DataManager();

        List<uint> PlayerNetID = new List<uint>();

        public ModKit.ModKit Context { get; set; }

        public void MainPanel(Player player)
        {
            if (player.GetVehicleModel() == "Euro Lion's City 12" || player.GetVehicleModel() == "Fast Scoler 4")
            {
                Panel panel = Context.PanelHelper.Create("SAE | Accueil", UIPanel.PanelType.Text, player, () => MainPanel(player));

                panel.TextLines.Add($"{TextFormattingHelper.Bold(TextFormattingHelper.Align("Informations :", TextFormattingHelper.Aligns.Center))}");

                panel.TextLines.Add($"{TextFormattingHelper.Size("Configuration : Prise de service d'une ligne", 20)}");
                panel.TextLines.Add($"{TextFormattingHelper.Size("Ventes : Permet de vendre des tickets", 20)}");

                panel.NextButton("Configuration", () => { LinePlayable_AvailableLine(player); });
                panel.NextButton($"{TextFormattingHelper.Size("Ventes (Indisponible)", 15)}", () => { });

                panel.AddButton("Retour", ui => AAMenu.AAMenu.menu.BizPanel(player));
                panel.CloseButton();
                panel.Display();
            }
            else
            {
                player.Notify("SAE", "Vous devez être dans un bus afin de pouvoir utiliser le SAE !", NotificationManager.Type.Error);
            }

        }

        public async void LinePlayable_AvailableLine(Player player)
        {
            Panel panel = Context.PanelHelper.Create("SAE | Configuration", UIPanel.PanelType.TabPrice, player, () => LinePlayable_AvailableLine(player));

            var LineData = await OrmManager.JobBus_LineManager.QueryAll();

            foreach (OrmManager.JobBus_LineManager LineManager in LineData)
            {
                panel.AddTabLine(LineManager.LineName, "", ItemUtils.GetIconIdByItemId(1112), _ => { LinePlayable_ShowInfo(player, LineManager); });
            }

            panel.AddButton("Choisir", (ui) => { panel.SelectTab(); });
            panel.PreviousButton();
            panel.CloseButton();
            panel.Display();
        }

        public async void LinePlayable_ShowInfo(Player player, OrmManager.JobBus_LineManager LineManager)
        {
            Panel panel = Context.PanelHelper.Create($"SAE | Ligne \"{LineManager.LineName}\"", UIPanel.PanelType.TabPrice, player, () => LinePlayable_ShowInfo(player, LineManager));

            int[] data = JsonConvert.DeserializeObject<int[]>(LineManager.BusStopID);

            var dataList = data.ToList();

            int index = 1;

            foreach (var BusStopData in dataList)
            {
                var BusStop = await OrmManager.JobBus_BusStopManager.Query(BusStopData);

                panel.AddTabLine(BusStop.BusStopName, "Ordre : " + index, ItemUtils.GetIconIdByItemId(1112), _ => { });

                index++;
            }
            panel.AddButton($"{TextFormattingHelper.Size("Choisir cette ligne", 15)}", async (ui) =>
            {
                if (!PlayerNetID.Contains(player.setup.netId) && !DataManager.busLineDictionary.ContainsKey(player.setup.netId))
                {
                    player.ClosePanel(ui);
                    PlayerNetID.Add(player.setup.netId);
                    await LinePlayable_Start(player, LineManager);
                }
                else
                {
                    player.ClosePanel(ui);
                    LinePlayable_StopLine(player, LineManager);
                }
            });
            panel.PreviousButton();
            panel.CloseButton();
            panel.Display();
        }

        public void LinePlayable_StopLine(Player player, OrmManager.JobBus_LineManager LineManager)
        {
            Panel panel = Context.PanelHelper.Create($"SAE | Ligne \"{LineManager.LineName}\"", UIPanel.PanelType.Text, player, () => LinePlayable_StopLine(player, LineManager));
            panel.TextLines.Add($"Vous effectuez déja la ligne \"{LineManager.LineName}\" ! Souhaitez vous déposer les passagers sur le trottoir ?");

            panel.AddButton("Oui", (ui) =>
            {
                player.ClosePanel(ui);
                PlayerNetID.Remove(player.setup.netId);
                DataManager.BusDriver_StopLine(player.setup.netId);
                player.DestroyAllVehicleCheckpoint();
                player.setup.TargetDisableNavigation();

                uint bus = player.GetVehicleId();
                Vehicle vehicle = NetworkServer.spawned[bus].GetComponent<Vehicle>();

                vehicle.bus.NetworkgirouetteText = "";
                vehicle.bus.NetworkrightText = "";
                vehicle.bus.Networkline = "";
            });

            panel.AddButton("Non", (ui) =>
            {
                player.ClosePanel(ui);
            });

            panel.PreviousButton();
            panel.CloseButton();
            panel.Display();
        }

        public void RemoveNetIDFromList(int ConnID)
        {
            PlayerNetID.Remove((uint)ConnID);
            DataManager.busLineDictionary.Remove((uint)ConnID);
        }

        public async Task LinePlayable_Start(Player player, OrmManager.JobBus_LineManager LineManager)
        {
            int[] data = JsonConvert.DeserializeObject<int[]>(LineManager.BusStopID);
            var dataList = data.ToList();

            List<Vector3> positions = new List<Vector3>();

            List<String> BusStopName = new List<String>();

            int TotalBusStopNumber = 0;

            foreach (var Data in dataList)
            {
                var BusStopData = await OrmManager.JobBus_BusStopManager.Query(Data);
                positions.Add(new Vector3(BusStopData.PositionX, BusStopData.PositionY, BusStopData.PositionZ));

                BusStopName.Add(BusStopData.BusStopName);

                TotalBusStopNumber++;
            }

            DataManager.BusDriver_StartLine(player, LineManager, BusStopName[0], TotalBusStopNumber);

            if (positions.Count == 0)
            {
                player.Notify("SAE", $"Erreur, aucun arrêt n'a été trouvé sur la ligne de bus \"{LineManager.LineName}\"", NotificationManager.Type.Error);
                return;
            }

            NVehicleCheckpoint[] checkpoints = new NVehicleCheckpoint[positions.Count];

            for (int i = 0; i < positions.Count; i++)
            {
                if (player.GetVehicleModel() == "Euro Lion's City 12" || player.GetVehicleModel() == "Fast Scoler 4")
                {
                    uint bus = player.GetVehicleId();
                    Vehicle vehicle = NetworkServer.spawned[bus].GetComponent<Vehicle>();

                    int currentIndex = i;

                    checkpoints[i] = new NVehicleCheckpoint(player.netId, positions[i], async (c, vId) =>
                    {

                        player.Notify("SAE", $"Arrêt de bus : \"{BusStopName[currentIndex]}\"", NotificationManager.Type.Info);

                        DataManager.BusDriver_BusStop(player.setup.netId, BusStopName[currentIndex], currentIndex);

                        while (!vehicle.bus.HasAnyDoorOpened() || !vehicle.bus.NetworkisKneelDown)
                        {
                            await Task.Delay(500);
                        }
                        player.DestroyVehicleCheckpoint(c);
                        player.setup.TargetDisableNavigation();
                        player.Notify("SAE", "Les clients montent/descendent du bus..", NotificationManager.Type.Info, 5f);
                        player.setup.NetworkisFreezed = true;

                        await Task.Delay(5000);

                        ReceiveMoney(player);

                        while (vehicle.bus.HasAnyDoorOpened() || vehicle.bus.NetworkisKneelDown)
                        {
                            await Task.Delay(500);
                        }

                        player.setup.NetworkisFreezed = false;

                        if (currentIndex < positions.Count - 1)
                        {
                            player.CreateVehicleCheckpoint(checkpoints[currentIndex + 1]);
                            player.setup.TargetSetGPSTarget(positions[currentIndex + 1]);
                            player.Notify("SAE", $"Prochain arrêt de bus : \"{BusStopName[currentIndex + 1]}\"", NotificationManager.Type.Info);

                            vehicle.bus.NetworkrightText = BusStopName[currentIndex + 1];

                        }
                        else
                        {
                            vehicle.bus.NetworkgirouetteText = "";
                            vehicle.bus.NetworkrightText = "";
                            vehicle.bus.Networkline = "";
                            player.Notify("SAE", $"Vous êtes au terminus de la ligne de bus \"{LineManager.LineName}\"", NotificationManager.Type.Success);
                            PlayerNetID.Remove(player.setup.netId);
                            DataManager.BusDriver_StopLine(player.setup.netId);
                        }
                    });
                }
                else
                {
                    player.Notify("SAE", "Vous devez être dans un bus afin de pouvoir utiliser le SAE !", NotificationManager.Type.Error);
                }
            }

            if (checkpoints.Length > 0)
            {
                player.CreateVehicleCheckpoint(checkpoints[0]);
                player.setup.TargetSetGPSTarget(positions[0]);
                uint bus = player.GetVehicleId();
                Vehicle vehicle = NetworkServer.spawned[bus].GetComponent<Vehicle>();

                player.Notify("SAE", $"Démarrage de la ligne de bus {LineManager.LineName}, bon courage !", NotificationManager.Type.Success, 10f);

                player.SendText($"{TextFormattingHelper.Color("[ASTUCES]", TextFormattingHelper.Colors.Orange)} Lorsque vous êtes à un {TextFormattingHelper.Color("arrêt de bus", TextFormattingHelper.Colors.Info)}, n'oubliez pas {TextFormattingHelper.Color("d'agenouiller et d'ouvrir les portes du bus !", TextFormattingHelper.Colors.Orange)}");
                vehicle.bus.NetworkgirouetteText = "Destination\n" + BusStopName[BusStopName.Count - 1];
                vehicle.bus.NetworkrightText = BusStopName[0];
                vehicle.bus.Networkline = LineManager.LineNumber;
            }
        }

        public void ReceiveMoney(Player player)
        {
            int customerCount = UnityEngine.Random.Range(Main.Main._JobBusConfig.MinCustomerPerBusStop, Main.Main._JobBusConfig.MaxCustomerPerBusStop);
            float totalMoney = 0f;

            for (int i = 0; i < customerCount; i++)
            {
                float money = UnityEngine.Random.Range(Main.Main._JobBusConfig.MinMoneyPerCustomer, Main.Main._JobBusConfig.MaxMoneyPerCustomer + 1);
                totalMoney += money;
            }

            float taxPercentage = Main.Main._JobBusConfig.TaxPercentage;
            float PlayerReceivePercentage = Main.Main._JobBusConfig.PlayerReceivePercentage;

            float cityHallMoney = totalMoney * (taxPercentage / 100f);

            float playerMoney = totalMoney * (PlayerReceivePercentage / 100f); ;

            float BusMoney = totalMoney - cityHallMoney - playerMoney;

            if (Nova.biz.FetchBiz(Main.Main._JobBusConfig.CityHallId) != null)
            {
                Nova.biz.FetchBiz(Main.Main._JobBusConfig.CityHallId).Bank += Math.Round(cityHallMoney, 2);
                Nova.biz.FetchBiz(Main.Main._JobBusConfig.CityHallId).Save();
            }

            player.biz.Bank += Math.Round(BusMoney, 2);
            player.biz.Save();

            player.AddBankMoney(Math.Round(playerMoney, 2));
            player.character.Save();

            player.Notify("Gains", $"Vous venez de reçevoir {TextFormattingHelper.Color(Math.Round(playerMoney, 2).ToString(), TextFormattingHelper.Colors.Orange)}€", NotificationManager.Type.Info);
        }
    }
}