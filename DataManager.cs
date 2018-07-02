namespace DynShop
{
    public interface DataManager
    {
        int SchemaVersion { get; }
        void Unload();
        void CheckSchema();
        bool AddItem(ItemType type, ShopObject shopObject);
        ShopObject GetItem(ItemType type, ushort itemID);
        bool DeleteItem(ItemType type, ushort itemID);

    }
}