using System;
using Rocket.API;
using System.Xml.Serialization;
using Rocket.Core.Logging;
using System.Collections.Generic;

namespace DynShop
{
    public class DShopConfig : IRocketPluginConfiguration
    {
        public int ConfigVersion = 0;
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



        public decimal DefaultSellMultiplier = .25m;
        public decimal MinDefaultBuyCost = .4m;
        public decimal MaxBuyCost = 6000m;
        public decimal DefaultIncrement = .01m;


        public int FlatFileSchemaVersion = 0;
        [XmlArray("Items"), XmlArrayItem(ElementName = "Item")]
        public List<ShopItem> Items = new List<ShopItem>();

        [XmlArray("Vehicles"), XmlArrayItem(ElementName = "Vehicle")]
        public List<ShopVehicle> Vehicles = new List<ShopVehicle>();

        public void DefaultItems()
        {
            if (FlatFileSchemaVersion == 0)
            {
                FlatFileSchemaVersion = 1;
                
            }
        }

        public bool AddItemDB(ItemType type, ShopObject shopObject)
        {
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