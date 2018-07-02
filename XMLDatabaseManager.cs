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
        public int SchemaVersion { get { return 1; } }


        internal XMLDatabaseManager()
        {
            Items = DShop.Instance.Configuration.Instance.Items.ToDictionary(v => v.ItemID, v => v);
            Vehicles = DShop.Instance.Configuration.Instance.Vehicles.ToDictionary(v => v.ItemID, v => v);
            CheckSchema();
        }

        public void CheckSchema()
        {
            if (DShop.Instance.Configuration.Instance.FlatFileSchemaVersion < SchemaVersion)
                DShop.Instance.Configuration.Instance.DefaultItems();
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
            if (type == ItemType.Item)
            {
                if (!Items.ContainsKey(itemID))
                    return new ShopItem();
                else
                    return Items[itemID];
            }
            else
            {
                if (!Vehicles.ContainsKey(itemID))
                    return new ShopVehicle();
                else
                    return Vehicles[itemID];
            }
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
        }
    }
}
