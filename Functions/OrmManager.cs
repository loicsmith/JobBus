﻿using ModKit.ORM;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MODRP_JobBus.Functions
{
    internal class OrmManager
    {
        public class JobBus_LineManager : ModEntity<JobBus_LineManager>
        {
            [AutoIncrement][PrimaryKey] public int Id { get; set; }

            public string LineName { get; set; }
            public string BusStopID { get; set; }
        }

        public class JobBus_BusStopManager : ModEntity<JobBus_BusStopManager>
        {
            [AutoIncrement][PrimaryKey] public int Id { get; set; }

            public string BusStopName { get; set; }
            public float PositionX { get; set; }
            public float PositionY { get; set; }
            public float PositionZ { get; set; }
        }

    }
}