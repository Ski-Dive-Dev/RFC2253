using System;
using System.Text.RegularExpressions;

namespace SkiDiveCode.Ldap.Rfc2253
{
    public class Rfc2253Section3
    {
        // Note: RFC 2253 limits characters to the ASCII set (non-ASCII multi-byte characters need to be escaped as
        // hex digits)

        #region Rfc2253 Section 3
        // Note: Parses both LDAPv3 as well as LDAPv2 (per RFC 2253).  However, per RFC 2253, Distinguished Names
        // should not OUTPUT LDAPv2 syntax.

        private const string backslash = @"\\";         // Regex backslash needs to be escaped to match \ literally
        private const string octothorpe = @"\#";     // Regex Comment char needs to be escaped to match # literally
        private const string quotation = "\"";
        private const string digit = @"\d";
        private const string alpha = @"[A-Za-z]";
        private const string hexChar = @"[0-9A-Fa-f]";
        private const string hexPair = hexChar + hexChar;
        private static readonly string hexString = $@"(?:{hexPair})+";
        private const string special = @",=+<>#;";
        private static readonly string pair = $@"{backslash}([{special}{backslash}{quotation}]|{hexPair})";
 
 
        private static readonly string stringChar = $@"[^{special}{backslash}{quotation}]";
        private static readonly string quoteChar = $@"[^{backslash}{quotation}]";

        // Reorganized from RFC since the Regex parser is "greedy":
        private static readonly string stringRegexPattern =
            $@"{octothorpe}{hexString}" +
            $@"|{quotation}({quoteChar}|{pair})*{quotation}" +                  // Quoted only from LDAPv2
            $@"|({stringChar}|{pair})*";

        private static readonly string attributeValue = stringRegexPattern;
        private static readonly string oid =                                    // OID prefix only from LDAPv2
                 $@"(oid\.|OID\.)?{digit}+(\.{digit}+)*";
        private static readonly string keyChar = $@"{alpha}|{digit}|-";

        // There seems to be an error in RFC 2253 - it describes at least one keychar (1*keychar) follows ALPHA.
        // Requiring at least two characters for the attributeType would cause "L=", "O=" and "C=" to fail.
        private static readonly string attributeType = $@"{oid}|{alpha}(?:{keyChar})*"; // Reordered for greedy Regex

        private static readonly string attributeTypeAndValue =
            $@"{attributeType}" +
            $@"[ ]*=[ ]*" +                                                     // Spaces [ ]* only from LDAPv2
            $@"{attributeValue}";

        private static readonly string name_Component = $@"{attributeTypeAndValue}" +
            $@"(([ ]*\+[ ]*{attributeTypeAndValue})+)?";                        // Spaces [ ]* only from LDAPv2

        private const string rdnDelimiters = @",;";                             // ";" only from LDAPv2
        private static readonly string name = $@"{name_Component}";
        private static readonly string nextName =
            $@"[ ]*[{rdnDelimiters}][ ]*{name_Component}";                      // Spaces [ ]* only from LDAPv2


        // *Note: RFC 2253 specifically calls for space (' ' ASCII 32) characters, not just any whitespace char.

        #endregion
    }
}
