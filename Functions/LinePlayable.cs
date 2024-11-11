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
        public List<uint> PlayerNetID = new List<uint>();

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
                panel.NextButton($"{TextFormattingHelper.Size("Ventes", 20)}", () => { LinePlayable_TicketsBuy_List(player); });

                panel.AddButton("Retour", ui => AAMenu.AAMenu.menu.BizPanel(player));
                panel.CloseButton();
                panel.Display();
            }
            else
            {
                player.Notify("SAE", "Vous devez être dans un bus afin de pouvoir utiliser le SAE !", NotificationManager.Type.Error);
            }

        }
        public void LinePlayable_TicketsBuy_List(Player player)
        {
            Panel panel = Context.PanelHelper.Create("SAE | Ventes de tickets", UIPanel.PanelType.TabPrice, player, () => LinePlayable_TicketsBuy_List(player));

            foreach (Player p in Nova.server.GetPlayersInRange(5f, player.setup.transform.position))
            {
                // if (player.netId == p.netId) { }
                //else
                //  {
                panel.AddTabLine(p.character.Firstname, "", ItemUtils.GetIconIdByItemId(1112), (ui) =>
                {
                    LinePlayable_TicketsBuy_ChoiceTicket(player, p);
                });
                //}
            }
            panel.NextButton("Sélectionner", () =>
            {
                panel.SelectTab();
            });
            panel.PreviousButton();
            panel.CloseButton();
            panel.Display();
        }

        public void LinePlayable_TicketsBuy_ChoiceTicket(Player player, Player CustomerPlayer)
        {
            Panel panel = Context.PanelHelper.Create("SAE | Choix du ticket", UIPanel.PanelType.Text, player, () => LinePlayable_TicketsBuy_ChoiceTicket(player, CustomerPlayer));

            panel.TextLines.Add($"Client : {CustomerPlayer.character.Firstname}");
            panel.NextButton("Tickets Heure", () =>
            {
                LinePlayable_TicketsBuy_BuyTicket(player, CustomerPlayer, "Heure");
            });
            panel.NextButton("Tickets Journée", () =>
            {
                LinePlayable_TicketsBuy_BuyTicket(player, CustomerPlayer, "Journée");
            });
            panel.NextButton("Tickets Montée", () =>
            {
                LinePlayable_TicketsBuy_BuyTicket(player, CustomerPlayer, "Mensuel");
            });
            panel.PreviousButton();
            panel.CloseButton();
            panel.Display();
        }

        public void LinePlayable_TicketsBuy_BuyTicket(Player player, Player CustomerPlayer, string Param)
        {
            Panel panel = Context.PanelHelper.Create("SAE | Impression du ticket", UIPanel.PanelType.Text, player, () => LinePlayable_TicketsBuy_BuyTicket(player, CustomerPlayer, Param));

            panel.TextLines.Add($"Client : {CustomerPlayer.character.Firstname}");
            panel.TextLines.Add($"Type : Ticket {Param}");

            switch (Param)
            {
                case "Heure":
                    panel.AddButton("Impression", (ui) =>
                    {
                        player.ClosePanel(ui);
                        LinePlayable_TicketsBuy_BuyTicketValidate(player, CustomerPlayer, "Heure", Main.Main._JobBusConfig.TicketHeure);
                    });
                    break;
                case "Journée":
                    panel.AddButton("Impression", (ui) =>
                    {
                        player.ClosePanel(ui);
                        LinePlayable_TicketsBuy_BuyTicketValidate(player, CustomerPlayer, "Journée", Main.Main._JobBusConfig.TicketJournée);
                    });
                    break;
                case "Mensuel":
                    panel.AddButton("Impression", (ui) =>
                    {
                        player.ClosePanel(ui);
                        LinePlayable_TicketsBuy_BuyTicketValidate(player, CustomerPlayer, "Mensuel", Main.Main._JobBusConfig.TicketMensuel);
                    });
                    break;
            }
            panel.PreviousButton();
            panel.CloseButton();
            panel.Display();
        }

        public void LinePlayable_TicketsBuy_BuyTicketValidate(Player player, Player CustomerPlayer, string Param, double Price)
        {
            Panel panel = Context.PanelHelper.Create("Impression du ticket", UIPanel.PanelType.Text, CustomerPlayer, () => LinePlayable_TicketsBuy_BuyTicketValidate(player, CustomerPlayer, Param, Price));
            panel.TextLines.Add($"{TextFormattingHelper.Align(TextFormattingHelper.Bold("Achat d'un ticket"), TextFormattingHelper.Aligns.Center)}");
            panel.TextLines.Add($"{TextFormattingHelper.Bold("Ticket : ")}" + $"{Param}");
            panel.TextLines.Add($"{TextFormattingHelper.Bold("Prix : ")}" + $"{Price}");
            panel.AddButton($"Payer", (ui) =>
            {
                if (CustomerPlayer.character.Money >= Price)
                {
                    player.biz.Bank += Price;
                    player.biz.Save();

                    CustomerPlayer.character.Money -= Price;

                    player.ClosePanel(ui);

                    player.Notify("Gains", $"Vous venez de reçevoir {TextFormattingHelper.Color(Price.ToString(), TextFormattingHelper.Colors.Orange)}€", NotificationManager.Type.Info);
                    CustomerPlayer.Notify("Ticket", $"Vous venez d'acheter un ticket {Param} pour {Price}€");
                }
                else
                {
                    CustomerPlayer.Notify("Pas assez d'argent !", $"Vous n'avez pas {Price}€ dans vos poches !", NotificationManager.Type.Error);
                }
            });

            panel.CloseButton();
            panel.Display();
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
                if (!PlayerNetID.Contains(player.setup.netId) && !DataManager.Instance.busLineDictionary.ContainsKey(player.setup.netId))
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
                DataManager.Instance.BusDriver_StopLine(player.setup.netId);
                player.DestroyAllVehicleCheckpoint();
                player.setup.TargetDisableNavigation();

                uint bus = player.GetVehicleId();
                Vehicle vehicle = NetworkServer.spawned[bus].GetComponent<Vehicle>();

                vehicle.bus.NetworkgirouetteText = "";
                vehicle.bus.NetworkrightText = "";
                vehicle.bus.Networkline = "";
                vehicle.bus.NetworkbusColor = Color.black;
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
            DataManager.Instance.busLineDictionary.Remove((uint)ConnID);
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

            DataManager.Instance.BusDriver_StartLine(player, LineManager, BusStopName[0], TotalBusStopNumber, BusStopName[1]);
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

                        while (!vehicle.bus.HasAnyDoorOpened() || !vehicle.bus.NetworkisKneelDown)
                        {
                            await Task.Delay(500);
                        }

                        string nextBusStop = (currentIndex + 1 < BusStopName.Count) ? BusStopName[currentIndex + 1] : "Aucun";
                        DataManager.Instance.BusDriver_BusStop(player.setup.netId, BusStopName[currentIndex], currentIndex + 1, nextBusStop);

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
                            vehicle.bus.NetworkbusColor = Color.black;

                            player.Notify("SAE", $"Vous êtes au terminus de la ligne de bus \"{LineManager.LineName}\"", NotificationManager.Type.Success);
                            PlayerNetID.Remove(player.setup.netId);
                            DataManager.Instance.BusDriver_StopLine(player.setup.netId);
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
                if (ColorUtility.TryParseHtmlString(LineManager.LineColor, out Color color))
                {
                    vehicle.bus.NetworkbusColor = color;
                }
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