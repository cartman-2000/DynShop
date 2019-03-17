using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace DynShop
{
    public class VehicleInfo
    {
        public VehicleInfo() { }
        public VehicleInfo(ulong steamID, ushort vehicleID)
        {
            VehicleID = vehicleID;
            MapName = Provider.map.ToLower();
            TimeBought = DateTime.Now;
            SteamID = steamID;
        }
        [XmlIgnore]
        public int InfoID = 0;

        public ushort VehicleID = 0;
        public string MapName = string.Empty;
        public DateTime TimeBought;
        public ulong SteamID = 0;
    }
}
