using Rocket.Core.Plugins;
using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DynShop
{
    public class DShop : RocketPlugin<DShopConfig>
    {
        public static DShop Instance;
        internal static DataManager Database;
        internal static bool Debug = false;

        protected override void Load()
        {
            Instance = this;
            if (Configuration.Instance.Backend == BackendType.MySQL)
                Database = new MySQLDatabaseManager();
            else
                Database = new XMLDatabaseManager();
            if (Database.IsLoaded)
                Instance.Configuration.Instance.DefaultItems();
            Instance.Configuration.Save();
            Debug = Instance.Configuration.Instance.Debug;
        }

        protected override void Unload()
        {
            Database.Unload();
            Database = null;
        }

        public delegate void PlayerDShopBuy(decimal curBallance, UnturnedPlayer player, ushort numItems, ShopObject sObject, ItemType type, decimal newCost, decimal totalCost, ushort totalItems);

        public event PlayerDShopBuy OnShopBuy;

        internal void _OnShopBuy(decimal curBallance, UnturnedPlayer player, ushort numItems, ShopObject sObject, ItemType type, decimal newCost, decimal totalCost, ushort totalItems)
        {
            OnShopBuy?.Invoke(curBallance, player, numItems, sObject, type, newCost, totalCost, totalItems);
        }

        public delegate void PlayerDShopSell(decimal curBallance, UnturnedPlayer player, ushort numItems, ShopObject sObject, ItemType type, decimal newCost, decimal totalCost, ushort totalItems, decimal totalAttatchmentCost);
        public event PlayerDShopSell OnShopSell;

        internal void _OnShopSell(decimal curBallance, UnturnedPlayer player, ushort numItems, ShopObject sObject, ItemType type, decimal newCost, decimal totalCost, ushort totalItems, decimal totalAttatchmentCost)
        {
            OnShopSell?.Invoke(curBallance, player, numItems, sObject, type, newCost, totalCost, totalItems, totalAttatchmentCost);
        }

    }
}
