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

        public static bool IsFraction(this string value, out decimal fraction)
        {
            fraction = 0;
            decimal p1 = 0;
            decimal p2 = 0;
            if (value.Contains("/"))
            {
                if (decimal.TryParse(value.Split('/')[0], out p1) && decimal.TryParse(value.Split('/')[1], out p2))
                {
                    if (p2 != 0)
                    {
                        fraction = decimal.Divide(p1, p2);
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
