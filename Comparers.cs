using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

class BatchEqaulityComparer : IEqualityComparer<List<List<uint>>>
{
    public bool Equals(List<List<uint>>? b1,List<uint>? b2)
    {
        if (ReferenceEquals(b1, b2))
            return true;

        if (b2 is null || b1 is null)
            return false;

        return b1[0].SequenceEqual(b2);
    }
    public bool Equals(List<List<uint>>? b1, List<List<uint>>? b2)
    {
        if (ReferenceEquals(b1, b2))
            return true;

        if (b2 is null || b1 is null)
            return false;

        return b1[0].SequenceEqual(b2[0]);
    }

    public int GetHashCode(List<List<uint>> batch) 
    {
        return (int)(batch.Count) ^ (int)batch[1][0];
    }
}