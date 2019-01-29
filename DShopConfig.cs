using System;
using Rocket.API;
using System.Xml.Serialization;
using Rocket.Core.Logging;
using System.Collections.Generic;
using SDG.Unturned;
using Rocket.Core.Assets;
using System.IO;

namespace DynShop
{
    public class DShopConfig : IRocketPluginConfiguration
    {
        public int ObjectListConfigVersion = 0;
        [XmlIgnore]
        public BackendType Backend = BackendType.MySQL;
        [XmlElement("Backend")]
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

        public string DatabaseAddress = "address";
        public string DatabaseName = "Database Name";
        public string DatabaseUsername = "Username";
        public string DatabasePassword = "Password";
        public ushort DatabasePort = 3306;
        public string DatabaseTablePrefix = "dshop";

        public bool Debug = false;

        public bool RunInStaticPrices = false;
        public bool GasCansEmptyOnBuy = false;
        public bool UseItemQuality = true;
        public bool SellAttatchmentsOnGun = false;
        public decimal DefaultSellMultiplier = .25m;
        public decimal MinDefaultBuyCost = .4m;
        public decimal DefaultBuyCost = 10;
        public decimal MaxBuyCost = 6000m;
        public decimal DefaultIncrement = .01m;
        public ushort MaxBuyCount = 300;

        public bool CanSellVehicles = true;
        public bool VehicleSellDropElements = true;
        public bool IgnoreVehicleInfoMap = false;
        public bool IgnoreVehicleInfoSpecificServer = false;

        public int FlatFileSchemaVersion = 0;
        [XmlArray("Items"), XmlArrayItem(ElementName = "Item")]
        public List<ShopItem> Items = new List<ShopItem>();

        [XmlArray("Vehicles"), XmlArrayItem(ElementName = "Vehicle")]
        public List<ShopVehicle> Vehicles = new List<ShopVehicle>();

        [XmlArray("VehicleInfos"), XmlArrayItem(ElementName = "VehicleInfo")]
        public List<VehicleInfo> VehicleInfos = new List<VehicleInfo>();

        public void DefaultItems()
        {
            IAsset<DefaultValues> defaultValues = null;
            string text = Path.Combine(DShop.Instance.Directory, string.Format("{0}.defaultvalues.xml", DShop.Instance.Name));

            try
            {
                defaultValues = new XMLFileAsset<DefaultValues>(text, null, null);
                defaultValues.Load();
                if (ObjectListConfigVersion < defaultValues.Instance.FileVersion)
                {
                    ObjectListConfigVersion = defaultValues.Instance.FileVersion;
                    Dictionary<ushort, ShopObject> items = DShop.Database.GetAllItems(ItemType.Item);
                    Dictionary<ushort, ShopObject> vehicles = DShop.Database.GetAllItems(ItemType.Vehicle);
                    Logger.Log("Adding new Default items to database!");
                    // Start adding items to the database from the defaults file that aren't present in the database.
                    foreach (ShopItem item in defaultValues.Instance.Items)
                    {
                        if (!items.ContainsKey(item.ItemID))
                        {
                            // Get generate the asset name for the database.
                            item.AssetName();
                            DShop.Database.AddItem(ItemType.Item, item as ShopObject);
                        }
                    }
                    foreach (ShopVehicle vehicle in defaultValues.Instance.Vehicles)
                    {
                        if (!vehicles.ContainsKey(vehicle.ItemID))
                        {
                            // Get generate the asset name for the database.
                            vehicle.AssetName();
                            DShop.Database.AddItem(ItemType.Vehicle, vehicle as ShopObject);
                        }
                    }
                    Logger.Log("Finished!");
                }
                //defaultValues.Save();
            }
            catch
            {
                Logger.LogWarning("Error parsing the defaults file, skipping loading shop defaults!");
                return;
            }

        }

        public void LoadDefaults() {}
    }
}