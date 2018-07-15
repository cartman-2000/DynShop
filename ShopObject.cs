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

        public string AssetName(ShopObject type, ushort itemID)
        {
            string itemName = string.Empty;
            if (type is ShopItem)
                itemName = ((ItemAsset)Assets.find(EAssetType.ITEM, itemID)).itemName;
            else
                itemName = ((VehicleAsset)Assets.find(EAssetType.VEHICLE, itemID)).vehicleName;
            return !string.IsNullOrEmpty(itemName) ? itemName : string.Empty;
        }

    }
}
