using System;
using System.Collections.Generic;
using System.Text;

namespace SkiDiveCode.Ldap.Rfc2253
{
    public interface IRelativeDistinguishedName : INormalizable
    {
        IAttributeComponent Type { get; }
        IAttributeComponent Value { get; }
    }
}
