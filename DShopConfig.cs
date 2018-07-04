using System;
using Rocket.API;
using System.Xml.Serialization;
using Rocket.Core.Logging;
using System.Collections.Generic;
using SDG.Unturned;

namespace DynShop
{
    public class DShopConfig : IRocketPluginConfiguration
    {
        public int ObjectListConfigVersion = 0;
        [XmlIgnore]
        public BackendType Backend = BackendType.MySQL;
        [XmlAttribute("Backend")]
        public string XmlBackend
        {
            get
            {
                return Enum.GetName(typeof(BackendType), Backend);
            }
            set
            {
                try
                {
                    Backend = (BackendType)Enum.Parse(typeof(BackendType), value, true);
                }
                catch
                {
                    Logger.LogWarning("Warning: Invalid Backend type, falling back to MySQL.");
                    Backend = BackendType.MySQL;
                }
            }
        }
        public string DatabaseTablePrefix = "dshop";

        public decimal DefaultSellMultiplier = .25m;
        public decimal MinDefaultBuyCost = .4m;
        public decimal MaxBuyCost = 6000m;
        public decimal DefaultIncrement = .01m;

        [XmlArray("Items"), XmlArrayItem(ElementName = "Item")]
        public List<ShopItem> Items = new List<ShopItem>();

        [XmlArray("Vehicles"), XmlArrayItem(ElementName = "Vehicle")]
        public List<ShopVehicle> Vehicles = new List<ShopVehicle>();

        public void DefaultItems()
        {
            if (ObjectListConfigVersion == 0)
            {
                ObjectListConfigVersion = 1;
                // Items
                AddItemDB(new ShopItem(2, 2, .5m, .5m, .01m));

                // Vehicles
                AddItemDB(new ShopVehicle(1, 397));
            }
        }

        public bool AddItemDB(ShopObject shopObject)
        {
            ItemType type;
            if (shopObject is ShopItem)
                type = ItemType.Item;
            else
                type = ItemType.Vehicle;
            // Only add items to database if they're not present.
            if (DShop.Database.GetItem(type, shopObject.ItemID).ItemID == 0)
                return DShop.Database.AddItem(type, shopObject);
            return false;
        }

        public void LoadDefaults()
        {
            
        }
    }
}