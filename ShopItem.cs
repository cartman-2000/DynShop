using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace DynShop
{
    public class ShopItem : ShopObject
    {
        [XmlAttribute]
        public decimal SellMultiplier = .25m;
        [XmlAttribute]
        public decimal MinBuyPrice = .2m;
        [XmlAttribute]
        public decimal Change = .01m;

        public ShopItem() {}
        public ShopItem(ushort itemID, decimal buyCost, decimal sellMultiplier, decimal minBuyPrice, decimal change)
        {
            ItemID = itemID;
            BuyCost = buyCost;
            SellMultiplier = sellMultiplier;
            MinBuyPrice = minBuyPrice;
            Change = change;
            ItemName = this.AssetName(itemID);
        }
    }
}
