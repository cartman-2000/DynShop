using System.Collections.Generic;

namespace DynShop
{
    public interface DataManager
    {
        void Unload();
        void CheckSchema();
        bool ConvertDB(BackendType toBackend);
        bool AddItem(ItemType type, ShopObject shopObject);
        ShopObject GetItem(ItemType type, ushort itemID);
        Dictionary<ushort, ShopObject> GetAllItems(ItemType type);
        bool DeleteItem(ItemType type, ushort itemID);
        bool IsLoaded { get; set; }
        BackendType Backend { get; }
    }
}