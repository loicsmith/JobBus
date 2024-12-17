using Life.Network;
using Life.UI;
using ModKit.Helper;
using ModKit.Utils;

namespace JobBus.Functions
{
    internal class LineViewer
    {

        public ModKit.ModKit Context { get; set; }

        public void MainPanel(Player player)
        {
            Panel panel = Context.PanelHelper.Create("Arrêt de bus - Informations voyageurs", UIPanel.PanelType.TabPrice, player, () => MainPanel(player));

            if (DataManager.Instance.busLineDictionary.Count == 0)
            {
                panel.AddTabLine("Aucune ligne en cours", "", ItemUtils.GetIconIdByItemId(1012), _ => { });
            }
            else
            {
                foreach (var entry in DataManager.Instance.busLineDictionary)
                {
                    BusLineInfo lineInfo = entry.Value;
                    panel.AddTabLine($"{TextFormattingHelper.Size($"{lineInfo.LineName}", 15)}\n{TextFormattingHelper.Color(TextFormattingHelper.Size(TextFormattingHelper.LineHeight($"Arrêt actuel : {lineInfo.CurrentBusStopName}\nProchain arrêt : {lineInfo.NextBusStopName}", 15), 15), TextFormattingHelper.Colors.Purple)}", $"Arrêt {lineInfo.CurrentBusStopNumber}/{lineInfo.TotalBusStops}", ItemUtils.GetIconIdByItemId(1012), _ => { });
                }
            }
            panel.CloseButton();
            panel.Display();
        }
    }
}
