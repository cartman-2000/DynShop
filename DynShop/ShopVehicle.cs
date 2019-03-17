﻿using Rocket.Unturned.Player;
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
        public ShopVehicle(ushort itemID, decimal buyCost, decimal sellMultiplier, RestrictBuySell restrictBuySell)
        {
            ItemID = itemID;
            BuyCost = buyCost;
            SellMultiplier = sellMultiplier;
            RestrictBuySell = restrictBuySell;
            AssetName();
        }

        internal bool Buy(decimal curBallance, UnturnedPlayer player, out decimal totalCost, out short totalItems)
        {
            totalItems = 0;
            totalCost = 0;
            Asset itemAsset = Assets.find(EAssetType.VEHICLE, ItemID);
            if (decimal.Subtract(curBallance, BuyCost) < 0m)
                return false;
            if (itemAsset == null)
            {
                totalItems = -1;
                return false;
            }
            if (RestrictBuySell == RestrictBuySell.SellOnly)
            {
                totalItems = -3;
                return false;
            }
            try
            {
                player.GiveVehicle(ItemID);
                DShop.Instance.Database.AddVehicleInfo((ulong)player.CSteamID, ItemID);
                totalCost = decimal.Add(totalCost, BuyCost);
                curBallance = decimal.Subtract(curBallance, BuyCost);
                totalItems++;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                totalItems = -2;
                return false;
            }
            DShop.Instance._OnShopBuy(curBallance, player, 1, this, ItemType.Vehicle, 0, totalCost, totalItems);
            return true;
        }

        internal bool Sell(decimal curBallance, UnturnedPlayer player, RaycastInfo raycastInfo, out decimal totalCost, out short actualCount)
        {
            bool sufficientAmount = false;
            totalCost = 0;
            actualCount = 0;
            InteractableVehicle vehicle = null;
            if (RestrictBuySell == RestrictBuySell.BuyOnly)
            {
                actualCount = -1;
                return false;
            }
            VehicleInfo vInfo = DShop.Instance.Database.GetVehicleInfo((ulong)player.CSteamID, ItemID);
            if (vInfo == null)
            {
                // The car the player's looking at hasn't been bought before from them, from the shop.
                actualCount = -2;
                return false;
            }
            else
            {
                vehicle = raycastInfo.vehicle;
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
                DShop.Instance.Database.DeleteVehicleInfo(vInfo);
                vehicle.askDamage(ushort.MaxValue, false);
                totalCost = decimal.Multiply(BuyCost, SellMultiplier);
                DShop.Instance._OnShopSell(decimal.Add(curBallance, totalCost), player, 1, this, ItemType.Vehicle, BuyCost, totalCost, actualCount, 0);
            }
            return sufficientAmount;
        }
    }
}
