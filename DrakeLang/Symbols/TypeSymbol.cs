﻿//------------------------------------------------------------------------------
// DrakeLang - Viv's C#-esque sandbox.
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
using System.Collections.Immutable;
using System.Linq;

namespace DrakeLang.Symbols
{
    internal sealed class TypeSymbolBuilder
    {
        public TypeSymbolBuilder()
        {
        }

        /// <summary>
        /// Gets or sets the name of the type.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the namespace of the type.
        /// </summary>
        public NamespaceSymbol? Namespace { get; set; }

        /// <summary>
        /// Gets or sets the generic type arguments of the type.
        /// </summary>
        public ImmutableArray<TypeSymbol> GenericTypeArguments { get; set; } = ImmutableArray<TypeSymbol>.Empty;

        public ImmutableArray<MethodSymbol> Methods { get; set; } = ImmutableArray<MethodSymbol>.Empty;

        public TypeSymbol Build() => new(this);
    }

    internal sealed class GenericArgumentSymbolBuilder
    {
        public GenericArgumentSymbolBuilder()
        {
        }

        public GenericArgumentSymbolBuilder(string? name)
        {
            Name = name;
        }

        /// <summary>
        /// Gets or sets the name of the type argument.
        /// </summary>
        public string? Name { get; set; }

        public TypeSymbol Build() => new(this);
    }

    public sealed class TypeSymbol : MemberSymbol
    {
        /// <summary>
        /// The base type of all types.
        /// </summary>
        public static readonly TypeSymbol Object = new TypeSymbol("object");

        private readonly TypeSymbol? _genericTypeDefinition;

        #region Constructors

        public TypeSymbol(string name) : this(new TypeSymbolBuilder { Name = name })
        {
        }

        internal TypeSymbol(TypeSymbolBuilder builder) : base(builder.Name ?? throw new ArgumentException("Type must have a name.", nameof(builder)))
        {
            BaseType = Object;
            _genericTypeDefinition = this;

            if (builder.GenericTypeArguments.IsDefaultOrEmpty)
            {
                IsGenericTypeDefinition = false;
                IsGenericType = false;
                IsConcreteType = true;
            }
            else
            {
                IsGenericTypeDefinition = true;
                IsGenericType = true;
                IsConcreteType = false;

                GenericTypeArguments = builder.GenericTypeArguments;
            }

            Namespace = builder.Namespace;

            if (!builder.Methods.IsDefaultOrEmpty)
            {
                Methods = builder.Methods;
                AssertValidMembers(Methods);
            }
        }

        internal TypeSymbol(GenericArgumentSymbolBuilder builder) : base(builder.Name ?? throw new ArgumentException("Generic type argument must have a name.", nameof(builder)))
        {
            IsGenericTypeArgument = true;
        }

        private TypeSymbol(TypeSymbol genericType, ImmutableArray<TypeSymbol> typeArguments) : base(genericType.BaseSymbolName)
        {
            _genericTypeDefinition = genericType;

            BaseType = genericType.BaseType;
            IsGenericTypeDefinition = false;
            IsGenericType = true;
            IsConcreteType = typeArguments.All(arg => arg.IsConcreteType);

            Namespace = genericType.Namespace;
            GenericTypeArguments = typeArguments;

            Methods = MakeConcrete(genericType.Methods);
        }

        #region AssertValidMember

        private void AssertValidMembers<T>(ImmutableArray<T> symbols) where T : ISymbol
        {
            foreach (T symbol in symbols)
            {
                switch (symbol)
                {
                    case MethodSymbol method:
                        AssertValidMember(method);
                        break;

                    case ParameterSymbol parameter:
                        AssertValidMember(parameter);
                        break;

                    case TypeSymbol type:
                        AssertValidMember(type);
                        break;

                    default:
                        throw new Exception($"Unhandled symbol kind '{symbol.GetType()}'.");
                }
            }
        }

        private void AssertValidMember(MethodSymbol method)
        {
            AssertValidMembers(method.Parameters);
            AssertValidMember(method.ReturnType);

            if (method.Namespace is not null)
                throw new ArgumentException($"Instance methods cannot have a namespace (offending method was '{method.Name}' with namespace '{method.Namespace}'.");
        }

        private void AssertValidMember(ParameterSymbol parameter)
        {
            AssertValidMember(parameter.Type);
        }

        private void AssertValidMember(TypeSymbol type)
        {
            if (IsConcreteType)
            {
                if (!type.IsConcreteType)
                    throw new ArgumentException($"Cannot declare concrete type containing non-concrete type '{type}' as a member parameter type or return type.");
            }
            else if (IsGenericType)
            {
                if (type.IsGenericTypeArgument && !GenericTypeArguments.Contains(type))
                    throw new ArgumentException(
                        $"Generic type arguments in members of generic types must match type's generic type's arguments. " +
                        $"Type '{type}' is not a generic argument of the constructed type."
                    );
            }
        }

        #endregion AssertValidMember

        #region MakeConcrete

