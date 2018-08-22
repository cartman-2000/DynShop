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
        public decimal SellMultiplier = .25m;
        [XmlAttribute]
        public decimal MinBuyPrice = .2m;
        [XmlAttribute]
        public decimal Change = .01m;

        public ShopItem() {}
        public ShopItem(ushort itemID, decimal buyCost, decimal sellMultiplier, decimal minBuyPrice, decimal change)
        {
            ItemID = itemID;
            BuyCost = buyCost;
            SellMultiplier = sellMultiplier;
            MinBuyPrice = minBuyPrice;
            Change = change;
            ItemName = AssetName();
        }

        internal bool Buy(decimal curBallance, UnturnedPlayer player, ushort numItems, out decimal newCost, out decimal totalCost, out ushort totalItems)
        {
            bool sufficientAmount = true;
            totalItems = 0;
            newCost = BuyCost;
            totalCost = 0;

            Asset itemAsset = Assets.find(EAssetType.ITEM, ItemID);
            if (itemAsset == null || ((ItemAsset)itemAsset).isPro)
                return false;

            decimal oldCost = BuyCost;
            for (int i = 0; i < numItems; i++)
            {
                if (curBallance- totalCost - BuyCost < 0)
                {
                    sufficientAmount = false;
                    break;
                }
                try
                {

                    // Give items to client
                    Item item = new Item(ItemID, EItemOrigin.CRAFT);
                    player.Inventory.forceAddItem(item, true);
                    totalCost += BuyCost;
                    totalItems++;
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                    return false;
                }
                if (!DShop.Instance.Configuration.Instance.RunInStaticPrices)
                {
                    if ((BuyCost + Change) > DShop.Instance.Configuration.Instance.MaxBuyCost)
                        continue;
                    BuyCost += Change;
                    newCost = BuyCost;
                }
            }
            if (oldCost != BuyCost)
                DShop.Database.AddItem(ItemType.Item, this);
            if (totalItems > 0)
                DShop.Instance._OnShopBuy(curBallance, player, numItems, this, ItemType.Item, newCost, totalCost, totalItems);
            return sufficientAmount;
        }

        internal bool Sell(decimal curBallance, UnturnedPlayer player, ushort numItems, out decimal newCost, out decimal totalCost, out ushort totalItems, out decimal totalAttatchmentCost)
        {
            bool sufficientAmount = true;
            totalItems = 0;
            newCost = 0;
            totalCost = 0;
            totalAttatchmentCost = 0;

            bool runStaticPrices = DShop.Instance.Configuration.Instance.RunInStaticPrices;

            Asset itemAsset = Assets.find(EAssetType.ITEM, ItemID);
            List<InventorySearch> items = player.Inventory.search(ItemID, true, true);
            // if num items is maxed, set to the found item count in the inventory.
            if (numItems == ushort.MaxValue)
                numItems = (ushort)items.Count;
            Dictionary<ushort, ShopObject> attatchments = new Dictionary<ushort, ShopObject>();

            if (items.Count == 0)
                return false;
            if (itemAsset == null)
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

                        if (sightID != 0)
                        {
                            ProccessAttatchment(sightID, 1, sightHealth, ref attatchments, ref totalAttatchmentCost, ref totalCost);
                        }
                        if (tacticalID != 0)
                        {
                            ProccessAttatchment(tacticalID, 1, tacticalHealth, ref attatchments, ref totalAttatchmentCost, ref totalCost);
                        }
                        if (gripID != 0)
                        {
                            ProccessAttatchment(gripID, 1, gripHealth, ref attatchments, ref totalAttatchmentCost, ref totalCost);
                        }
                        if (barrelID != 0)
                        {
                            ProccessAttatchment(barrelID, 1, barrelHealth, ref attatchments, ref totalAttatchmentCost, ref totalCost);
                        }
                        if (magazineID != 0)
                        {
                            ProccessAttatchment(magazineID, magazineAmount, magazineHealth, ref attatchments, ref totalAttatchmentCost, ref totalCost);
                        }

                    }
                }
                // remove items from client.
                player.Inventory.removeItem(items[0].page, player.Inventory.getIndex(items[0].page, items[0].jar.x, items[0].jar.y));
                items.RemoveAt(0);

                if (!runStaticPrices)
                {
                    if ((BuyCost - Change) < MinBuyPrice)
                        continue;
                    BuyCost -= Change;
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
                DShop.Instance._OnShopSell(curBallance, player, numItems, this, ItemType.Item, newCost, totalCost, totalItems, totalAttatchmentCost);
            return sufficientAmount;
        }

        private void ProccessAttatchment(ushort itemID, byte amount, byte health, ref Dictionary<ushort, ShopObject> attatchments, ref decimal totalAttatchmentCost, ref decimal totalCost)
        {
            ShopObject sObject = null;
            if (attatchments.ContainsKey(itemID))
                sObject = attatchments[itemID];
            else
            {
                sObject = DShop.Database.GetItem(ItemType.Item, itemID);
                attatchments.Add(itemID, sObject);
            }
            if (sObject.ItemID == itemID)
            {
                Item item = new Item(itemID, amount, health);
                Asset iAsset = Assets.find(EAssetType.ITEM, itemID);
                if (iAsset != null)
                {
                    ShopItem tmp = sObject as ShopItem;
                    totalAttatchmentCost = decimal.Add(totalAttatchmentCost, tmp.CalcSellCost(iAsset, item));
                    totalCost = decimal.Add(totalCost, tmp.CalcSellCost(iAsset, item));
                    if (sObject.BuyCost - tmp.Change >= MinBuyPrice)
                        sObject.BuyCost -= tmp.Change;
                }
            }
        }

        internal decimal CalcSellCost(Asset asset, Item item)
        {
            decimal sellCost = decimal.Multiply(BuyCost, SellMultiplier);
            if (DShop.Instance.Configuration.Instance.UseItemQuality)
            {
                if (asset is ItemMagazineAsset || asset is ItemSupplyAsset)
                    sellCost = decimal.Multiply(sellCost, decimal.Divide(item.amount, ((ItemAsset)asset).amount));
                else
                    sellCost = decimal.Multiply(sellCost, decimal.Divide(item.durability, 100));
            }
            return sellCost;
        }
    }
}
