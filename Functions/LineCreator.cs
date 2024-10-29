using Life;
using Life.Network;
using Life.UI;
using ModKit.Helper;
using ModKit.Utils;
using Newtonsoft.Json;
using System.Linq;

namespace MODRP_JobBus.Functions
{
    internal class LineCreator
    {
        public ModKit.ModKit Context { get; set; }

        public void MainPanel(Player player)
        {
            Panel panel = Context.PanelHelper.Create("LineCreator | Accueil", UIPanel.PanelType.Text, player, () => MainPanel(player));
            panel.TextLines.Add("Veuillez sélectionner une catégorie afin de poursuivre");
            panel.NextButton("Gestion Ligne", () => LineManager_Main(player));
            panel.NextButton("Gestion Arrêt", () => BusStopManager_Main(player));

            panel.AddButton("Retour", ui => AAMenu.AAMenu.menu.AdminPanel(player));
            panel.CloseButton();
            panel.Display();
        }

        #region Line Manager
        public async void LineManager_Main(Player player)
        {
            Panel panel = Context.PanelHelper.Create("LineCreator | Line Manager", UIPanel.PanelType.TabPrice, player, () => LineManager_Main(player));

            var LineData = await OrmManager.JobBus_LineManager.QueryAll();

            foreach (OrmManager.JobBus_LineManager Line in LineData)
            {
                panel.AddTabLine(Line.LineName, "ID : " + Line.Id, ItemUtils.GetIconIdByItemId(1112), _ => { LineManager_MoreOptions(player, Line); });
            }
            panel.NextButton("Ajouter", () => LineManager_Add(player));
            panel.NextButton("Options", () => panel.SelectTab());

            panel.PreviousButton();
            panel.CloseButton();
            panel.Display();
        }

        public void LineManager_Add(Player player)
        {
            Panel panel = Context.PanelHelper.Create("LineCreator | Add Line", UIPanel.PanelType.Input, player, () => LineManager_Add(player));
            panel.TextLines.Add("Veuillez saisir un nom afin de créer une ligne de bus");
            panel.SetInputPlaceholder("Nom de la ligne de bus..");

            panel.AddButton("Valider", async (ui) =>
            {
                string _LineName = ui.inputText;

                int[] dataArray = { };

                string JsonData = JsonConvert.SerializeObject(dataArray);

                OrmManager.JobBus_LineManager instance = new OrmManager.JobBus_LineManager { LineName = _LineName, BusStopID = JsonData };
                var result = await instance.Save();

                if (result)
                {
                    player.Notify("LineCreator", $"La ligne de bus portant le nom de \"{_LineName}\" vient d'être crée", NotificationManager.Type.Success);
                }
                else
                {
                    player.Notify("LineCreator", $"Une erreur est survenue lors de la création de la ligne de bus portant le nom de \"{_LineName}\"", NotificationManager.Type.Error);
                }
                player.ClosePanel(ui);
                LineManager_Main(player);
            });
            panel.PreviousButton();
            panel.CloseButton();
            panel.Display();
        }

        public void LineManager_MoreOptions(Player player, OrmManager.JobBus_LineManager LineManager)
        {
            Panel panel = Context.PanelHelper.Create("LineCreator |  More Options", UIPanel.PanelType.Text, player, () => LineManager_MoreOptions(player, LineManager));
            panel.TextLines.Add($"Nom de la ligne : \"{LineManager.LineName}\"");
            panel.TextLines.Add($"ID de la ligne : \"{LineManager.Id}\"");

            panel.AddButton($"{TextFormattingHelper.Size("Liste arrêt de bus", 15)}", (ui) =>
            {
                player.ClosePanel(ui);
                LineManager_ListBusStop(player, LineManager);
            });
            panel.AddButton($"{TextFormattingHelper.Size("Supprimer Ligne", 15)}", (ui) =>
            {
                player.ClosePanel(ui);
                LineManager_Delete(player, LineManager);
            });
            panel.PreviousButton();
            panel.CloseButton();
            panel.Display();
        }

