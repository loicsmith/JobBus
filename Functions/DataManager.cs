using Life.Network;
using Life.UI;
using ModKit.Helper;
using System;
using System.Collections.Generic;

public class BusLineInfo
{
    public string LineName { get; set; }
    public string LineNumber { get; set; }
    public string CurrentBusStopName { get; set; }
    public int CurrentBusStopNumber { get; set; }
    public int TotalBusStops { get; set; }
    public string NextBusStopName { get; set; }

    public BusLineInfo(string lineName, string lineNumber, string currentBusStopName, int currentBusStopNumber, int totalBusStop, string nextBusStopName)
    {
        LineName = lineName;
        LineNumber = lineNumber;
        CurrentBusStopName = currentBusStopName;
        CurrentBusStopNumber = currentBusStopNumber;
        TotalBusStops = totalBusStop;
        NextBusStopName = currentBusStopName;
    }
}

namespace MODRP_JobBus.Functions
{
    internal class DataManager
    {
        public ModKit.ModKit Context { get; set; }

        public Dictionary<uint, BusLineInfo> busLineDictionary = new Dictionary<uint, BusLineInfo>();

        public void BusDriver_StartLine(Player player, OrmManager.JobBus_LineManager LineManager, string CurrentBusStopName, int TotalBusStopNumber, string nextBusStopName)
        {
            BusLineInfo newLineInfo = new BusLineInfo(LineManager.LineName, LineManager.LineNumber, CurrentBusStopName, 1, TotalBusStopNumber, nextBusStopName);
            busLineDictionary.Add(player.setup.netId, newLineInfo);
            foreach (var entry in busLineDictionary)
            {
                BusLineInfo lineInfo = entry.Value;

                uint lineId = entry.Key;
                Console.WriteLine($"NetID : {lineId}");
                Console.WriteLine($"Nom de la Ligne: {lineInfo.LineName}");
                Console.WriteLine($"Numéro de la Ligne: {lineInfo.LineNumber}");
                Console.WriteLine($"Arrêt actuel: {lineInfo.CurrentBusStopName}");
                Console.WriteLine($"Numéro de l'arrêt: {lineInfo.CurrentBusStopNumber}/{lineInfo.TotalBusStops}");
                Console.WriteLine("---------------------------");

            }
        }

        public void BusDriver_StopLine(uint PlayerNetID)
        {
            busLineDictionary.Remove(PlayerNetID);
        }

        public void BusDriver_BusStop(uint PlayerNetID, string BusStopName, int BusStopNumber, string NextBusStopName)
        {
            if (busLineDictionary.TryGetValue(PlayerNetID, out BusLineInfo LineInfo))
            {
                LineInfo.CurrentBusStopName = BusStopName;
                LineInfo.CurrentBusStopNumber = BusStopNumber;
                LineInfo.NextBusStopName = NextBusStopName;

                foreach (var entry in busLineDictionary)
                {

                    BusLineInfo lineInfo = entry.Value;

                    uint lineId = entry.Key;
                    Console.WriteLine($"NetID : {lineId}");
                    Console.WriteLine($"Nom de la Ligne: {lineInfo.LineName}");
                    Console.WriteLine($"Numéro de la Ligne: {lineInfo.LineNumber}");
                    Console.WriteLine($"Arrêt actuel: {lineInfo.CurrentBusStopName}");
                    Console.WriteLine($"Arrêt suivant: {lineInfo.NextBusStopName}");
                    Console.WriteLine($"Numéro de l'arrêt: {lineInfo.CurrentBusStopNumber}/{lineInfo.TotalBusStops}");
                    Console.WriteLine("---------------------------");

                }
            }
        }
        // MainPanel Tempo, ira dans LineViewer après
        public void MainPanel(Player player)
        {
            Panel panel = Context.PanelHelper.Create("Arrêt de bus - Informations voyageurs", UIPanel.PanelType.TabPrice, player, () => MainPanel(player));

            var allValues = busLineDictionary.Values;
            Console.WriteLine(allValues);
            Console.WriteLine(busLineDictionary.Count);


            foreach (var entry in allValues)
            {
                Console.WriteLine(entry.LineName);
                /*
                BusLineInfo lineInfo = entry.Value;
                panel.AddTabLine($"{TextFormattingHelper.Size($"{lineInfo.LineNumber}", 15)}\n{TextFormattingHelper.Color(TextFormattingHelper.Size(TextFormattingHelper.LineHeight($"Arrêt actuel : {lineInfo.CurrentBusStopNumber} - Prochain arrêt : {lineInfo.NextBusStopName}", 15), 15), TextFormattingHelper.Colors.Purple)}", $"{lineInfo.CurrentBusStopNumber}/{lineInfo.TotalBusStops}", ItemUtils.GetIconIdByItemId(1012), _ => { });
                */
            }
            panel.CloseButton();
            panel.Display();
        }
    }
}
