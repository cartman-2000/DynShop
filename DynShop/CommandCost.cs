using fr34kyn01535.Uconomy;
using Rocket.API;
using Rocket.Core.Logging;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Math = System.Math;

namespace DynShop
{
    public class CommandCost : IRocketCommand
    {
        internal static readonly string help = "Displays the cost of an item.";
        internal static readonly string syntax = "<\"Item Name\" | ItemID | h(held item)> || <v> <\"Vehicle Name\" | VehicleID | h(in/lookin at vehicle)>";
        public List<string> Aliases
        {
            get { return new List<string>(); }
        }

        public AllowedCaller AllowedCaller
        {
            get { return AllowedCaller.Both; }
        }

        public string Help
        {
            get { return help; }
        }

        public string Name
        {
            get { return "cost"; }
        }

        public List<string> Permissions
        {
            get { return new List<string> { "dshop.cost" }; }
        }

        public string Syntax
        {
            get { return syntax; }
        }

        public void Execute(IRocketPlayer caller, string[] command)
        {
            ItemType type = ItemType.Item;
            if (command.Length >= 1)
                type = command[0].ToLower() == "v" ? ItemType.Vehicle : ItemType.Item;

            if (command.Length == (type == ItemType.Item ? 0 : 1) || command.Length > (type == ItemType.Item ? 1 : 2))
            {
                UnturnedChat.Say(caller, DShop.Instance.Translate("cost_help3"));
                return;
            }

            if (!DShop.Instance.Database.IsLoaded)
            {
                UnturnedChat.Say(caller, DShop.Instance.Translate("db_load_error"));
                return;
            }

            if (!DShop.GetItemID(caller, command, type, 0, out ushort itemID))
                return;
            ShopObject shopObject = DShop.Instance.Database.GetItem(type, itemID);
            if (shopObject.ItemID != itemID)
            {
                Asset asset = itemID.AssetFromID(type);
                if (type == ItemType.Item)
                    UnturnedChat.Say(caller, DShop.Instance.Translate("item_not_in_db", (asset != null && ((ItemAsset)asset).itemName != null) ? ((ItemAsset)asset).itemName : string.Empty, itemID));
                else
                    UnturnedChat.Say(caller, DShop.Instance.Translate("item_not_in_db", (asset != null && ((VehicleAsset)asset).vehicleName != null) ? ((VehicleAsset)asset).vehicleName : string.Empty, itemID));
                return;
            }

                UnturnedChat.Say(caller, DShop.Instance.Translate(type == ItemType.Item ? "costs_item2" : "costs_vehicle2", shopObject.ItemName, shopObject.ItemID, Math.Round(shopObject.BuyCost, 2), Uconomy.Instance.Configuration.Instance.MoneyName,
                    Math.Round(decimal.Multiply(shopObject.BuyCost, shopObject.SellMultiplier), 2), Uconomy.Instance.Configuration.Instance.MoneyName, Enum.GetName(typeof(RestrictBuySell), shopObject.RestrictBuySell)));
        }
    }
}
