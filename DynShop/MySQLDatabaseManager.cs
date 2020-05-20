using I18N.West;
using MySql.Data.MySqlClient;
using Rocket.Core.Logging;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DynShop
{
    internal class MySQLDatabaseManager : IDataManager
    {
        private MySqlConnection Connection = null;

        private string Prefix;
        private string TableConfig;
        private string TableItems;
        private string TableVehicles;
        private string TableVehicleInfos;
        private string TableServerInstance;
        private string TableMaps;

        private int ServerInstance;
        private int ServerMapID;

        public bool IsLoaded { get; set; }

        public BackendType Backend { get { return BackendType.MySQL; } }

        internal MySQLDatabaseManager()
        {
            CP1250 cP1250 = new CP1250();
            Prefix = DShop.Instance.Configuration.Instance.DatabaseTablePrefix;
            TableConfig = Prefix + "_config";
            TableItems = Prefix + "_items";
            TableVehicles = Prefix + "_vehicles";
            TableVehicleInfos = Prefix + "_vehicleinfos";
            TableServerInstance = Prefix + "_serverinstance";
            TableMaps = Prefix + "_maps";
            CheckSchema();
        }

        public void CheckSchema()
        {
            MySqlCommand command = null;
            MySqlDataReader reader = null;
            try
            {
                if (!CreateConnection(ref Connection))
                    return;
                ushort version = 0;
                command = Connection.CreateCommand();
                command.CommandText = new QueryBuilder(QueryBuilderType.SHOW).Table(TableConfig).Build();
                object test = command.ExecuteScalar();

                if (test == null)
                {
                    command.CommandText = "CREATE TABLE `" + TableConfig + "` (" +
                            " `key` varchar(40) COLLATE utf8_unicode_ci NOT NULL," +
                            " `value` varchar(40) COLLATE utf8_unicode_ci NOT NULL," +
                            " PRIMARY KEY(`key`)" +
                            ") ENGINE = InnoDB DEFAULT CHARSET = utf8 COLLATE = utf8_unicode_ci;";
                    command.CommandText += "CREATE TABLE `" + TableItems + "` (" +
                        " `ItemID` SMALLINT UNSIGNED NOT NULL," +
                        " `BuyCost` DECIMAL(11, 4) NOT NULL DEFAULT '10.0000'," +
                        " `SellMultiplier` DECIMAL(11, 4) NOT NULL DEFAULT '0.2500'," +
                        " `MinBuyPrice` DECIMAL(11, 4) NOT NULL DEFAULT '0.2000'," +
                        " `ChangeRate` DECIMAL(11, 4) NOT NULL DEFAULT '0.0100'," +
                        " `ItemName` VARCHAR(255) CHARACTER SET utf8 COLLATE utf8_unicode_ci NOT NULL," +
                        " PRIMARY KEY (`ItemID`)" +
                        ") ENGINE = InnoDB DEFAULT CHARSET = utf8 COLLATE = utf8_unicode_ci;";
                    command.CommandText += "CREATE TABLE `" + TableVehicles + "` (" +
                        " `ItemID` SMALLINT UNSIGNED NOT NULL," +
                        " `BuyCost` DECIMAL(11, 4) NOT NULL DEFAULT '150.0000'," +
                        " `SellMultiplier` DECIMAL(11, 4) NOT NULL DEFAULT '0.5000'," +
                        " `ItemName` VARCHAR(255) CHARACTER SET utf8 COLLATE utf8_unicode_ci NOT NULL," +
                        " PRIMARY KEY (`ItemID`)" +
                        ") ENGINE = InnoDB DEFAULT CHARSET = utf8 COLLATE = utf8_unicode_ci;";
                    command.CommandText += "CREATE TABLE `" + TableVehicleInfos + "` (" +
                        " `ID` INT NOT NULL AUTO_INCREMENT," +
                        " `ServerID` INT NOT NULL," +
                        " `MapID` INT NOT NULL," +
                        " `VehicleID` SMALLINT UNSIGNED NOT NULL," +
                        " `SteamID` BIGINT UNSIGNED NOT NULL," +
                        " `BoughtTime` BIGINT NOT NULL," +
                        " PRIMARY KEY (`ID`)" +
                        ") ENGINE = InnoDB CHARSET=utf8 COLLATE utf8_unicode_ci;";
                    command.CommandText += "CREATE TABLE `" + TableServerInstance + "` (" +
                        " `ID` INT NOT NULL AUTO_INCREMENT," +
                        " `InstanceName` VARCHAR(60) NOT NULL," +
                        " `ServerName` VARCHAR(255) NOT NULL," +
                        " PRIMARY KEY (`ID`)" +
                        ") ENGINE = InnoDB CHARSET=utf8 COLLATE utf8_unicode_ci;";
                    command.CommandText += "CREATE TABLE `" + TableMaps + "` (" +
                        " `ID` INT NOT NULL AUTO_INCREMENT," +
                        " `MapName` VARCHAR(255) NOT NULL," +
                        " PRIMARY KEY (`ID`)" +
                        ") ENGINE = InnoDB CHARSET=utf8 COLLATE utf8_unicode_ci;";
                    command.ExecuteNonQuery();
                    CheckVersion(version, command);
                }
                else
                {
                    command.CommandText = new QueryBuilder(QueryBuilderType.SELECT).Column("value").Table(TableConfig).Where("key", "version").Build();
                    object result = command.ExecuteScalar();
                    if (result != null)
                    {
                        if (ushort.TryParse(result.ToString(), out version))
                            CheckVersion(version, command);
                        else
                        {
                            Logger.LogWarning("Error: Database version number not found.");
                            return;
                        }
                    }
                    else
                    {
                        Logger.LogWarning("Error: Database version number not found.");
                        return;
                    }
                }
                command.Parameters.AddWithValue("@instname", Provider.serverID.ToLower());
                command.Parameters.AddWithValue("@servername", Provider.serverName);
                command.CommandText = new QueryBuilder(QueryBuilderType.SELECT).Column("ID").Column("ServerName").Table(TableServerInstance).Where("InstanceName", "@instname").Build();
                reader = command.ExecuteReader();
                if (reader.Read())
                {
                    ServerInstance = reader.GetInt32("ID");
                    if (reader.GetString("ServerName") != Provider.serverName)
                    {
                        reader.Dispose();
                        command.CommandText = new QueryBuilder(QueryBuilderType.UPDATE).Table(TableServerInstance).Column("ServerName", "@servername").Where("InstanceName", "@instname").Build();
                        command.ExecuteNonQuery();
                    }
                }
                // No value in the database, add one.
                else
                {
                    if (!reader.IsClosed)
                        reader.Dispose();
                    command.CommandText = new QueryBuilder(QueryBuilderType.INSERT).Table(TableServerInstance).Column("InstanceName", "@instname").Column("ServerName", "@servername").Build();
                    command.ExecuteNonQuery();
                    command.CommandText = new QueryBuilder(QueryBuilderType.SELECT).Column("ID").Table(TableServerInstance).Where("InstanceName", "@instname").Build();
                    ServerInstance = int.Parse(command.ExecuteScalar().ToString());
                }
                if (!reader.IsClosed)
                    reader.Dispose();

                command.Parameters.AddWithValue("@mapname", Provider.map.ToLower());
                command.CommandText = new QueryBuilder(QueryBuilderType.SELECT).Column("ID").Table(TableMaps).Where("MapName", "@mapname").Build();
                object mapID = command.ExecuteScalar();
                if (!int.TryParse(mapID != null ? mapID.ToString() : "null", out ServerMapID))
                {
                    // No value in the database, add one.
                    command.CommandText = new QueryBuilder(QueryBuilderType.INSERT).Table(TableMaps).Column("MapName", "@mapname").Build();
                    command.ExecuteNonQuery();
                    command.CommandText = new QueryBuilder(QueryBuilderType.SELECT).Column("ID").Table(TableMaps).Where("MapName", "@mapname").Build();
                    ServerMapID = int.Parse(command.ExecuteScalar().ToString());
                }
                IsLoaded = true;
            }
            catch (MySqlException ex)
            {
                Logger.LogException(ex);
            }
            finally
            {
                if (reader != null && !reader.IsClosed)
                    reader.Dispose();
                if (command != null)
                    command.Dispose();
                Connection.Close();
            }
        }

        private void CheckVersion(ushort version, MySqlCommand command)
        {
            ushort updatingVersion = 0;
            try
            {
                bool updated = false;
                if (version < 1)
                {
                    updated = true;
                    updatingVersion = 1;
                    command.CommandText = new QueryBuilder(QueryBuilderType.INSERT).Table(TableConfig).Column("key", "version").Column("value", updatingVersion).Build();
                    command.ExecuteNonQuery();
                }
                if (version < 2)
                {
                    updated = true;
                    updatingVersion = 2;
                    command.CommandText = new QueryBuilder(QueryBuilderType.ALTERTABLE_ADD).Table(TableItems).AlterColumn("MaxBuyPrice", "DECIMAL(15,4) NOT NULL DEFAULT '0'").After("ChangeRate").Build();
                    command.CommandText += new QueryBuilder(QueryBuilderType.ALTERTABLE_CHANGE).Table(TableItems).ChangeColumn("BuyCost", "BuyCost", "DECIMAL(11,6) NOT NULL DEFAULT '10'").
                        ChangeColumn("SellMultiplier", "SellMultiplier", "DECIMAL(11,6) NOT NULL DEFAULT '0.25'").ChangeColumn("ChangeRate", "ChangeRate", "DECIMAL(11,6) NOT NULL DEFAULT '0.01'").Build();
                   command.ExecuteNonQuery();
                }
                if (version < 3)
                {
                    updated = true;
                    updatingVersion = 3;
                    command.CommandText = new QueryBuilder(QueryBuilderType.ALTERTABLE_ADD_INDEX).Table(TableMaps).IndexColumn(IndexType.Unique, null, "MapName").Build();
                    command.CommandText += new QueryBuilder(QueryBuilderType.ALTERTABLE_ADD_INDEX).Table(TableServerInstance).IndexColumn(IndexType.Unique, null, "InstanceName").Build();
                    command.CommandText += new QueryBuilder(QueryBuilderType.ALTERTABLE_ADD_INDEX).Table(TableVehicleInfos).IndexColumn(IndexType.Index, "VehicleOwner", "VehicleID", "SteamID").Build();
                    command.ExecuteNonQuery();
                }
                if (version < 4)
                {
                    updated = true;
                    updatingVersion = 4;
                    command.CommandText = new QueryBuilder(QueryBuilderType.ALTERTABLE_ADD).Table(TableItems).AlterColumn("RestrictBuySell", "TINYINT NOT NULL DEFAULT '0'").After("MaxBuyPrice").Build();
                    command.CommandText += new QueryBuilder(QueryBuilderType.ALTERTABLE_ADD).Table(TableVehicles).AlterColumn("RestrictBuySell", "TINYINT NOT NULL DEFAULT '0'").After("SellMultiplier").Build();
                    command.ExecuteNonQuery();
                }
                if (updated)
                {
                    if (version >= 1)
                    {
                        command.CommandText = new QueryBuilder(QueryBuilderType.UPDATE).Table(TableConfig).Column("value", updatingVersion).Where("key", "version").Build();
                        command.ExecuteNonQuery();
                    }
                    Logger.LogWarning("The dshop database has been updated to version: " + updatingVersion.ToString());
                }
            }
            catch (MySqlException ex)
            {
                HandleException(ex, "Failed in updating Database schema to version " + updatingVersion + ", you may have to do a manual update to the database schema.");
            }
        }

        private void HandleException(MySqlException ex, string msg = null)
        {
            if (ex.Number == 0)
            {
                Logger.LogException(ex, "Error: Connection lost to database server.");
            }
            else
            {
                Logger.LogWarning(ex.Number.ToString() + ":" + ((MySqlErrorCode)ex.Number).ToString());
                Logger.LogException(ex, msg);
            }
        }

        public bool ConvertDB(BackendType toBackend)
        {
            bool result = false;
            if (toBackend == Backend)
                return result;
            else if (toBackend == BackendType.XML)
            {
                IDataManager database = new XMLDatabaseManager();
                if (!database.IsLoaded)
                    return result;
                try
                {
                    Dictionary<ushort, ShopObject> items = GetAllItems(ItemType.Item);
                    Dictionary<ushort, ShopObject> vehicles = GetAllItems(ItemType.Vehicle);

                    foreach (ShopObject item in items.Values)
                    {
                        database.AddItem(ItemType.Item, item);
                    }
                    foreach (ShopObject vehicle in vehicles.Values)
                    {
                        database.AddItem(ItemType.Vehicle, vehicle);
                    }
                    result = true;
                }
                catch(MySqlException ex)
                {
                    HandleException(ex);
                }
                finally
                {
                    if (database.IsLoaded)
                        database.Unload();
                    database = null;
                }
            }
            return result;
        }

        public void SanityCheck()
        {

        }

        public Dictionary<ushort, ShopObject> GetAllItems(ItemType type)
        {
            MySqlCommand command = null;
            MySqlDataReader reader = null;
            Dictionary<ushort, ShopObject> itemList = new Dictionary<ushort, ShopObject>();
            ShopObject item = null;
            try
            {
                if (!CreateConnection(ref Connection))
                    return itemList;
                command = Connection.CreateCommand();
                if (type == ItemType.Item)
                    command.CommandText = new QueryBuilder(QueryBuilderType.SELECT).Column("ItemID").Column("BuyCost").Column("SellMultiplier").Column("MinBuyPrice").Column("ChangeRate").Column("MaxBuyPrice").Column("RestrictBuySell").Table(TableItems).Build();
                else
                    command.CommandText = new QueryBuilder(QueryBuilderType.SELECT).Column("ItemID").Column("BuyCost").Column("SellMultiplier").Column("RestrictBuySell").Table(TableVehicles).Build();
                reader = command.ExecuteReader();
                if (!reader.HasRows)
                    return itemList;
                while (reader.Read())
                {
                    if (type == ItemType.Item)
                    {
                        item = ShopObjectBuild(ItemType.Item, reader);
                        itemList.Add(item.ItemID, item);
                    }
                    else
                    {
                        item = ShopObjectBuild(ItemType.Vehicle, reader);
                        itemList.Add(item.ItemID, item);

                    }
                }
                reader.Dispose();
            }
            catch (MySqlException ex)
            {
                HandleException(ex);
            }
            finally
            {
                if (command != null)
                    command.Dispose();
                if (reader != null)
                    reader.Dispose();
                Connection.Close();
            }
            return itemList;
        }


        private bool CreateConnection(ref MySqlConnection connection)
        {
            try
            {
                if (connection == null)
                    connection = new MySqlConnection(string.Format("SERVER={0};DATABASE={1};UID={2};PASSWORD={3};PORT={4};", DShop.Instance.Configuration.Instance.DatabaseAddress, DShop.Instance.Configuration.Instance.DatabaseName,
                        DShop.Instance.Configuration.Instance.DatabaseUsername, DShop.Instance.Configuration.Instance.DatabasePassword, DShop.Instance.Configuration.Instance.DatabasePort));
                connection.Open();
                return true;
            }
            catch(MySqlException ex)
            {
                Logger.LogException(ex, "Failed to connect to the database server!");
                return false;
            }
        }

        public void Unload()
        {
            Connection.Dispose();
            Connection = null;
            IsLoaded = false;
        }

        private ShopObject ShopObjectBuild(ItemType type, MySqlDataReader reader)
        {
            if (type == ItemType.Item)
                return new ShopItem(reader.GetUInt16("ItemID"), reader.GetDecimal("BuyCost"), reader.GetDecimal("SellMultiplier"), reader.GetDecimal("MinBuyPrice"), reader.GetDecimal("ChangeRate"), reader.GetDecimal("MaxBuyPrice"), (RestrictBuySell)reader.GetByte("RestrictBuySell"));
            else
                return new ShopVehicle(reader.GetUInt16("ItemID"), reader.GetDecimal("BuyCost"), reader.GetDecimal("SellMultiplier"), (RestrictBuySell)reader.GetByte("RestrictBuySell"));
        }

        public bool AddItem(ItemType type, ShopObject shopObject)
        {
            MySqlCommand command = null;
            bool result = false;
            try
            {
                if (!CreateConnection(ref Connection))
                    return result;
                command = Connection.CreateCommand();
                command.Parameters.AddWithValue("@itemName", shopObject.ItemName);

                if (type == ItemType.Item)
                {
                    ShopItem item = shopObject as ShopItem;
                    command.CommandText = new QueryBuilder(QueryBuilderType.INSERT).Table(TableItems).Column("ItemID", item.ItemID).Column("BuyCost", item.BuyCost).Column("SellMultiplier", item.SellMultiplier).Column("MinBuyPrice", item.MinBuyPrice).
                        Column("ChangeRate", item.Change).Column("MaxBuyPrice", item.MaxBuyPrice).Column("RestrictBuySell", (byte)item.RestrictBuySell).Column("ItemName", "@itemName").DuplicateInsertUpdate().Build();
                }
                else
                {
                    ShopVehicle vehicle = shopObject as ShopVehicle;
                    command.CommandText = new QueryBuilder(QueryBuilderType.INSERT).Table(TableVehicles).Column("ItemID", vehicle.ItemID).Column("BuyCost", vehicle.BuyCost).Column("SellMultiplier", vehicle.SellMultiplier).
                        Column("RestrictBuySell", (byte)vehicle.RestrictBuySell).Column("ItemName", "@itemName").DuplicateInsertUpdate().Build();
                }
                command.ExecuteNonQuery();
                result = true;
            }
            catch (MySqlException ex)
            {
                HandleException(ex);
            }
            finally
            {
                if (command != null)
                    command.Dispose();
                Connection.Close();
            }
            return result;
        }

        public ShopObject GetItem(ItemType type, ushort itemID)
        {
            ShopObject shopObject = new ShopObject();
            MySqlDataReader reader = null;
            MySqlCommand command = null;
            try
            {
                if (!CreateConnection(ref Connection))
                    return shopObject;
                command = Connection.CreateCommand();
                if (type == ItemType.Item)
                    command.CommandText = new QueryBuilder(QueryBuilderType.SELECT).Column("ItemID").Column("BuyCost").Column("SellMultiplier").Column("MinBuyPrice").Column("ChangeRate").Column("MaxBuyPrice").Column("RestrictBuySell").Where("ItemID", itemID).Table(TableItems).Build();
                else
                    command.CommandText = new QueryBuilder(QueryBuilderType.SELECT).Column("itemID").Column("BuyCost").Column("SellMultiplier").Column("RestrictBuySell").Table(TableVehicles).Where("ItemID", itemID).Build();
                reader = command.ExecuteReader();
                if (reader.Read())
                {
                        shopObject = ShopObjectBuild(type, reader);
                }
            }
            catch (MySqlException ex)
            {
                HandleException(ex);
            }
            finally
            {
                if (reader != null)
                    reader.Dispose();
                if (command != null)
                    command.Dispose();
                Connection.Close();
            }
            // Returns either ShopItem or ShopVehicle based type if id is found(with conversion), ShopObject, if it's not found.
            return shopObject;
        }


        public bool DeleteItem(ItemType type, ushort itemID)
        {
            bool result = false;
            MySqlCommand command = null;
            if (GetItem(type, itemID).ItemID != itemID)
                return result;
            else
            {
                try
                {
                    if (!CreateConnection(ref Connection))
                        return result;
                    command = Connection.CreateCommand();
                    command.CommandText = new QueryBuilder(QueryBuilderType.DELETE).Table(type == ItemType.Item ? TableItems : TableVehicles).Where("ItemID", itemID).Build();
                    command.ExecuteNonQuery();
                    result = true;
                }
                catch(MySqlException ex)
                {
                    HandleException(ex);
                }
                finally
                {
                    if (command != null)
                        command.Dispose();
                    Connection.Close();
                }
                return result;
            }
        }

        public bool AddVehicleInfo(ulong SteamID, ushort vehicleID)
        {
            VehicleInfo info = new VehicleInfo(SteamID, vehicleID);
            bool result = false;
            MySqlCommand command = null;
            try
            {
                if (!CreateConnection(ref Connection))
                    return result;
                command = Connection.CreateCommand();
                command.CommandText = new QueryBuilder(QueryBuilderType.INSERT).Table(TableVehicleInfos).Column("ServerID", ServerInstance).Column("MapID", ServerMapID).Column("VehicleID", vehicleID).Column("SteamID", SteamID).Column("BoughtTime", info.TimeBought.ToBinary()).Build();
                command.ExecuteNonQuery();
                result = true;
            }
            catch(MySqlException ex)
            {
                HandleException(ex);
            }
            finally
            {
                if (command != null)
                    command.Dispose();
                Connection.Close();
            }
            return result;
        }

        public VehicleInfo GetVehicleInfo(ulong SteamID, ushort vehicleID)
        {
            VehicleInfo vInfo = null;
            MySqlDataReader reader = null;
            MySqlCommand command = null;
            try
            {
                if (!CreateConnection(ref Connection))
                    return vInfo;
                command = Connection.CreateCommand();
                QueryBuilder qB = new QueryBuilder(QueryBuilderType.SELECT).Column("a.ID").Column("a.ServerID").Column("a.MapID").Column("a.VehicleID").Column("a.SteamID").Column("a.BoughtTime").Column("b.MapName").Table(TableVehicleInfos, "a").LeftJoin(TableMaps, "a.MapID", "b.ID", "b").Where("a.SteamID", SteamID).Where("a.VehicleID", vehicleID).WhereAnd().OrderBy("a.ID", true).Limit(1);
                if (!DShop.Instance.Configuration.Instance.IgnoreVehicleInfoMap)
                    qB.Where("a.MapID", ServerMapID);
                if (!DShop.Instance.Configuration.Instance.IgnoreVehicleInfoSpecificServer)
                    qB.Where("a.ServerID", ServerInstance);
                command.CommandText = qB.Build();
                reader = command.ExecuteReader();
                if (reader.Read())
                {
                    vInfo = new VehicleInfo();
                    vInfo.InfoID = reader.GetInt32("ID");
                    vInfo.MapName = reader.GetString("MapName");
                    vInfo.SteamID = reader.GetUInt64("SteamID");
                    vInfo.TimeBought = DateTime.FromBinary(reader.GetInt64("BoughtTime"));
                    vInfo.VehicleID = reader.GetUInt16("VehicleID");
                }
            }
            catch (MySqlException ex)
            {
                HandleException(ex);
            }
            finally
            {
                if (reader != null)
                    reader.Dispose();
                if (command != null)
                    command.Dispose();
                Connection.Close();
            }
            return vInfo;
        }

        public bool DeleteVehicleInfo(VehicleInfo vInfo)
        {
            bool result = false;
            MySqlCommand command = null;
            try
            {
                if (!CreateConnection(ref Connection))
                    return result;
                command = Connection.CreateCommand();
                command.CommandText = new QueryBuilder(QueryBuilderType.DELETE).Table(TableVehicleInfos).Where("ID", vInfo.InfoID).Build();
                command.ExecuteNonQuery();
                result = true;
            }
            catch(MySqlException ex)
            {
                HandleException(ex);
            }
            finally
            {
                if (command != null)
                    command.Dispose();
                Connection.Close();
            }
            return result;
        }
    }
}
