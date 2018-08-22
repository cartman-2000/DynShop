using Rocket.Core.Logging;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DynShop
{
    public class ShopVehicle : ShopObject
    {
        public ShopVehicle() {}
        public ShopVehicle(ushort itemID, decimal buyCost)
        {
            ItemID = itemID;
            BuyCost = buyCost;
            ItemName = AssetName();
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

        internal bool Sell()
        {
            bool sufficientAmount = true;
            // Stub
            return sufficientAmount;
        }

    }
}
