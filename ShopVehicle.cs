using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DynShop
{
    public class ShopVehicle : ShopObject
    {
        public ShopVehicle() {}
        public ShopVehicle(ushort itemID, decimal buyCost, string itemName)
        {
            ItemID = itemID;
            BuyCost = buyCost;
            ItemName = itemName;
        }
    }
}
