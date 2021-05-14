// Copyright Karel Kroeze, 2021-2021.
// SelfAwareHR/SelfAwareHR/SpecLevel.cs

using System;
using JetBrains.Annotations;

namespace SelfAwareHR.Utilities
{
    public struct SpecLevel : IEquatable<SpecLevel>, IComparable<SpecLevel>
    {
        public string Spec;
        public int    Level;

        public static implicit operator SpecLevel(FeatureBase feature)
        {
            return new SpecLevel(feature.Spec, feature.Level);
        }

        public static implicit operator SpecLevel(RoleSpecLevel rsl)
        {
            return new SpecLevel(rsl.Spec, rsl.Level);
        }

        public SpecLevel(string spec, int level)
        {
            Spec  = spec;
            Level = level;
        }

        public static bool operator ==(SpecLevel self, object other)
        {
            return self.Equals(other);
        }

        public static bool operator !=(SpecLevel self, object other)
        {
            return !(self == other);
        }

        public bool Equals(SpecLevel other)
        {
            return Spec == other.Spec && Level == other.Level;
        }

        public override bool Equals(object obj)
        {
            return obj is SpecLevel other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Spec != null ? Spec.GetHashCode() : 0) * 397) ^ Level;
            }
        }

        public override string ToString()
        {
            return $"{Spec} ({Level})";
        }

        public int CompareTo(SpecLevel other)
        {
            return Spec != other.Spec
                ? string.Compare(Spec, other.Spec, StringComparison.Ordinal)
                : Level.CompareTo(other.Level);
        }
    }


    public struct RoleSpecLevel : IEquatable<RoleSpecLevel>, IComparable<RoleSpecLevel>, IComparable<SpecLevel>
    {
        public Employee.EmployeeRole Role;
        public string                Spec;
        public int                   Level;

        public RoleSpecLevel(Employee.EmployeeRole role, [NotNull] string spec, int level)
        {
            Role  = role;
            Spec  = spec ?? throw new ArgumentNullException(nameof(spec));
            Level = level;
        }

        public static bool operator ==(RoleSpecLevel self, object other)
        {
            return self.Equals(other);
        }

        public static bool operator !=(RoleSpecLevel self, object other)
        {
            return !(self == other);
        }

        public RoleSpecLevel(Employee.EmployeeRole role, SpecLevel specLevel) : this(
            role, specLevel.Spec, specLevel.Level)
        {
        }

        public bool Equals(RoleSpecLevel other)
        {
            return Role == other.Role && Spec == other.Spec && Level == other.Level;
        }

        public override bool Equals(object obj)
        {
            return obj is RoleSpecLevel other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int) Role;
                hashCode = (hashCode * 397) ^ (Spec != null ? Spec.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Level;
                return hashCode;
            }
        }

        public int CompareTo(RoleSpecLevel other)
        {
            return Spec != other.Spec
                ? string.Compare(Spec, other.Spec, StringComparison.Ordinal)
                : Level.CompareTo(other.Level);
        }

        public int CompareTo(SpecLevel other)
        {
            return Spec != other.Spec
                ? string.Compare(Spec, other.Spec, StringComparison.Ordinal)
                : Level.CompareTo(other.Level);
        }

        public override string ToString()
        {
            return $"{Role} :: {Spec} ({Level})";
        }
    }
}