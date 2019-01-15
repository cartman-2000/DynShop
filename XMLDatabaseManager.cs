using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DynShop
{
    internal class XMLDatabaseManager : DataManager
    {
        private Dictionary<ushort, ShopVehicle> Vehicles = new Dictionary<ushort, ShopVehicle>();
        private Dictionary<ushort, ShopItem> Items = new Dictionary<ushort, ShopItem>();

        public bool IsLoaded { get; set; }

        public BackendType Backend { get { return BackendType.XML; } }



        internal XMLDatabaseManager()
        {
            Items = DShop.Instance.Configuration.Instance.Items.ToDictionary(v => v.ItemID, v => v);
            Vehicles = DShop.Instance.Configuration.Instance.Vehicles.ToDictionary(v => v.ItemID, v => v);
            CheckSchema();
            IsLoaded = true;
        }

        public int SchemaVersion
        {
            get { return DShop.Instance.Configuration.Instance.FlatFileSchemaVersion; }
            set { DShop.Instance.Configuration.Instance.FlatFileSchemaVersion = value; }
        }

        public void CheckSchema()
        {
            if (SchemaVersion < 1)
            {
                SchemaVersion = 1;
            }

        }

        public bool ConvertDB(BackendType toBackend)
        {
            if (toBackend == Backend)
                return false;
            else if (toBackend == BackendType.MySQL)
            {
                DataManager database = new MySQLDatabaseManager();
                if (!database.IsLoaded)
                    return false;
                foreach (ShopItem item in Items.Values)
                {
                    database.AddItem(ItemType.Item, item);
                }
                foreach (ShopVehicle vehicle in Vehicles.Values)
                {
                    database.AddItem(ItemType.Vehicle, vehicle);
                }
                database.Unload();
                database = null;
                return true;
            }
            return false;
        }

        public Dictionary<ushort, ShopObject> GetAllItems(ItemType type)
        {
            return type == ItemType.Item ? Items.ToDictionary(k => k.Key, v => (ShopObject)v.Value) : Vehicles.ToDictionary(k => k.Key, v => (ShopObject)v.Value);
        }


        public bool AddItem(ItemType type, ShopObject shopObject)
        {
            if (type == ItemType.Item)
            {
                ShopItem item = shopObject as ShopItem;
                if (Items.ContainsKey(item.ItemID))
                {
                    Items[item.ItemID] = item;
                    return true;
                }
                else
                {
                    Items.Add(item.ItemID, item);
                    return true;
                }
            }
            else
            {
                ShopVehicle vehicle = shopObject as ShopVehicle;
                if (Vehicles.ContainsKey(vehicle.ItemID))
                {
                    Vehicles[vehicle.ItemID] = vehicle;
                    return true;
                }
                else
                {
                    Vehicles.Add(vehicle.ItemID, vehicle);
                    return true;
                }
            }
        }

        public bool DeleteItem(ItemType type, ushort itemID)
        {
            if (type == ItemType.Item)
            {
                if (!Items.ContainsKey(itemID))
                    return false;
                else
                {
                    Items.Remove(itemID);
                    Save();
                    return true;
                }
            }
            else
            {
                if (!Vehicles.ContainsKey(itemID))
                    return false;
                else
                {
                    Vehicles.Remove(itemID);
                    Save();
                    return true;
                }
            }
        }

        public ShopObject GetItem(ItemType type, ushort itemID)
        {
            ShopObject shopObject = new ShopObject();
            if (type == ItemType.Item)
            {
                if (Items.ContainsKey(itemID))
                    shopObject = Items[itemID];
                    
            }
            else
            {
                if (Vehicles.ContainsKey(itemID))
                    shopObject = Vehicles[itemID];
            }
            // Return ShopObject if an id isn't found, Returns the right type of class otherwise(after conversion).
            return shopObject;
        }

        private void Save()
        {
            DShop.Instance.Configuration.Instance.Items = Items.Values.ToList();
            DShop.Instance.Configuration.Instance.Vehicles = Vehicles.Values.ToList();
            DShop.Instance.Configuration.Save();
        }

        public void Unload()
        {
            Save();
            Vehicles.Clear();
            Items.Clear();
            IsLoaded = false;
        }

        public bool AddVehicleInfo(ulong SteamID, ushort vehicleID)
        {
            DShop.Instance.Configuration.Instance.VehicleInfos.Add(new VehicleInfo(SteamID, vehicleID));
            return true;
        }

        public VehicleInfo GetVehicleInfo(ulong SteamID, ushort vehicleID)
        {
            VehicleInfo vInfo = null;
            if (DShop.Instance.Configuration.Instance.IgnoreVehicleInfoMap)
                vInfo = DShop.Instance.Configuration.Instance.VehicleInfos.FirstOrDefault(i => i.SteamID == SteamID && i.VehicleID == vehicleID);
            else
                vInfo = DShop.Instance.Configuration.Instance.VehicleInfos.FirstOrDefault(i => i.SteamID == SteamID && i.VehicleID == vehicleID && i.MapName.ToLower() == Provider.map.ToLower());

            throw new NotImplementedException();
        }

        public bool DeleteVehicleInfo(VehicleInfo vInfo)
        {
            return DShop.Instance.Configuration.Instance.VehicleInfos.Remove(vInfo);
        }
    }
}
