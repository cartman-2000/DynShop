using fr34kyn01535.Uconomy;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DynShop
{
    public class CommandBuy : IRocketCommand
    {
        public List<string> Aliases
        {
            get { return new List<string>(); }
        }

        public AllowedCaller AllowedCaller
        {
            get { return AllowedCaller.Player; }
        }

        public string Help
        {
            get { return "Buys an item off of the shop."; }
        }

        public string Name
        {
            get { return "dbuy"; }
        }

        public List<string> Permissions
        {
            get { return new List<string> { "dshop.buy" }; }
        }

        public string Syntax
        {
            get { return "<\"Item Name\" | ItemID> [amount] | <v> <\"Vehicle Name\" | VehicleID>"; }
        }

        public void Execute(IRocketPlayer caller, string[] command)
        {
            ItemType type = ItemType.Item;
            if (command.Length >= 1)
                type = command[0].ToLower() == "v" ? ItemType.Vehicle : ItemType.Item;

            if (command.Length == (type == ItemType.Item ? 0 : 1) || command.Length > 2)
            {
                UnturnedChat.Say(caller, Name + " - " + Syntax);
                return;
            }

            ushort itemID = 0;
            ushort count = 1;

            if (command.Length == 2 && type == ItemType.Item)
            {
                if (!ushort.TryParse(command[type == ItemType.Item ? 1 : 2], out count))
                    UnturnedChat.Say(caller, "Invalid item count value used.");
                if (count > DShop.Instance.Configuration.Instance.MaxBuyCount)
                    count = DShop.Instance.Configuration.Instance.MaxBuyCount;
                if (count == 0)
                    count = 1;
            }



            if (!ushort.TryParse(command[type == ItemType.Item ? 0 : 1], out itemID))
                itemID = type == ItemType.Item ? command[0].AssetIDFromName(type) : command[1].AssetIDFromName(type);
            if (itemID.AssetFromID(type) == null)
            {
                UnturnedChat.Say(caller, "Invalid ID or Name entered.");
                return;
            }
            ShopObject sObject = DShop.Database.GetItem(type, itemID);
            if (sObject.ItemID != itemID)
            {
                UnturnedChat.Say(caller, string.Format("Item/Vehicle: {0}({1}) not in the shop database.", itemID.AssetFromID(type), itemID));
            }

            UnturnedPlayer player = caller as UnturnedPlayer;
            decimal balance = Uconomy.Instance.Database.GetBalance(caller.Id);

            decimal newCost = sObject.BuyCost;
            decimal totalCost = 0;
            ushort actualCount = 0;

            if (type == ItemType.Item)
            {
                ShopItem sItem = sObject as ShopItem;
                if (sItem.Buy(balance, player, count, out newCost, out totalCost, out actualCount))
                {

                }
                else
                {

                }
            }
            else
            {
                ShopVehicle sVehicle = sObject as ShopVehicle;
                if (sVehicle.Buy(balance, player, out totalCost, out actualCount))
                {

                }
                else
                {

                }
            }

            if (sObject.BuyCost != newCost)
            {
                DShop.Database.AddItem(type, sObject);
                DShop.Instance._OnShopBuy(player, totalCost, actualCount, itemID, type);
            }

        }
    }
}
