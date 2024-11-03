using Life.Network;
using System;
using System.Collections.Generic;

public class BusLineInfo
{
    public string LineName { get; set; }
    public string LineNumber { get; set; }
    public string CurrentBusStopName { get; set; }
    public int CurrentBusStopNumber { get; set; }
    public int TotalBusStops { get; set; }

    public BusLineInfo(string lineName, string lineNumber, string currentBusStopName, int currentBusStopNumber, int totalBusStop)
    {
        LineName = lineName;
        LineNumber = lineNumber;
        CurrentBusStopName = currentBusStopName;
        CurrentBusStopNumber = currentBusStopNumber;
        TotalBusStops = totalBusStop;
        // Prochain arrêt a add
    }
}

namespace MODRP_JobBus.Functions
{
    internal class DataManager
    {
        public ModKit.ModKit Context { get; set; }

        public Dictionary<uint, BusLineInfo> busLineDictionary = new Dictionary<uint, BusLineInfo>();

        public void BusDriver_StartLine(Player player, OrmManager.JobBus_LineManager LineManager, string CurrentBusStopName, int TotalBusStopNumber)
        {
            BusLineInfo newLineInfo = new BusLineInfo(LineManager.LineName, LineManager.LineNumber, CurrentBusStopName, 1, TotalBusStopNumber);
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
                
            }
        }

        public void BusDriver_StopLine(uint PlayerNetID)
        {
            busLineDictionary.Remove(PlayerNetID);
        }

        public void BusDriver_BusStop(uint PlayerNetID, string BusStopName, int BusStopNumber)
        {
            if (busLineDictionary.TryGetValue(PlayerNetID, out BusLineInfo LineInfo))
            {
                LineInfo.CurrentBusStopName = BusStopName;
                LineInfo.CurrentBusStopNumber = BusStopNumber;

                foreach (var entry in busLineDictionary)
                {

                    BusLineInfo lineInfo = entry.Value;

                    uint lineId = entry.Key;
                    Console.WriteLine($"NetID : {lineId}");
                    Console.WriteLine($"Nom de la Ligne: {lineInfo.LineName}");
                    Console.WriteLine($"Numéro de la Ligne: {lineInfo.LineNumber}");
                    Console.WriteLine($"Arrêt actuel: {lineInfo.CurrentBusStopName}");
                    Console.WriteLine($"Numéro de l'arrêt: {lineInfo.CurrentBusStopNumber}/{lineInfo.TotalBusStops}");

                }
            }
        }
    }
}
