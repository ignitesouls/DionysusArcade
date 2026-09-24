using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversalReplacementRandomizer;

public interface IReplacementValidator
{
    public bool Validate(int target, int replacement);
}
