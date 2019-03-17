using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace DynShop
{
    public class ShopObject
    {
        [XmlAttribute]
        public ushort ItemID = 0;
        [XmlAttribute]
        public decimal BuyCost = 10;
        [XmlAttribute]
        public decimal SellMultiplier = .25m;
        [XmlAttribute]
        public RestrictBuySell RestrictBuySell = RestrictBuySell.None;
        [XmlAttribute]
        public string ItemName = "#NULL";

        public void AssetName()
        {
            Asset asset = Assets.find(this is ShopItem ? EAssetType.ITEM : EAssetType.VEHICLE, ItemID);
            if (asset != null)
            {
                if (this is ShopItem)
                {
                    ItemAsset item = asset as ItemAsset;
                    if (item.itemName != null)
                        ItemName = item.itemName;
                }
                else
                {
                    VehicleAsset item = asset as VehicleAsset;
                    if (item.vehicleName != null)
                        ItemName = item.vehicleName;
                }
            }
            return;
        }
    }

    [Serializable]
    public enum RestrictBuySell : byte
    {
        [XmlEnum(Name = "0")]
        None = 0,
        [XmlEnum(Name = "1")]
        SellOnly = 1,
        [XmlEnum(Name = "2")]
        BuyOnly = 2
    }
}
