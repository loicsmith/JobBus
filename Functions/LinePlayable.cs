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
        bool LineInProgress = false;

        public ModKit.ModKit Context { get; set; }

        public void MainPanel(Player player)
        {
            if (player.GetVehicleModel() == "Euro Lion's City 12" || player.GetVehicleModel() == "Fast Scoler 4")
            {
                Panel panel = Context.PanelHelper.Create("SAE | Accueil", UIPanel.PanelType.Text, player, () => MainPanel(player));

                panel.TextLines.Add($"{TextFormattingHelper.Bold(TextFormattingHelper.Align("Informations :", TextFormattingHelper.Aligns.Center))}");

                panel.TextLines.Add("Configuration : Prise de service d'une ligne");
                panel.TextLines.Add("Ventes : Permet de vendre des tickets");

                panel.NextButton("Configuration", () => { LinePlayable_AvailableLine(player); });
                panel.NextButton("Ventes", () => { });

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
            panel.AddButton("Choisir cette ligne", async (ui) =>
            {
                if (LineInProgress == false)
                {
                    player.ClosePanel(ui);
                    LineInProgress = true;
                    await LinePlayable_Start(player, LineManager);
                }
                else
                {
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
            panel.TextLines.Add($"Vous effectuez déja la ligne {LineManager.LineName} ! Souhaitez vous déposer les passagers sur le trottoir ?");

            panel.AddButton("Oui", (ui) =>
            {
                player.ClosePanel(ui);
                LineInProgress = false;
                player.DestroyAllVehicleCheckpoint();
                player.setup.TargetDisableNavigation();
            });

            panel.AddButton("Non", (ui) =>
            {
                player.ClosePanel(ui);
            });
            panel.PreviousButton();
            panel.CloseButton();
            panel.Display();
        }

        public async Task LinePlayable_Start(Player player, OrmManager.JobBus_LineManager LineManager)
        {
            int[] data = JsonConvert.DeserializeObject<int[]>(LineManager.BusStopID);
            var dataList = data.ToList();

            List<Vector3> positions = new List<Vector3>();

            List<String> BusStopName = new List<String>();

            foreach (var Data in dataList)
            {
                var BusStopData = await OrmManager.JobBus_BusStopManager.Query(Data);
                positions.Add(new Vector3(BusStopData.PositionX, BusStopData.PositionY, BusStopData.PositionZ));

                BusStopName.Add(BusStopData.BusStopName);
            }

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
                        player.Notify("SAE - Astuces", "N'oubliez pas d'agenouiller et d'ouvrir les portes du bus !", NotificationManager.Type.Info, 5f);

                        while (!(vehicle.bus.HasAnyDoorOpened() && vehicle.bus.NetworkisKneelDown))
                        {
                            await Task.Delay(500);
                        }

                        player.DestroyVehicleCheckpoint(c);
                        player.setup.TargetDisableNavigation();
                        player.Notify("SAE", "Les clients montent/descendent du bus..", NotificationManager.Type.Info, 5f);
                        while (!(vehicle.bus.HasAnyDoorOpened() && vehicle.bus.NetworkisKneelDown))
                        {
                            await Task.Delay(500);
                            player.setup.NetworkisFreezed = true;
                        }
                        await Task.Delay(5000);
                        while (!(vehicle.bus.HasAnyDoorOpened() && vehicle.bus.NetworkisKneelDown))
                        {
                            await Task.Delay(500);
                            player.setup.NetworkisFreezed = false;
                        }


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
                            LineInProgress = false;
                        }


                        // a chaque passage

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

                player.Notify("SAE - Astuces", "Lorsque vous êtes à un arrêt de bus, n'oubliez pas d'agenouiller et d'ouvrir les portes du bus !", NotificationManager.Type.Info, 10f);
                vehicle.bus.NetworkgirouetteText = "Destination\n" + BusStopName[BusStopName.Count - 1];
                vehicle.bus.NetworkrightText = BusStopName[1];
                vehicle.bus.Networkline = LineManager.LineNumber;
            }
        }

    }
}
