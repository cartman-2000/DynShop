using Rocket.Core.Logging;
using Rocket.Unturned.Player;
using SDG.Unturned;
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
        public decimal MinBuyPrice = .2m;
        [XmlAttribute]
        public decimal Change = .01m;
        [XmlAttribute]
        public decimal MaxBuyPrice = 0;

        public ShopItem() {}
        public ShopItem(ushort itemID, decimal buyCost, decimal sellMultiplier, decimal minBuyPrice, decimal change, decimal maxBuyPrice, RestrictBuySell restrictBuySell)
        {
            ItemID = itemID;
            BuyCost = buyCost;
            SellMultiplier = sellMultiplier;
            MinBuyPrice = minBuyPrice;
            Change = change;
            MaxBuyPrice = maxBuyPrice;
            RestrictBuySell = restrictBuySell;
            AssetName();
        }

        internal bool Buy(decimal curBallance, UnturnedPlayer player, ushort numItems, out decimal newCost, out decimal totalCost, out short totalItems)
        {
            bool sufficientAmount = true;
            totalItems = 0;
            newCost = BuyCost;
            totalCost = 0;

            Asset itemAsset = Assets.find(EAssetType.ITEM, ItemID);
            if (itemAsset == null || ((ItemAsset)itemAsset).isPro)
            {
                totalItems = -1;
                return false;
            }
            if (RestrictBuySell == RestrictBuySell.SellOnly)
            {
                totalItems = -3;
                return false;
            }
            decimal oldCost = BuyCost;
            for (int i = 0; i < numItems; i++)
            {
                if (decimal.Subtract(curBallance, BuyCost) < 0m)
                {
                    sufficientAmount = false;
                    break;
                }
                try
                {
                    // Give items to client, try to bypass cheats setting on server.
                    Item item = new Item(ItemID, EItemOrigin.CRAFT);
                    // set full amount of fuel in gas cans.
                    if (itemAsset is ItemFuelAsset && !DShop.Instance.Configuration.Instance.GasCansEmptyOnBuy)
                        item.state = BitConverter.GetBytes(((ItemFuelAsset)itemAsset).fuel);
                    player.Inventory.forceAddItem(item, true);
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
                if (!DShop.Instance.Configuration.Instance.RunInStaticPrices)
                {
                    // if there's no MaxBuyPrice set on the item, use the one set in the config.
                    if ((MaxBuyPrice == 0 && decimal.Add(BuyCost, Change) > DShop.Instance.Configuration.Instance.MaxBuyCost) || (MaxBuyPrice > 0 && decimal.Add(BuyCost, Change) > MaxBuyPrice))
                        continue;
                    BuyCost = decimal.Add(BuyCost, Change);
                    newCost = BuyCost;
                }
            }
            if (oldCost != BuyCost)
                DShop.Database.AddItem(ItemType.Item, this);
            if (totalItems > 0)
                DShop.Instance._OnShopBuy(curBallance, player, numItems, this, ItemType.Item, newCost, totalCost, totalItems);
            return sufficientAmount;
        }

        internal bool Sell(decimal curBallance, UnturnedPlayer player, ushort numItems, out decimal newCost, out decimal totalCost, out short totalItems, out decimal totalAttatchmentCost)
        {
            bool sufficientAmount = true;
            totalItems = 0;
            newCost = 0;
            totalCost = 0;
            totalAttatchmentCost = 0;
            if (RestrictBuySell == RestrictBuySell.BuyOnly)
            {
                totalItems = -1;
                return false;
            }
            bool runStaticPrices = DShop.Instance.Configuration.Instance.RunInStaticPrices;

            Asset itemAsset = Assets.find(EAssetType.ITEM, ItemID);
            if (itemAsset == null)
                return false;
            List<InventorySearch> items = player.Inventory.search(ItemID, true, true);
            // look for item in weapon slots, not handled by inventory search.
            if (itemAsset is ItemWeaponAsset)
            {
                if (player.Inventory.items != null)
                {
                    // First two pages are for the weapon slots.
                    for (byte p = 0; p < 2; p++)
                    if (player.Inventory.items[p] != null && player.Inventory.getItemCount(p) > 0)
                    {
                        ItemJar item = player.Inventory.getItem(p, 0);
                        if (item.item.id == ItemID)
                            items.Add(new InventorySearch(p, item));
                    }
                }
            }
            // if num items is maxed, set to the found item count in the inventory.
            if (numItems == ushort.MaxValue)
                numItems = (ushort)items.Count;
            Dictionary<ushort, ShopObject> attatchments = new Dictionary<ushort, ShopObject>();

            if (items.Count == 0)
                return false;
            decimal oldCost = BuyCost;
            for (int i = 0; i < numItems; i++)
            {

                if (items.Count == 0)
                {
                    if (totalItems < numItems)
                        sufficientAmount = false;
                    break;
                }
                if (SellMultiplier == 0)
                {
                    sufficientAmount = false;
                    break;
                }

                if (player.Player.equipment.checkSelection(items[0].page, items[0].jar.x, items[0].jar.y))
                    player.Player.equipment.dequip();

                totalCost = decimal.Add(totalCost, CalcSellCost(itemAsset, items[0].jar.item));
                totalItems++;

                if (DShop.Instance.Configuration.Instance.SellAttatchmentsOnGun)
                {
                    if (itemAsset is ItemGunAsset)
                    {
                        // index Item ID 0-1, Health 13
                        byte[] state = items[0].jar.item.state;
                        ushort sightID = BitConverter.ToUInt16(state, 0);
                        byte sightHealth = state[13];
                        // index Item ID 2-3, Health 14
                        ushort tacticalID = BitConverter.ToUInt16(state, 2);
                        byte tacticalHealth = state[14];
                        // index Item ID 4-5, Health 15
                        ushort gripID = BitConverter.ToUInt16(state, 4);
                        byte gripHealth = state[15];
                        // index Item ID 6-7, Health 16
                        ushort barrelID = BitConverter.ToUInt16(state, 6);
                        byte barrelHealth = state[16];
                        // index Item ID 8-9, Amount 10, Health 17
                        ushort magazineID = BitConverter.ToUInt16(state, 8);
                        byte magazineAmount = state[10];
                        byte magazineHealth = state[17];

                        if (sightID != 0 && ((ItemGunAsset)itemAsset).hasSight)
                        {
                            ProccessAttatchment(sightID, 1, sightHealth, ref attatchments, ref totalAttatchmentCost, ref totalCost, player);
                        }
                        if (tacticalID != 0 && ((ItemGunAsset)itemAsset).hasTactical)
                        {
                            ProccessAttatchment(tacticalID, 1, tacticalHealth, ref attatchments, ref totalAttatchmentCost, ref totalCost, player);
                        }
                        if (gripID != 0 && ((ItemGunAsset)itemAsset).hasGrip)
                        {
                            ProccessAttatchment(gripID, 1, gripHealth, ref attatchments, ref totalAttatchmentCost, ref totalCost, player);
                        }
                        if (barrelID != 0 && ((ItemGunAsset)itemAsset).hasBarrel)
                        {
                            ProccessAttatchment(barrelID, 1, barrelHealth, ref attatchments, ref totalAttatchmentCost, ref totalCost, player);
                        }
                        if (magazineID != 0)
                        {
                            ProccessAttatchment(magazineID, magazineAmount, magazineHealth, ref attatchments, ref totalAttatchmentCost, ref totalCost, player);
                        }

                    }
                }
                // remove items from client.
                player.Inventory.removeItem(items[0].page, player.Inventory.getIndex(items[0].page, items[0].jar.x, items[0].jar.y));
                items.RemoveAt(0);

                if (!runStaticPrices)
                {
                    if (decimal.Subtract(BuyCost, Change) < MinBuyPrice)
                        continue;
                    BuyCost = decimal.Subtract(BuyCost, Change); ;
                    newCost = BuyCost;
                }
            }
            if (oldCost != BuyCost)
                DShop.Database.AddItem(ItemType.Item, this);

            // Update costs for all sold attachments.
            if(!runStaticPrices && attatchments.Count != 0)
            {
                foreach (KeyValuePair<ushort, ShopObject> item in attatchments)
                {
                    if (item.Key == item.Value.ItemID)
                        DShop.Database.AddItem(ItemType.Item, item.Value);
                }
            }
            if (totalItems > 0)
                DShop.Instance._OnShopSell(decimal.Add(curBallance, totalCost), player, numItems, this, ItemType.Item, newCost, totalCost, totalItems, totalAttatchmentCost);
            return sufficientAmount;
        }

        private void ProccessAttatchment(ushort itemID, byte amount, byte health, ref Dictionary<ushort, ShopObject> attatchments, ref decimal totalAttatchmentCost, ref decimal totalCost, UnturnedPlayer player)
        {
            ShopObject sObject = null;
            Asset iAsset = Assets.find(EAssetType.ITEM, itemID);
            Item item = null;
            if (iAsset != null)
            {
                if (attatchments.ContainsKey(itemID))
                    sObject = attatchments[itemID];
                else
                {
                    sObject = DShop.Database.GetItem(ItemType.Item, itemID);
                    attatchments.Add(itemID, sObject);
                }
                if (sObject.ItemID == itemID)
                {
                    item = new Item(itemID, amount, health);
                    ShopItem tmp = sObject as ShopItem;
                    totalAttatchmentCost = decimal.Add(totalAttatchmentCost, tmp.CalcSellCost(iAsset, item));
                    totalCost = decimal.Add(totalCost, tmp.CalcSellCost(iAsset, item));
                    if (decimal.Subtract(sObject.BuyCost, tmp.Change) > tmp.MinBuyPrice && !DShop.Instance.Configuration.Instance.RunInStaticPrices)
                        sObject.BuyCost = decimal.Subtract(sObject.BuyCost, tmp.Change);
                }
                else
                {
                    // give the attachment to the player as it's not in the shop db.
                    item = new Item(itemID, EItemOrigin.CRAFT, health);
                    item.amount = amount;
                    player.Inventory.forceAddItem(item, true);
                }
            }
        }

        internal decimal CalcSellCost(Asset asset, Item item)
        {
            decimal sellCost = decimal.Multiply(BuyCost, SellMultiplier);
            if (DShop.Instance.Configuration.Instance.UseItemQuality)
            {
                // for mags/ammo boxes, calc quality percentage based on number of rounds left divided by the capacity in the asset. Also limit the amount down to asset capacity/full health, if the amount is more than that.
                if (asset is ItemMagazineAsset || asset is ItemSupplyAsset)
                    sellCost = item.amount >= ((ItemAsset)asset).amount ? sellCost : decimal.Multiply(sellCost, decimal.Divide(item.amount, ((ItemAsset)asset).amount));
                else if (asset is ItemFuelAsset)
                    sellCost = BitConverter.ToUInt16(item.state, 0) >= ((ItemFuelAsset)asset).fuel ? sellCost : decimal.Multiply(sellCost, decimal.Divide(BitConverter.ToUInt16(item.state, 0), ((ItemFuelAsset)asset).fuel));
                else if (((ItemAsset)asset).showQuality)
                    sellCost = item.durability >= 100 ? sellCost : decimal.Multiply(sellCost, decimal.Divide(item.durability, 100));
            }
            return sellCost;
        }
    }
}
