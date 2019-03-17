namespace DynShop
{
    internal class QueryJoin
    {
        internal QueryJoin(string _valueA, string _valueB, string _as)
        {
            ValueA = _valueA;
            ValueB = _valueB;
            As = _as;
        }

        internal string ValueA;
        internal string ValueB;
        internal string As;
    }
}