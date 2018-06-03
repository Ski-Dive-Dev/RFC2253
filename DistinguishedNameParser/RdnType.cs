using System;
using System.Collections.Generic;
using System.Text;

namespace Rfc2253
{
    public class RdnType : Rfc2253Base, IAttributeComponent, INormalizable
    {
        public string Value { get; private set; }
        public bool IsOid { get; private set; }

        private RdnType() { /* Disable default constructor for public use. */ }

        public static IAttributeComponent Create(string rdnType, bool isOid = false, bool isCaseSensitive = false)
        {
            return new RdnType()
            {
                Value = rdnType ?? throw new ArgumentNullException(nameof(RdnType)),
                IsOid = isOid,
                IsCaseSensitive = isCaseSensitive
            };
        }


        /// <summary>
        /// Returns the attribute type as a normalized string.
        /// </summary>
        public override string GetAsNormalized(bool convertAttributeTypeToNormalized)
        {
            try
            {
                var normalizedAttributeType = Value;

                if (IsOid)
                {
                    // Note: RFC 2253: "This form [octothrope character ... followed by the hexadecimal
                    // representation of each of the bytes of the BER encoding of the X.500 AttributeValue] SHOULD
                    // be used if the Attribute type is of the dotted-decimal form [i.e., OID]".

                    // Note: Per RFC 2253, not considering mixed case (e.g., "Oid.")
                    if (Value.StartsWith("OID.") || Value.StartsWith("oid."))
                    {
                        const int lengthOfOidPrefix = 4;
                        normalizedAttributeType = normalizedAttributeType.Substring(startIndex: lengthOfOidPrefix);
                    }
                }
                else if (!IsCaseSensitive)
                {
                    normalizedAttributeType = ConvertToLowerCase(normalizedAttributeType);
                }

                if (convertAttributeTypeToNormalized)
                {
                    Value = normalizedAttributeType;
                    IsNormalized = true;
                }

                return normalizedAttributeType;
            }
            catch (Exception ex)
            {
                var message = $"An error occurred while normalizing a Distinguished Name, '{Value}'.";
                throw new Exception(message, ex);
            }
        }


        /// <summary>
        /// Returns the attribute type as a string representation of its current state, which may or may not be
        /// normalized.
        /// </summary>
        public override string ToString() => Value;
    }
}
