using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

using Logger = Rocket.Core.Logging.Logger;

namespace DynShop
{
    public class ShopVehicle : ShopObject
    {
        public ShopVehicle() {}
        public ShopVehicle(ushort itemID, decimal buyCost, decimal sellMultiplier)
        {
            ItemID = itemID;
            BuyCost = buyCost;
            SellMultiplier = sellMultiplier;
            AssetName();
        }

        internal bool Buy(decimal curBallance, UnturnedPlayer player, out decimal totalCost, out ushort totalItems)
        {
            totalItems = 0;
            totalCost = 0;
            Asset itemAsset = Assets.find(EAssetType.VEHICLE, ItemID);
            curBallance -= BuyCost;
            if (curBallance - BuyCost < 0)
                return false;
            if (itemAsset == null)
                return false;
            try
            {
                DShop.Database.AddVehicleInfo((ulong)player.CSteamID, ItemID);
                player.GiveVehicle(ItemID);
                totalCost += BuyCost;
                totalItems++;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                totalItems--;
                return false;
            }
            DShop.Instance._OnShopBuy(curBallance, player, 1, this, ItemType.Vehicle, 0, totalCost, totalItems);
            return true;
        }

        internal bool Sell(decimal curBallance, UnturnedPlayer player, out decimal totalCost, out ushort actualCount)
        {
            bool sufficientAmount = false;
            totalCost = 0;
            actualCount = 0;
            VehicleInfo vInfo = DShop.Database.GetVehicleInfo((ulong)player.CSteamID, ItemID);
            if (vInfo != null)
            {
                bool hasLocked = false;
                bool withinRange = false;
                InteractableVehicle vehicle = null;
                for (int i = 0; i < VehicleManager.vehicles.Count; i++)
                {
                    vehicle = VehicleManager.vehicles[i];
                    if (vehicle.id == ItemID && vehicle.lockedOwner == player.CSteamID)
                    {
                        hasLocked = true;
                        if (Vector3.Distance(player.Position, vehicle.transform.position) <= 10 && !vehicle.isDead && !vehicle.isDrowned)
                        {
                            withinRange = true;
                            break;
                        }
                    }
                }
                if (!hasLocked)
                    actualCount = 2;
                else if (!withinRange)
                {
                    actualCount = 3;
                }
                else
                {
                    sufficientAmount = true;
                    actualCount = 1;
                    DShop.Database.DeleteVehicleInfo(vInfo);
                    vehicle.askDamage(ushort.MaxValue, false);
                }
            }
            return sufficientAmount;
        }

    }
}
