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

        public static string AssetName(this ushort itemID, ItemType type)
        {
            if (type == ItemType.Item)
                return ((ItemAsset)Assets.find(EAssetType.ITEM, itemID)).itemName;
            else
                return ((VehicleAsset)Assets.find(EAssetType.VEHICLE, itemID)).vehicleName;
        }

        public static string AssetName(this string itemName, ItemType type)
        {
            if (type == ItemType.Item)
                return ((ItemAsset)Assets.find(EAssetType.ITEM, itemName)).itemName;
            else
                return ((VehicleAsset)Assets.find(EAssetType.VEHICLE, itemName)).vehicleName;
        }
    }
}