        public async void LineManager_ListBusStop(Player player, OrmManager.JobBus_LineManager LineManager)
        {
            Panel panel = Context.PanelHelper.Create($"LineCreator | Ligne \"{LineManager.LineName}\"", UIPanel.PanelType.TabPrice, player, () => LineManager_ListBusStop(player, LineManager));

            int[] data = JsonConvert.DeserializeObject<int[]>(LineManager.BusStopID);

            var dataList = data.ToList();
            foreach (var BusStopData in dataList)
            {
                var BusStop = await OrmManager.JobBus_BusStopManager.Query(BusStopData);

                panel.AddTabLine(BusStop.BusStopName, "ID : " + BusStop.Id, ItemUtils.GetIconIdByItemId(1112), _ => { BusStopLineManager_JSON(player, LineManager, BusStop.Id, false); });
            }

            panel.AddButton("Ajouter", (ui) =>
            {
                player.ClosePanel(ui);
                BusStopLineManager_Add(player, LineManager);
            });
            panel.AddButton("Supprimer", (ui) =>
            {
                panel.SelectTab();
                player.ClosePanel(ui);
                LineManager_ListBusStop(player, LineManager);
            });

            panel.PreviousButton();
            panel.CloseButton();
            panel.Display();
        }

        public void LineManager_Delete(Player player, OrmManager.JobBus_LineManager LineManager)
        {
            Panel panel = Context.PanelHelper.Create("LineCreator | Delete Line", UIPanel.PanelType.Text, player, () => LineManager_Delete(player, LineManager));
            panel.TextLines.Add($"Veuillez confirmer la suppresion de la ligne de bus : \"{LineManager.LineName}\"");

            panel.NextButton("Valider", async () =>
            {
                var result = await LineManager.Delete();

                if (result)
                {
                    player.Notify("LineCreator", $"La ligne de bus portant le nom de \"{LineManager.LineName}\" vient d'être supprimé", NotificationManager.Type.Success);
                }
                else
                {
                    player.Notify("LineCreator", $"Une erreur est survenue lors de la suppression de la ligne de bus portant le nom de \"{LineManager.LineName}\"", NotificationManager.Type.Error);
                }
                LineManager_Main(player);
            });
            panel.PreviousButton();
            panel.CloseButton();
            panel.Display();
        }

        #endregion

        #region Bus Stop Manager
        public async void BusStopManager_Main(Player player)
        {
            Panel panel = Context.PanelHelper.Create("LineCreator | Bus Stop Manager", UIPanel.PanelType.TabPrice, player, () => BusStopManager_Main(player));

            var LineData = await OrmManager.JobBus_BusStopManager.QueryAll();

            foreach (OrmManager.JobBus_BusStopManager Line in LineData)
            {
                panel.AddTabLine(Line.BusStopName, "ID : " + Line.Id, ItemUtils.GetIconIdByItemId(1112), _ => { BusStopManager_MoreOptions(player, Line); });
            }

            panel.NextButton("Ajouter", () => BusStopManager_AddBusStop(player));
            panel.NextButton("Options", () => panel.SelectTab());
            panel.PreviousButton();
            panel.CloseButton();
            panel.Display();
        }

        public void BusStopManager_AddBusStop(Player player)
        {
            Panel panel = Context.PanelHelper.Create("LineCreator | Add Bus Stop", UIPanel.PanelType.Input, player, () => BusStopManager_AddBusStop(player));
            panel.TextLines.Add("Veuillez saisir un nom d'arrêt de bus");
            panel.SetInputPlaceholder("Nom de l'arrêt de bus..");

            panel.AddButton("Suivant", (ui) =>
            {
                string BusStopName = ui.inputText;
                player.ClosePanel(ui);
                BusStopManager_AddBusStop_Position(player, BusStopName, 0);
            });
            panel.PreviousButton();
            panel.CloseButton();
            panel.Display();
        }

