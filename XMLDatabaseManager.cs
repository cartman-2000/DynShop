using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DynShop
{
    internal class XMLDatabaseManager : DataManagerFields, DataManager
    {
        private Dictionary<ushort, ShopVehicle> Vehicles = new Dictionary<ushort, ShopVehicle>();
        private Dictionary<ushort, ShopItem> Items = new Dictionary<ushort, ShopItem>();


        internal XMLDatabaseManager()
        {
            Items = DShop.Instance.Configuration.Instance.Items.ToDictionary(v => v.ItemID, v => v);
            Vehicles = DShop.Instance.Configuration.Instance.Vehicles.ToDictionary(v => v.ItemID, v => v);
            Backend = BackendType.XML;
            CheckSchema();
        }

        public void CheckSchema()
        {
                DShop.Instance.Configuration.Instance.DefaultItems();
        }

        public bool ConvertDB(BackendType toBackend)
        {
            if (toBackend == Backend)
                return false;
            else if (toBackend == BackendType.MySQL)
            {
                DataManager database = new MySQLDatabaseManager();
                foreach (ShopItem item in Items.Values)
                {
                    database.AddItem(ItemType.Item, item);
                }
                foreach (ShopVehicle vehicle in Vehicles.Values)
                {
                    database.AddItem(ItemType.Vehicle, vehicle);
                }
                database.Unload();
                return true;
            }
            return false;

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
        }
    }
}
