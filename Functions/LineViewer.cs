using Life.Network;
using Life.UI;
using ModKit.Helper;
using ModKit.Utils;

namespace MODRP_JobBus.Functions
{
    internal class LineViewer
    {
        public DataManager DataManager = new DataManager();

        public ModKit.ModKit Context { get; set; }

        // MainPanel pas utilisé le temps du bug
        public void MainPanel(Player player)
        {
            Panel panel = Context.PanelHelper.Create("Arrêt de bus - Informations voyageurs", UIPanel.PanelType.TabPrice, player, () => MainPanel(player));

            foreach (var entry in DataManager.busLineDictionary)
            {
                BusLineInfo lineInfo = entry.Value;

                panel.AddTabLine($"{TextFormattingHelper.Size($"{lineInfo.LineNumber}", 15)}\n{TextFormattingHelper.Color(TextFormattingHelper.Size(TextFormattingHelper.LineHeight($"Arrêt actuel : {lineInfo.CurrentBusStopNumber} - Prochain arrêt : {lineInfo.NextBusStopName}", 15), 15), TextFormattingHelper.Colors.Purple)}", $"{lineInfo.CurrentBusStopNumber}/{lineInfo.TotalBusStops}", ItemUtils.GetIconIdByItemId(1012), _ => { });
            }
            panel.CloseButton();
            panel.Display();
        }
    }
}
