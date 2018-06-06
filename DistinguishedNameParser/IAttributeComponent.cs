using System;
using System.Collections.Generic;
using System.Text;

namespace SkiDiveCode.Ldap.Rfc2253
{
    public interface IAttributeComponent : INormalizable
    {
        string Value { get; }
        bool IsCaseSensitive { get; }
    }
}
