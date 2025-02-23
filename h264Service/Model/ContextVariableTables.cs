
using System.Text.Json.Serialization;
using H264.Types;

namespace ContextVarTables
{
    public class VarTables
    {
        public Table9_12 Table9_12 { get; set; } = default!;
        public Table9_13 Table9_13 { get; set; } = default!;
    }

    public abstract class IdxTable
    {
        [JsonPropertyName("table_name")]
        public string TableName { get; set; } = string.Empty;
        [JsonPropertyName("initialized")]
        public bool Initialized { get; set; }
    }
    public class Table9_12 : IdxTable
    {
        [JsonPropertyName("ctx_variables")]
        public List<CtxTable> CtxTable { get; set; } = default!;
    }

    public class CabacInt
    {
        [JsonPropertyName("0")]
        public List<CtxTable> CabacIntZero { get; set; } = new List<CtxTable>();
        [JsonPropertyName("1")]
        public List<CtxTable> CabacIntOne { get; set; } = new List<CtxTable>();
        [JsonPropertyName("2")]
        public List<CtxTable> CabacIntTwo { get; set; } = new List<CtxTable>();
    }

    public class Table9_13 : IdxTable
    {
        [JsonPropertyName("cabac_init")]
        public CabacInt CabacInt { get; set; } = new CabacInt();
    }
}