        public void BusStopManager_AddBusStop_Position(Player player, string _BusStopName, int _BusStopLineId)
        {
            Panel panel = Context.PanelHelper.Create("LineCreator | Bus Stop Position", UIPanel.PanelType.Text, player, () => BusStopManager_AddBusStop_Position(player, _BusStopName, _BusStopLineId));

            float PosX = player.setup.transform.position.x;
            float PosY = player.setup.transform.position.y;
            float PosZ = player.setup.transform.position.z;

            panel.TextLines.Add("Veuillez confirmer la position de l'arrêt de bus");
            panel.TextLines.Add("Position X : " + PosX);
            panel.TextLines.Add("Position Y : " + PosY);
            panel.TextLines.Add("Position Z : " + PosZ);

            panel.AddButton("Confirmer", async (ui) =>
            {

                OrmManager.JobBus_BusStopManager instance = new OrmManager.JobBus_BusStopManager { BusStopName = _BusStopName, PositionX = PosX, PositionY = PosY, PositionZ = PosZ };
                var result = await instance.Save();

                if (result)
                {
                    player.Notify("LineCreator", $"L'arrêt de bus portant le nom de \"{_BusStopName}\" vient d'être crée", NotificationManager.Type.Success);
                }
                else
                {
                    player.Notify("LineCreator", $"Une erreur est survenue lors de la création de l'arrêt de bus portant le nom de \"{_BusStopName}\"", NotificationManager.Type.Error);
                }
                player.ClosePanel(ui);
                BusStopManager_Main(player);
            });
            panel.PreviousButton();
            panel.CloseButton();
            panel.Display();
        }

        public void BusStopManager_MoreOptions(Player player, OrmManager.JobBus_BusStopManager BusStopManager)
        {
            Panel panel = Context.PanelHelper.Create("LineCreator | More Options", UIPanel.PanelType.Text, player, () => BusStopManager_MoreOptions(player, BusStopManager));

            panel.TextLines.Add($"{TextFormattingHelper.Bold(TextFormattingHelper.Align("Informations :", TextFormattingHelper.Aligns.Center))}");
            panel.TextLines.Add("Nom de l'arrêt : " + BusStopManager.BusStopName);
            panel.TextLines.Add("ID : " + BusStopManager.Id);
            panel.TextLines.Add("Position X : " + BusStopManager.PositionX);
            panel.TextLines.Add("Position Y : " + BusStopManager.PositionY);
            panel.TextLines.Add("Position Z : " + BusStopManager.PositionZ);

            panel.AddButton("S'y Téléporter", (ui) =>
            {
                player.setup.TargetSetPosition(new UnityEngine.Vector3(BusStopManager.PositionX, BusStopManager.PositionY, BusStopManager.PositionZ));
                player.Notify("LineCreator", $"Téléportation vers l'arrêt de bus \"{BusStopManager.BusStopName}\"", NotificationManager.Type.Success);
            });

            panel.AddButton("Supprimer", (ui) =>
            {
                player.ClosePanel(ui);
                BusStopManager_Delete(player, BusStopManager);
            });

            panel.PreviousButton();
            panel.CloseButton();
            panel.Display();
        }

