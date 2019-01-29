using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
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
            if (decimal.Subtract(curBallance, BuyCost) < 0m)
                return false;
            if (itemAsset == null)
                return false;
            try
            {
                player.GiveVehicle(ItemID);
                DShop.Database.AddVehicleInfo((ulong)player.CSteamID, ItemID);
                totalCost = decimal.Add(totalCost, BuyCost);
                curBallance = decimal.Subtract(curBallance, BuyCost);
                totalItems++;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                totalItems = 2;
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
                    if (vehicle.id == ItemID && vehicle.isLocked && vehicle.lockedOwner == player.CSteamID)
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
                else if (withinRange && !vehicle.isEmpty)
                    actualCount = 4;
                else if (!withinRange)
                    actualCount = 3;
                else
                {
                    sufficientAmount = true;
                    actualCount = 1;
                    if (DShop.Instance.Configuration.Instance.VehicleSellDropElements)
                    {
                        BarricadeRegion vregion = null;
                        byte x;
                        byte y;
                        ushort plant;
                        if (BarricadeManager.tryGetPlant(vehicle.transform, out x, out y, out plant, out vregion))
                        {
                            for (int j = 0; j < vregion.drops.Count; j++)
                            {
                                if (j < vregion.drops.Count && vregion.barricades[j].barricade.id > 0)
                                {
                                    Item item = new Item(vregion.barricades[j].barricade.id, true);
                                    ItemManager.dropItem(item, vregion.drops[j].model.position, false, true, true);
                                }
                            }
                        }
                    }
                    DShop.Database.DeleteVehicleInfo(vInfo);
                    vehicle.askDamage(ushort.MaxValue, false);
                    totalCost = decimal.Multiply(BuyCost, SellMultiplier);
                    DShop.Instance._OnShopSell(decimal.Add(curBallance, totalCost), player, 1, this, ItemType.Item, BuyCost, totalCost, actualCount, 0);
                }
            }
            return sufficientAmount;
        }

    }
}
