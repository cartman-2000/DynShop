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
        public string ItemName = "";

        public string AssetName(ShopObject type)
        {
            string assetName = "NULL";
            Asset asset = Assets.find(type is ShopItem? EAssetType.ITEM : EAssetType.VEHICLE, ItemID);
            if (asset == null)
                return assetName;
            if (type is ShopItem)
            {
                ItemAsset item = asset as ItemAsset;
                if (item.itemName != null)
                    assetName = item.itemName;
            }
            else
            {
                VehicleAsset item = asset as VehicleAsset;
                if (item.vehicleName != null)
                    assetName = item.vehicleName;
            }
            return assetName;
        }

    }
}
