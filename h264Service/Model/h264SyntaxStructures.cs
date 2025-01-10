namespace h264.syntaxstructures

public class SubMbPredLayer
{
    private uint mbtype;
    public SubMbPredLayer(uint mb_type)
    {
        mbtype = mb_type;
    }

    public SubMbPredLayer GetSubMbPredLayer()
    {
        return this;
    }

    public List<uint> sub_mb_type { get; set; } = new List<uint>();
    public List<uint> ref_idx_l0 { get; set; } = new List<uint>();
    public List<uint> ref_idx_l1 { get; set; } = new List<uint>();
    public List<uint> mvd_l0 { get; set; } = new List<uint>();
    public List<uint> mvd_l1 { get; set; } = new List<uint>();
}

// The mb_pred Syntax structure.
public class MbPred
{
    
}