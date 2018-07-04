using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DynShop
{
    public static class Extensions
    {
        public static decimal Calculate(this decimal curBallance, UnturnedPlayer player, ushort itemID, ushort numItems, decimal curCost, out decimal newCost, decimal minCost, float sellMultiplier, bool isBuying = true, bool shouldApplyDynCost = true)
        {
            newCost = 0;
            return curBallance;
        }

        public static string AssetName(this ShopObject type, ushort itemID)
        {
            if (type is ShopItem)
                return ((ItemAsset)Assets.find(EAssetType.ITEM, itemID)).itemName;
            else
                return ((VehicleAsset)Assets.find(EAssetType.VEHICLE, itemID)).vehicleName;
        }
    }
}
