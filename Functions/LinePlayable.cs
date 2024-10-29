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
        public ModKit.ModKit Context { get; set; }

        public void MainPanel(Player player)
        {
            Console.WriteLine(player.GetVehicleId());

            if (player.GetVehicleId() == 0 || player.GetVehicleId() == 0)
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
                player.ClosePanel(ui);
                await LinePlayable_Start(player, LineManager);
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
                if (player.GetVehicleId() == 0 || player.GetVehicleId() == 0)
                {
                    uint bus = player.GetVehicleId();
                    Vehicle vehicle = NetworkServer.spawned[bus].GetComponent<Vehicle>();

                    int currentIndex = i;

                    checkpoints[i] = new NVehicleCheckpoint(player.netId, positions[i], async (c, vId) =>
                    {
                        player.Notify("SAE", $"Arrêt de bus : \"{BusStopName}\"", NotificationManager.Type.Info);
                        player.DestroyVehicleCheckpoint(c);
                        player.setup.TargetDisableNavigation();
                        await Task.Delay(1000);


                        if (currentIndex < positions.Count - 1)
                        {
                            player.CreateVehicleCheckpoint(checkpoints[currentIndex + 1]);
                            player.setup.TargetSetGPSTarget(positions[currentIndex + 1]);
                            player.Notify("SAE", $"Prochain arrêt de bus : \"{BusStopName[currentIndex + 1]}\"", NotificationManager.Type.Info);


                            vehicle.bus.rightText = BusStopName[currentIndex + 1];


                        }
                        else
                        {
                            player.Notify("SAE", $"Vous êtes au terminus de la ligne de bus \"{LineManager.LineName}\"", NotificationManager.Type.Success);
                        }


                        // chaque passage a
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

                vehicle.bus.girouetteText = "Destination";
                vehicle.bus.rightText = BusStopName[1];
            }
        }

    }
}