        private ImmutableArray<T> MakeConcrete<T>(ImmutableArray<T> symbols) where T : ISymbol
        {
            ImmutableArray<T>.Builder? builder = null;

            for (int i = 0; i < symbols.Length; i++)
            {
                T oldSymbol = symbols[i];
                ISymbol newSymbol = oldSymbol switch
                {
                    MethodSymbol method => MakeConcrete(method),
                    ParameterSymbol parameter => MakeConcrete(parameter),
                    TypeSymbol type => MakeConcrete(type),

                    _ => throw new Exception($"Unhandled symbol kind '{oldSymbol.GetType()}'."),
                };

                if (builder is null && !ReferenceEquals(oldSymbol, newSymbol))
                {
                    builder = ImmutableArray.CreateBuilder<T>(symbols.Length);
                    for (int j = 0; j < i; j++)
                    {
                        builder.Add(symbols[i]);
                    }
                }

                if (builder is not null)
                    builder.Add((T)newSymbol);
            }

            return builder?.MoveToImmutable() ?? symbols;
        }

        private MethodSymbol MakeConcrete(MethodSymbol method)
        {
            var parameters = MakeConcrete(method.Parameters);
            var returnType = MakeConcrete(method.ReturnType);

            if (parameters == method.Parameters && returnType == method.ReturnType)
                return method;

            return new(method.Name, parameters, returnType);
        }

        private ParameterSymbol MakeConcrete(ParameterSymbol parameter)
        {
            var type = MakeConcrete(parameter.Type);

            if (type == parameter.Type)
                return parameter;

            return new(parameter.Name, type);
        }

        private TypeSymbol MakeConcrete(TypeSymbol type)
        {
            if (type.IsConcreteType)
                return type;

            if (type.IsGenericType)
            {
                var typeArguments = type.GenericTypeArguments.Select(arg => MakeConcrete(arg)).ToImmutableArray();
                return type.MakeConcreteType(typeArguments);
            }

            // Type is generic type argument.
            var index = _genericTypeDefinition!.GenericTypeArguments.IndexOf(type);
            return GenericTypeArguments[index];
        }

        #endregion MakeConcrete

        #endregion Constructors

        public override NamespaceSymbol? Namespace { get; }

        public override SymbolKind Kind => SymbolKind.Type;

        /// <summary>
        /// This type's base type.
        /// </summary>
        public TypeSymbol? BaseType { get; }

        public ImmutableArray<TypeSymbol> GenericTypeArguments { get; } = ImmutableArray<TypeSymbol>.Empty;
        public ImmutableArray<MethodSymbol> Methods { get; } = ImmutableArray<MethodSymbol>.Empty;

        public override string Name
        {
            get
            {
                if (IsGenericType)
                    return BaseSymbolName + "<" + string.Join(", ", GenericTypeArguments) + ">";
                else
                    return BaseSymbolName;
            }
        }

        public string BaseSymbolName => base.Name;

        public bool IsGenericTypeDefinition { get; }
        public bool IsGenericType { get; }
        public bool IsConcreteType { get; }
        public bool IsGenericTypeArgument { get; }

        public TypeSymbol MakeConcreteType(params TypeSymbol[] typeArguments) => MakeConcreteType(typeArguments.ToImmutableArray());

        public TypeSymbol MakeConcreteType(ImmutableArray<TypeSymbol> typeArguments)
        {
            if (!IsGenericType)
                throw new Exception($"Cannot create concrete type from non-generic type '{Name}'.");
            else if (!IsGenericTypeDefinition)
                return GetGenericDefinition().MakeConcreteType(typeArguments);

            if (GenericTypeArguments.Length != typeArguments.Length)
                throw new Exception($"The type '{Name}' was given {typeArguments.Length} type arguments, but has {GenericTypeArguments.Length} generic arguments. Count must be equal.");
            else if (!typeArguments.All(arg => arg.IsConcreteType))
                throw new Exception($"Cannot create concrete type with any non-concrete type arguments.");

            return new(this, typeArguments);
        }

        public TypeSymbol GetGenericDefinition()
        {
            if (!IsGenericType)
                throw new InvalidOperationException($"Cannot get generic defintion of non-generic type '{Name}'.");

            if (IsGenericTypeDefinition)
                return this;

            return _genericTypeDefinition!.GetGenericDefinition();
        }

        public TypeSymbol FindCommonAncestor(TypeSymbol other)
        {
            if (IsGenericTypeArgument)
                throw new InvalidOperationException($"Cannot find ancestor of generic type argument '{Name}'.");

            if (this == other)
                return this;
            else
                return Object;
        }

        #region Operators

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj))
                return true;

            if (obj is not TypeSymbol other)
                return false;

            return Name == other.Name &&
                Namespace == other.Namespace &&
                BaseType == other.BaseType &&
                GenericTypeArguments.SequenceEqual(other.GenericTypeArguments) &&
                Methods.SequenceEqual(other.Methods);
        }

        public override int GetHashCode()
        {
            if (BaseType == Object)
                return -1;

            return HashCode.Combine(Name, Namespace, BaseType, GenericTypeArguments.Length, Methods.Length);
        }

        public static bool operator ==(TypeSymbol? left, TypeSymbol? right)
        {
            if (left is null)
                return right is null;

            return left.Equals(right);
        }

        public static bool operator !=(TypeSymbol? left, TypeSymbol? right)
        {
            return !(left == right);
        }

        #endregion Operators
    }
}