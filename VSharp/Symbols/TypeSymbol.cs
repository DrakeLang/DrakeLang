//------------------------------------------------------------------------------
// VSharp - Viv's C#-esque sandbox.
// Copyright (C) 2019  Vivian Vea
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace VSharp.Symbols
{
    /// <summary>
    /// Base type for type-related symbols
    /// </summary>
    public abstract class BaseTypeSymbol : Symbol
    {
        private protected BaseTypeSymbol(string name) : base(name)
        { }

        #region Operators

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj))
                return true;

            return obj is BaseTypeSymbol other &&
                Name == other.Name &&
                GetType() == other.GetType();
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name);
        }

        public static bool operator ==(BaseTypeSymbol? left, BaseTypeSymbol? right)
        {
            if (left is null)
                return right is null;

            return left.Equals(right);
        }

        public static bool operator !=(BaseTypeSymbol? left, BaseTypeSymbol? right)
        {
            return !(left == right);
        }

        #endregion Operators
    }

    internal class GenericTypeSymbolBuilder
    {
        public GenericTypeSymbolBuilder()
        {
        }

        public GenericTypeSymbolBuilder(GenericTypeSymbol basedOn)
        {
            BaseSymbolName = basedOn.BaseSymbolName;
            Namespace = basedOn.Namespace;
            BaseType = basedOn.BaseType;
            GenericArgumentsDescriptions = basedOn.GenericArgumentsDescriptions;
        }

        /// <summary>
        /// Gets or sets the name of the type.
        /// </summary>
        public string? BaseSymbolName { get; set; }

        /// <summary>
        /// Gets or sets the namespace of the type.
        /// </summary>
        public NamespaceSymbol? Namespace { get; set; }

        /// <summary>
        /// Gets or sets the parent type. If null, the type will just inherit from <see cref="TypeSymbol.Object"/>.
        /// </summary>
        public TypeBaseSymbol? BaseType { get; set; }

        /// <summary>
        /// Gets or sets the generic type arguments of the type.
        /// </summary>
        public ImmutableArray<GenericTypeArgumentSymbol> GenericArgumentsDescriptions { get; set; } = ImmutableArray<GenericTypeArgumentSymbol>.Empty;

        public virtual GenericTypeSymbol Build() => new(this);
    }

    internal class TypeBaseSymbolBuilder : GenericTypeSymbolBuilder
    {
        public TypeBaseSymbolBuilder()
        { }

        public TypeBaseSymbolBuilder(GenericTypeSymbol basedOn) : base(basedOn)
        {
            if (basedOn is TypeBaseSymbol baseType)
            {
                GenericTypeArguments = baseType.GenericTypeArguments;
            }
        }

        public virtual ImmutableArray<BaseTypeSymbol> GenericTypeArguments { get; set; } = ImmutableArray<BaseTypeSymbol>.Empty;

        public override TypeBaseSymbol Build() => new(this);
    }

    internal sealed class TypeSymbolBuilder : TypeBaseSymbolBuilder
    {
        public TypeSymbolBuilder()
        {
            GenericTypeArguments = ImmutableArray<TypeSymbol>.Empty;
        }

        public TypeSymbolBuilder(GenericTypeSymbol basedOn) : base(basedOn)
        {
            GenericTypeArguments = ImmutableArray<TypeSymbol>.Empty;
        }

        public new ImmutableArray<TypeSymbol> GenericTypeArguments
        {
            get => base.GenericTypeArguments.CastArray<TypeSymbol>();
            set => base.GenericTypeArguments = ImmutableArray<BaseTypeSymbol>.CastUp(value);
        }

        public override TypeSymbol Build() => new(this);
    }

    public class GenericTypeSymbol : BaseTypeSymbol, IMemberSymbol
    {
        private readonly TypeBaseSymbol? _baseType;

        internal GenericTypeSymbol(string name) : this(new GenericTypeSymbolBuilder { BaseSymbolName = name })
        { }

        internal GenericTypeSymbol(GenericTypeSymbolBuilder builder) : base(builder.BaseSymbolName ?? throw new ArgumentException("Type must have a name", nameof(builder)))
        {
            if (builder.BaseType is not null &&
                builder.BaseType.GenericTypeArguments.Any(arg => arg is GenericTypeArgumentSymbol genericArg && !builder.GenericArgumentsDescriptions.Contains(genericArg)))
            {
                throw new Exception($"Base type cannot have generic arguments not that differ from implementors arguments.");
            }

            Namespace = builder.Namespace;
            _baseType = builder.BaseType;
            GenericArgumentsDescriptions = builder.GenericArgumentsDescriptions;
        }

        public NamespaceSymbol? Namespace { get; }

        public override SymbolKind Kind => SymbolKind.Type;
        public TypeBaseSymbol BaseType => _baseType ?? TypeSymbol.Object;
        public ImmutableArray<GenericTypeArgumentSymbol> GenericArgumentsDescriptions { get; }

        internal TypeSymbol MakeGenericType(params TypeSymbol[] typeArguments) => MakeGenericType(typeArguments.ToImmutableArray());

        internal TypeSymbol MakeGenericType(ImmutableArray<TypeSymbol> typeArguments) => new TypeSymbolBuilder(this)
        {
            GenericTypeArguments = typeArguments,
        }.Build();

        internal TypeBaseSymbol CreateBaseType(ImmutableArray<BaseTypeSymbol> genericArguments) => new TypeBaseSymbolBuilder(this)
        {
            GenericTypeArguments = genericArguments,
        }.Build();

        internal virtual GenericTypeSymbol FindCommonAncestor(GenericTypeSymbol other)
        {
            if (this == other || this == TypeSymbol.Object)
                return this;
            else if (other == TypeSymbol.Object)
                return other;

            var stack = new Stack<(GenericTypeSymbol, GenericTypeSymbol)>();
            stack.Push((this, other));
            while (stack.Count > 0)
            {
                var (c1, c2) = stack.Pop();
                if (c1 == c2)
                    return c1;

                if (c1.BaseType != TypeSymbol.Object && c2.BaseType != TypeSymbol.Object)
                {
                    stack.Push((c1.BaseType, c2.BaseType));
                    stack.Push((c1, c2.BaseType));
                    stack.Push((c1.BaseType, c2));
                }
            }

            return TypeSymbol.Object;
        }

        public override string Name
        {
            get
            {
                if (GenericArgumentsDescriptions.Length == 0)
                    return BaseSymbolName;
                else
                    return BaseSymbolName + "<" + string.Join(", ", GenericArgumentsDescriptions) + ">";
            }
        }

        public virtual string FullName => Namespace is null ? Name : Namespace.Name + "." + Name;

        public override string ToString() => FullName;

        public string BaseSymbolName => base.Name;

        #region Operators

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj))
                return true;

            if (!base.Equals(obj))
                return false;

            if (obj is not GenericTypeSymbol other)
                return false;

            return Namespace == other.Namespace &&
                BaseType == other.BaseType &&
                GenericArgumentsDescriptions.SequenceEqual(other.GenericArgumentsDescriptions);
        }

        public override int GetHashCode()
        {
            if (BaseType == TypeSymbol.Object)
                return -1;

            return HashCode.Combine(base.GetHashCode(), Namespace, _baseType);
        }

        #endregion Operators
    }

    /// <summary>
    /// Represents the base type of other classes.
    /// </summary>
    public class TypeBaseSymbol : GenericTypeSymbol
    {
        internal TypeBaseSymbol(string name) : base(name)
        { }

        internal TypeBaseSymbol(TypeBaseSymbolBuilder builder) : base(builder)
        {
            if (builder.GenericTypeArguments.IsDefaultOrEmpty)
                GenericTypeArguments = ImmutableArray<BaseTypeSymbol>.CastUp(builder.GenericArgumentsDescriptions);
            else if (GenericArgumentsDescriptions.Length != builder.GenericTypeArguments.Length)
                throw new Exception($"The type '{Name}' was given {builder.GenericTypeArguments.Length} type arguments, but has {GenericArgumentsDescriptions.Length} generic arguments. Count must be equal.");
            else
                GenericTypeArguments = builder.GenericTypeArguments;
        }

        public ImmutableArray<BaseTypeSymbol> GenericTypeArguments { get; }

        public override string Name
        {
            get
            {
                if (GenericTypeArguments.Length == 0)
                    return BaseSymbolName;
                else
                    return BaseSymbolName + "<" + string.Join(", ", GenericTypeArguments) + ">";
            }
        }

        internal override TypeBaseSymbol FindCommonAncestor(GenericTypeSymbol other) => (TypeBaseSymbol)base.FindCommonAncestor(other);

        #region Operators

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj))
                return true;

            if (!base.Equals(obj))
                return false;

            return obj is TypeBaseSymbol other &&
                GenericTypeArguments.SequenceEqual(other.GenericTypeArguments);
        }

        public override int GetHashCode() => base.GetHashCode();

        #endregion Operators
    }

    public sealed class TypeSymbol : TypeBaseSymbol
    {
        /// <summary>
        /// The base type of all types.
        /// </summary>
        public static readonly TypeSymbol Object = new TypeSymbol("object");

        internal TypeSymbol(string name) : this(new TypeSymbolBuilder { BaseSymbolName = name })
        {
        }

        internal TypeSymbol(TypeSymbolBuilder builder) : base(builder)
        {
            if (builder.BaseType is not null and not TypeSymbol)
                throw new ArgumentException($"Concrete type cannot have generic base type '{builder.BaseType}'.", nameof(builder));

            if (GenericArgumentsDescriptions.Length != builder.GenericTypeArguments.Length)
                throw new Exception($"The type '{Name}' was given {builder.GenericTypeArguments.Length} type arguments, but has {GenericArgumentsDescriptions.Length} generic arguments. Count must be equal.");

            GenericTypeArguments = builder.GenericTypeArguments;
        }

        public new TypeSymbol BaseType => (TypeSymbol)base.BaseType;
        public new ImmutableArray<TypeSymbol> GenericTypeArguments { get; }

        internal GenericTypeSymbol GetGenericTypeDefinition()
        {
            if (GenericTypeArguments.Length == 0)
                return this;
            else
                return new GenericTypeSymbolBuilder(this).Build();
        }

        internal override TypeSymbol FindCommonAncestor(GenericTypeSymbol other) => (TypeSymbol)base.FindCommonAncestor(other);

        #region Operators

        public override bool Equals(object? obj) => base.Equals(obj);

        public override int GetHashCode() => base.GetHashCode();

        #endregion Operators
    }

    public sealed class GenericTypeArgumentSymbol : BaseTypeSymbol
    {
        internal GenericTypeArgumentSymbol(string name) : base(name)
        {
        }

        public override SymbolKind Kind => SymbolKind.Type;
    }
}