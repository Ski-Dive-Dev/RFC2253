using System;
using System.Collections.Generic;
using System.Text;

namespace Rfc2253
{
    public interface IRelativeDistinguishedName : INormalizable
    {
        IAttributeComponent Type { get; }
        IAttributeComponent Value { get; }
    }
}