        public void BusStopManager_Delete(Player player, OrmManager.JobBus_BusStopManager BusStopManager)
        {
            Panel panel = Context.PanelHelper.Create("LineCreator | Delete Line", UIPanel.PanelType.Text, player, () => BusStopManager_Delete(player, BusStopManager));
            panel.TextLines.Add($"Veuillez confirmer la supression de l'arrêt de bus : \"{BusStopManager.BusStopName}\"");

            panel.AddButton("Valider", async (ui) =>
            {
                var result = await BusStopManager.Delete();

                if (result)
                {
                    BusStopLineManager_DeleteWhenBusStopIsDeleted(player, BusStopManager.Id);
                    player.Notify("LineCreator", $"L'arrêt de bus portant le nom de \"{BusStopManager.BusStopName}\" vient d'être supprimé", NotificationManager.Type.Success);
                }
                else
                {
                    player.Notify("LineCreator", $"Une erreur est survenue lors de la suppression de l'arrêt de bus portant le nom de \"{BusStopManager.BusStopName}\"", NotificationManager.Type.Error);
                }
                player.ClosePanel(ui);
                BusStopManager_Main(player);
            });
            panel.PreviousButton();
            panel.CloseButton();
            panel.Display();
        }
        #endregion

        #region Bus Stop <> Line Manager

        public async void BusStopLineManager_Add(Player player, OrmManager.JobBus_LineManager LineManager)
        {
            Panel panel = Context.PanelHelper.Create("LineCreator | Bus Stop <> Line Manager", UIPanel.PanelType.TabPrice, player, () => BusStopLineManager_Add(player, LineManager));

            var Data = await OrmManager.JobBus_BusStopManager.QueryAll();

            foreach (OrmManager.JobBus_BusStopManager BusStop in Data)
            {
                panel.AddTabLine(BusStop.BusStopName, "ID : " + BusStop.Id, ItemUtils.GetIconIdByItemId(1112), _ => { BusStopLineManager_JSON(player, LineManager, BusStop.Id, true); });
            }

            panel.AddButton($"{TextFormattingHelper.Size("Ajouter arrêt sélectionné", 10)}", (ui) =>
            {
                player.ClosePanel(ui);
                panel.SelectTab();
                LineManager_ListBusStop(player, LineManager);
            });
            panel.PreviousButton();
            panel.CloseButton();
            panel.Display();
        }

        public async void BusStopLineManager_DeleteWhenBusStopIsDeleted(Player player, int BusStopID)
        {
            var LineData = await OrmManager.JobBus_LineManager.QueryAll();

            foreach(OrmManager.JobBus_LineManager LineManager in LineData)
            {
                BusStopLineManager_JSON(player, LineManager, BusStopID, false);
            }
        }

        public async void BusStopLineManager_JSON(Player player, OrmManager.JobBus_LineManager LineManager, int BusStopID, bool AddOrRemove)
        {
            int[] data = JsonConvert.DeserializeObject<int[]>(LineManager.BusStopID);

            var dataList = data.ToList();

            if (AddOrRemove)
            {
                if (!dataList.Contains(BusStopID))
                {
                    dataList.Add(BusStopID);
                    player.Notify("LineCreator", $"L'arrêt de bus ayant pour ID \"{BusStopID}\" vient d'être ajouté à la ligne de bus \"{LineManager.LineName}\"", NotificationManager.Type.Success);
                }
                else
                {
                    player.Notify("LineCreator", $"L'arrêt de bus ayant pour ID \"{BusStopID}\" existe déjà la ligne de bus \"{LineManager.LineName}\" !", NotificationManager.Type.Error);
                }
            }
            else
            {
                if (dataList.Contains(BusStopID))
                {
                    dataList.Remove(BusStopID);
                    player.Notify("LineCreator", $"L'arrêt de bus ayant pour ID \"{BusStopID}\" vient d'être retiré de la ligne de bus \"{LineManager.LineName}\"");
                }
                else
                {
                    player.Notify("LineCreator", $"L'arrêt de bus ayant pour ID \"{BusStopID}\" n'existe pas pour la ligne de bus \"{LineManager.LineName}\" !", NotificationManager.Type.Error);
                }
            }

            data = dataList.ToArray();
            string updatedJson = JsonConvert.SerializeObject(data, Formatting.None);

            LineManager.BusStopID = updatedJson;
            await LineManager.Save();

        }
        #endregion

    }
}