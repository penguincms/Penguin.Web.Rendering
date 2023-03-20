using System;
using Loxifi;
using System.Linq;
using System.Text;

namespace Penguin.Web.Rendering.Extensions
{
    public static class TypeExtensions
    {
        /// <summary>
        /// Returns a string that can be used to declare a type in code (ex System.Collections.Generic.List&lt;System.String&gt;)
        /// </summary>
        /// <param name="type">The type to get the declaration for</param>
        /// <returns>The type declaration</returns>
        public static string GetDeclaration(this Type type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (!type.GetGenericArguments().Any())
            {
                return type.FullName;
            }
            else
            {
                StringBuilder sb = new();

                _ = sb.Append(type.FullName.To("`"));

                _ = sb.Append('<');

                _ = sb.Append(string.Join(",", type.GetGenericArguments().Select(t => t.GetDeclaration())));

                _ = sb.Append('>');

                return sb.ToString();
            }
        }
    }
}
