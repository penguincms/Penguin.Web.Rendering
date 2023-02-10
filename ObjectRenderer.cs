﻿using Microsoft.AspNetCore.Hosting;
using Penguin.Entities.Abstractions;
using Penguin.Reflection.Extensions;
using Penguin.Templating.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Penguin.Web.Rendering
{
    /// <summary>
    /// A base class for rendering out an instance of an entity into a compilable MVC view using a template
    /// To facilitate object binding and HTML generation for dynamic pages and email templates without needing to use
    /// reflection based binding
    /// </summary>
    public class ObjectRenderer
    {
        /// <summary>
        /// Small class used to hold parameter information in a way that is not
        /// reliant on the source (Method/Template)
        /// </summary>
        protected class ParameterMeta
        {
            /// <summary>
            /// The name of the parameter
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// The intended type of the parameter
            /// </summary>
            public Type ParameterType { get; set; }

            /// <summary>
            /// Constructs a new instance of this class using the supplied information
            /// </summary>
            /// <param name="name">The name of the parameter</param>
            /// <param name="parameterType"> The intended type of the parameter</param>
            public ParameterMeta(string name, Type parameterType)
            {
                ParameterType = parameterType;
                Name = name;
            }
        }

        internal IHostingEnvironment HostingEnvironment { get; set; }
        internal const string AUTOGENERATED_TAG = "@*Everything above this line is autogenerated. If you change it, you will lose your changes.*@";
        internal const string TUPLE_NOTE = "@*System.ValueTuple requires at least two types so all templates have a value model to match that requirement. You can ignore placeholder elements*@";
        internal static object TemplateLock = new();
        private const string NULL_TUPLE_MESSAGE = "Tuple type is null. This will probably be the case if we're not getting it from mscorelib anymore so code to account for that";

        /// <summary>
        /// Constructs a new instance of the Object Renderer
        /// </summary>
        /// <param name="hostingEnvironment">The IHosting environment used to determine the path the Templates should be rendered to (Views/Cache)</param>
        public ObjectRenderer(IHostingEnvironment hostingEnvironment)
        {
            HostingEnvironment = hostingEnvironment;
        }

        /// <summary>
        /// Takes the supplied parameter values and constructs a Tuple that represents the parameters to use as the model for generating the page
        /// </summary>
        /// <param name="toGenerate">Contains the information used to define and generate the object model that will be passed into the view when its rendered</param>
        /// <returns>A Tuple representing the Model as defined for the new dynamically generated view</returns>
        protected static object BuildPageModel(IEnumerable<TemplateParameter> toGenerate)
        {
            Contract.Requires(toGenerate != null);
            List<TemplateParameter> modelParameters = PadPageModel(toGenerate);

            return CreateTuple(modelParameters);
        }

        /// <summary>
        /// Constructs a Tuple with the intent of passing into a dynamically generated view for rendering
        /// </summary>
        /// <param name="toGenerate">Contains the information used to define and generate the object model that will be passed into the view when its rendered</param>
        /// <returns>A tuple representing an instance of a model to pass into a dynamically generated view</returns>
        protected static object CreateTuple(IEnumerable<TemplateParameter> toGenerate)
        {
            List<TemplateParameter> modelValues = PadPageModel(toGenerate);

            Contract.Requires(toGenerate != null);

            Type tupleType = Type.GetType("System.ValueTuple`" + toGenerate.Count())?.MakeGenericType(toGenerate.Select(p => p.Type).ToArray());

            if (tupleType is null)
            {
                throw new Exception(NULL_TUPLE_MESSAGE);
            }

            object[] values = new object[modelValues.Count];

            for (int i = 0; i < values.Length; i++)
            {
                values[i] = modelValues[i].Type.IsValueType && modelValues[i].Value is null
                    ? Activator.CreateInstance(modelValues[i].Type)
                    : modelValues[i].Value;
            }

            return Activator.CreateInstance(tupleType, values);
        }

        /// <summary>
        /// Takes the supplied object and generates a view path based on its Auditable entity properties
        /// </summary>
        /// <param name="e">The Auditable entity to generate the view based off of.Uses the ID/Guid and the DateModified to determine if the template needs to be updated</param>
        /// <param name="toGenerate">Contains the information used to define and generate the object model that will be passed into the view when its rendered</param>
        /// <param name="TemplateContents">The text string to inject into the template view, the Body of the view beyond what this system generates for injectable model information</param>
        /// <param name="FieldName">The name of the field of the entity that this view is intended to bind against used during path generation </param>
        /// <returns></returns>
        protected GeneratedTemplateInfo GenerateTemplatePath(IAuditableEntity e, IEnumerable<TemplateParameter> toGenerate, string TemplateContents, string FieldName = "")
        {
            Contract.Requires(e != null);

            object model = BuildPageModel(toGenerate);

            string RelativePath = Path.Combine("Client", "Views", "Cache", e.GetType().Name, e._Id.ToString(CultureInfo.CurrentCulture), FieldName, (e.DateModified.Value - new DateTime(1970, 1, 1)).TotalMilliseconds.ToString(CultureInfo.CurrentCulture) + ".cshtml");

            string AbsolutePath = Path.Combine(HostingEnvironment.ContentRootPath, RelativePath);

            DirectoryInfo CachePath = new FileInfo(AbsolutePath).Directory;

            if (!CachePath.Exists)
            {
                CachePath.Create();
            }

            lock (TemplateLock)
            {
                if (!File.Exists(AbsolutePath))
                {
                    string contents = TemplateContents?.ToString(CultureInfo.CurrentCulture) ?? string.Empty;

                    if (!contents.Contains(AUTOGENERATED_TAG))
                    {
                        List<string> Header = new()
                        {
                            TUPLE_NOTE,
                            $"@model ({string.Join(", ", PadPageModel(toGenerate).Select(p => $"{p.Type.GetDeclaration()} {p.Name}"))})",
                            AUTOGENERATED_TAG,
                            $"@*{e.Guid}*@"
                        };

                        contents = string.Join(Environment.NewLine, Header) + Environment.NewLine + Environment.NewLine + Environment.NewLine + contents;
                    }

                    File.WriteAllText(AbsolutePath, contents);
                }
            }

            return new GeneratedTemplateInfo(AbsolutePath, RelativePath, model);
        }

        private static List<TemplateParameter> PadPageModel(IEnumerable<TemplateParameter> modelParameters)
        {
            List<TemplateParameter> PaddedPageModel = modelParameters.ToList();

            int i = 0;
            while (PaddedPageModel.Count < 2)
            {
                string name = "PlaceHolder" + $"{++i}";

                PaddedPageModel.Add(new TemplateParameter(typeof(object), name, new object()));
            }

            return PaddedPageModel;
        }
    }
}