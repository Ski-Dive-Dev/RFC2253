using System;

namespace SkiDiveCode.Ldap.Rfc2253
{
    /// <summary>
    /// An RFC 2253 Relative Distinguished Name, with an Attribute Type (<see cref="Type"/>) and Attribute Value
    /// (<see cref="Value"/>).
    /// </summary>
    /// <remarks>
    /// Note that methods within this class can throw exceptions, and those exceptions may include the RDN Type and
    /// Value (for troubleshooting purposes), which may cause inadvertent leakage of information if those
    /// exceptions are not caught.
    /// </remarks>
    public class RelativeDistinguishedName : Rfc2253Base, IRelativeDistinguishedName, INormalizable, IComparable
    {
        public IAttributeComponent Type { get; private set; }
        public IAttributeComponent Value { get; private set; }

        private static readonly Lazy<IRelativeDistinguishedName> lazy =
            new Lazy<IRelativeDistinguishedName>(() => Create());


        /// <summary>
        /// Gets the default <see cref="RelativeDistinguishedName"/> object with an empty type and value.
        /// </summary>
        public static IRelativeDistinguishedName Default => lazy.Value;

        private RelativeDistinguishedName() { /* Disable default constructor for public use. */ }


        private static IRelativeDistinguishedName Create()
        {
            return new RelativeDistinguishedName()
            {
                Type = RdnType.Create(rdnType: String.Empty),
                Value = RdnValue.Create(rdnValue: String.Empty),
            };
        }


        /// <summary>
        /// Creates a new <see cref="RelativeDistinguishedName"/> with the given type and value.
        /// </summary>
        public static IRelativeDistinguishedName Create(IAttributeComponent type, IAttributeComponent value)
        {
            return new RelativeDistinguishedName()
            {
                Type = type ?? throw new ArgumentNullException(nameof(type)),
                Value = value ?? throw new ArgumentNullException(nameof(value)),
            };
        }


        /// <summary>
        /// Gets the Relative Distinguished Name as a normalized string and optionally normalizes the data
        /// structure.
        /// </summary>
        public override string GetAsNormalized(bool convertToNormalized)
        {
            try
            {
                var normalizedRdnString = String.Empty;

                if ((Value as RdnValue).IsMultiValued)
                {
                    normalizedRdnString = Value.GetAsNormalized(convertToNormalized);
                }
                else
                {
                    normalizedRdnString = Type.GetAsNormalized(convertToNormalized) + "=" +
                        Value.GetAsNormalized(convertToNormalized);
                }

                IsNormalized = true;                                        // because all its Type and Value are.
                if (normalizedRdnString == "=")
                    return String.Empty;
                else
                    return normalizedRdnString;
            }
            catch (Exception ex)
            {
                var message = $"An error occurred while normalizing a Relative Distinguished Name," +
                    $" '{Type}={Value}'.";
                throw new Exception(message, ex);
            }
        }


        /// <summary>
        /// Returns the Relative Distinguished Name as a string representation of its current state, which may or
        /// may not be normalized.
        /// </summary>
        public override string ToString()
        {
            switch (Type.Value)
            {
                case "":
                    return String.Empty;
                case multipleValuesPrefix:
                    return Value.ToString();
                default:
                    return Type + "=" + Value;
            }
        }

        public int CompareTo(object obj)
        {
            if (obj == null)
            {
                const int givenObjectPrecedesInSortOrder = 1;
                return givenObjectPrecedesInSortOrder;
            }


            var compareObject = obj as RelativeDistinguishedName;
            if (compareObject == null)
            {
                throw new ArgumentException($"Object is not a {nameof(RelativeDistinguishedName)}.");
            }

            if (Type.Equals(compareObject))
            {
                return String.Compare(Value.ToString(), compareObject.Value.ToString());
            }
            else
            {
                return String.Compare(Type.ToString(), compareObject.Type.ToString());
            }
        }
    }
}