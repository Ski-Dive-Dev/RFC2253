using System;
using System.Collections.Generic;
using System.Text;

namespace Rfc2253
{
    public interface IAttributeComponent : INormalizable
    {
        string Value { get; }
        bool IsCaseSensitive { get; }
    }
}
