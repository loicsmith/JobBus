using Life.Network;
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
        NextBusStopName = nextBusStopName;
    }
}

namespace JobBus.Functions
{
    internal class DataManager
    {
        public ModKit.ModKit Context { get; set; }

        private static DataManager _instance = null;
        public static DataManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new DataManager();
                }
                return _instance;
            }
        }

        public Dictionary<uint, BusLineInfo> busLineDictionary = new Dictionary<uint, BusLineInfo>();

        public void BusDriver_StartLine(Player player, OrmManager.JobBus_LineManager LineManager, string CurrentBusStopName, int TotalBusStopNumber, string nextBusStopName)
        {
            BusLineInfo newLineInfo = new BusLineInfo(LineManager.LineName, LineManager.LineNumber, CurrentBusStopName, 1, TotalBusStopNumber, nextBusStopName);
            DataManager.Instance.busLineDictionary.Add(player.setup.netId, newLineInfo);
        }

        public void BusDriver_StopLine(uint PlayerNetID)
        {
            DataManager.Instance.busLineDictionary.Remove(PlayerNetID);
        }

        public void BusDriver_BusStop(uint PlayerNetID, string BusStopName, int BusStopNumber, string NextBusStopName)
        {
            if (DataManager.Instance.busLineDictionary.TryGetValue(PlayerNetID, out BusLineInfo LineInfo))
            {
                LineInfo.CurrentBusStopName = BusStopName;
                LineInfo.CurrentBusStopNumber = BusStopNumber;
                LineInfo.NextBusStopName = NextBusStopName;
            }
        }
    }
}
