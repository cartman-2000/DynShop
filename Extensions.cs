using Rocket.Core.Logging;
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

        public static Asset AssetFromID(this ushort itemID, ItemType type)
        {
                return Assets.find(type == ItemType.Item ? EAssetType.ITEM : EAssetType.VEHICLE, itemID);
        }

        public static ushort AssetIDFromName(this string itemName, ItemType type)
        {
            ushort assetID = 0;
            Asset[] assets = Assets.find(type == ItemType.Item ? EAssetType.ITEM : EAssetType.VEHICLE);
            for (int i = 0; i < assets.Length; i++)
            {
                ItemAsset iAsset = null;
                VehicleAsset vAsset = null;
                if (type == ItemType.Item)
                    iAsset = (ItemAsset)assets[i];
                else
                    vAsset = (VehicleAsset)assets[i];

                if (type == ItemType.Item && iAsset != null && iAsset.itemName != null && iAsset.itemName.ToLower().Contains(itemName.ToLower()))
                {
                    assetID = iAsset.id;
                    break;
                }
                else if (type == ItemType.Vehicle && vAsset != null && vAsset.vehicleName != null && vAsset.vehicleName.ToLower().Contains(itemName.ToLower()))
                {
                    assetID = vAsset.id;
                    break;
                }
            }
            return assetID;
        }
    }
}
