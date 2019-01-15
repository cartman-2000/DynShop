﻿using SDG.Unturned;
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
        public string ItemName = "";

        public void AssetName()
        {
            Asset asset = Assets.find(this is ShopItem? EAssetType.ITEM : EAssetType.VEHICLE, ItemID);
            if (asset == null)
                ItemName = string.Empty;
            else
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
}
