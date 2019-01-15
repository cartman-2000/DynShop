using Rocket.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DynShop
{
    public class QueryBuilder
    {
        private string _table = string.Empty;
        private string _tableAs = string.Empty;
        private Dictionary<string, string> _columns = new Dictionary<string, string>();
        private Dictionary<string, QueryJoin> _leftJoins = new Dictionary<string, QueryJoin>();
        private Dictionary<string, string> _where = new Dictionary<string, string>();
        private bool _whereAnd = false;
        private string _orderByColumn = string.Empty;
        private bool _orderByAsc = true;
        private bool _useLimit = false;
        private int _limitCount = 1;
        private int _limitIndex = 0;
        private bool _duplicateInsertUpdate = false;
        private QueryBuilderType _type;

        public QueryBuilder(QueryBuilderType queryType)
        {
            _type = queryType;
        }

        public QueryBuilder Column(string columnName, object value = null)
        {
            string sValue = string.Empty;
            if (value != null && !string.IsNullOrEmpty(value.ToString()))
            {
                sValue = value.ToString();
                if (!sValue.Contains("@"))
                    sValue = "'" + sValue + "'";
            }
            _columns.Add(columnName, sValue);
            return this;
        }
        public QueryBuilder DuplicateInsertUpdate()
        {
            _duplicateInsertUpdate = true;
            return this;
        }
        public QueryBuilder Table(string tableName, string As = null)
        {
            _table = tableName;
            _tableAs = As;
            return this;
        }
        public QueryBuilder LeftJoin(string otherTable, string ColumnA, string ColumnB, string As = null)
        {
            _leftJoins.Add(otherTable, new QueryJoin(ColumnA, ColumnB, As));
            return this;
        }
        public QueryBuilder OrderBy(string column, bool byAsc)
        {
            _orderByColumn = column;
            _orderByAsc = byAsc;
            return this;
        }
        public QueryBuilder Limit(int count, int index = 0)
        {
            _useLimit = true;
            _limitCount = count;
            _limitIndex = index;
            return this;
        }
        public QueryBuilder Where(string column, object value)
        {
            string sValue = string.Empty;
            if (value != null && !string.IsNullOrEmpty(value.ToString()))
            {
                sValue = value.ToString();
                if (!sValue.Contains("@"))
                    sValue = "'" + sValue + "'";
            }
            _where.Add(column, sValue);
            return this;
        }
        public QueryBuilder WhereAnd()
        {
            _whereAnd = true;
            return this;
        }

        public string Build()
        {
            string query = string.Empty;
            int i = 0;
            switch (_type)
            {
                case QueryBuilderType.INSERT:
                    {
                        query = "INSERT INTO `" + _table + "` ";
                        query += "(`" + string.Join("`, `", _columns.Keys.ToArray()) + "`) VALUES (" + string.Join(", ", _columns.Values.ToArray()) + ")";
                        if (_duplicateInsertUpdate)
                        {
                            query += " ON DUPLICATE KEY UPDATE ";
                            foreach (string key in _columns.Keys)
                            {
                                query += "`" + key + "` = VALUES(`" + key + "`)";
                                i++;
                                if (i < _columns.Count)
                                    query += ", ";
                            }
                        }
                        break;
                    }
                case QueryBuilderType.SELECT:
                    {
                        query = "SELECT ";
                        foreach (KeyValuePair<string, string> val in _columns)
                        {
                            query += ColumnFormat(val.Key);
                            i++;
                            if (i < _columns.Count)
                                query += ", ";
                        }
                        query += " FROM `" + _table + "`" + (!string.IsNullOrEmpty(_tableAs) ? " AS " + _tableAs : string.Empty);
                        if (_leftJoins.Count > 0)
                        {
                            foreach (KeyValuePair<string, QueryJoin> value in _leftJoins)
                            {
                                query += " LEFT JOIN `" + value.Key + "`" + (!string.IsNullOrEmpty(value.Value.As) ? " AS " + value.Value.As : string.Empty) + " ON " + value.Value.ValueA + " = " + value.Value.ValueB;
                            }
                        }
                        if (_where.Count > 0)
                        {
                            query += " WHERE ";
                            i = 0;
                            foreach (KeyValuePair<string, string> value in _where)
                            {
                                query += ColumnFormat(value.Key) + " = " + value.Value;
                                i++;
                                if (i < _where.Count)
                                    query += _whereAnd ? " AND " : " OR ";
                            }
                        }
                        if (!string.IsNullOrEmpty(_orderByColumn))
                            query += " ORDER BY " + ColumnFormat(_orderByColumn) + " " + (_orderByAsc ? "ASC" : "DESC");

                        if (_useLimit)
                            query += " LIMIT " + _limitIndex + ", " + _limitCount;
                        break;
                    }
                case QueryBuilderType.UPDATE:
                    {
                        query = "UPDATE `" + _table + "` SET ";
                        foreach (KeyValuePair<string, string> value in _columns)
                        {
                            query += "`" + value.Key + "` = " + value.Value;
                            i++;
                            if (i < _columns.Count)
                                query += ", ";
                        }
                        break;
                    }
                case QueryBuilderType.DELETE:
                    {
                        query = "DELETE FROM `" + _table + "` WHERE ";
                        foreach (KeyValuePair<string, string> value in _where)
                        {
                            query += "`" + value.Key + "` = " + value.Value;
                            i++;
                            if (i < _where.Count)
                                query += _whereAnd ? " AND " : " OR ";
                        }
                        break;
                    }
                case QueryBuilderType.SHOW:
                    {
                        query = "SHOW TABLES LIKE '" + _table + "'";
                        break;
                    }
            }
            query += ";";
            if (DShop.Debug)
                Logger.LogWarning("Prepared query string: \"" + query + "\"");
            return query;
        }

        private string ColumnFormat(string value)
        {
            string query = string.Empty;
                if (value.Contains('.'))
                {
                    query += value.Split('.')[0] + ".`" + value.Split(new char[] { '.' }, 2)[1] + "`";
                }
                else
                {
                    query += "`" + value+ "`";
                }
            return query;
        }
    }
    public enum QueryBuilderType
    {
        INSERT,
        SELECT,
        UPDATE,
        DELETE,
        SHOW,
    }
}
