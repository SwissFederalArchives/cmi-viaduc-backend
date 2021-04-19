using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq.Expressions;
using System.Reflection;

namespace CMI.Utilities.Common
{
    public abstract class AbstractDocumentation
    {
        public Dictionary<PropertyInfo, string> Documentations { get; } = new Dictionary<PropertyInfo, string>();

        /// <summary>
        ///     Füge hier die Dokumentation hinzu, z.B. mit:
        ///     AddDescription CacheSettings>(x => x.Port, "Port, auf welchem der Cache Service lauscht.");
        /// </summary>
        public abstract void LoadDescriptions();

        protected void AddDescription<T>(Expression<Func<T, object>> expression, string description) where T : ApplicationSettingsBase
        {
            var memberExpression = expression.Body as MemberExpression ?? ((UnaryExpression) expression.Body).Operand as MemberExpression;
            var propertyInfo = typeof(T).GetProperty(memberExpression?.Member.Name ?? throw new InvalidOperationException()) ??
                               throw new InvalidOperationException();
            Documentations[propertyInfo] = description;
        }
    }
}