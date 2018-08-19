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
        }

        protected override void Unload()
        {
            Database.Unload();
            Database = null;
        }

        public delegate void PlayerDShopBuy(UnturnedPlayer player, decimal amt, ushort numItems, ushort itemID, ItemType type);
        public event PlayerDShopBuy OnShopBuy;

        internal void _OnShopBuy(UnturnedPlayer player, decimal amt, ushort numItems, ushort itemID, ItemType type)
        {
            OnShopBuy?.Invoke(player, amt, numItems, itemID, type);
        }

        public delegate void PlayerDShopSell(UnturnedPlayer player, decimal amt, ushort numItems, ushort itemID, ItemType type);
        public event PlayerDShopSell OnShopSell;

        internal void _OnShopSell(UnturnedPlayer player, decimal amt, ushort numItems, ushort itemID, ItemType type)
        {
            OnShopSell?.Invoke(player, amt, numItems, itemID, type);
        }

    }
}
