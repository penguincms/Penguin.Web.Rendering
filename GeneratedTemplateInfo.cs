using System;
using System.Collections.Generic;
using System.Text;

namespace Penguin.Web.Rendering
{
    /// <summary>
    /// Represents the result of creating a view to be used with rendering an object model to a string
    /// </summary>
    public class GeneratedTemplateInfo
    {
        internal GeneratedTemplateInfo(string absolutePath, string relativePath, object model)
        {
            AbsolutePath = absolutePath;
            RelativePath = relativePath;
            Model = model;
        }
        /// <summary>
        /// The absolute path to the generated template
        /// </summary>
        public string AbsolutePath { get; set; }

        /// <summary>
        /// The relative path to the generated template
        /// </summary>
        public string RelativePath { get; set; }

        /// <summary>
        /// The constructed object model to pass into the template
        /// </summary>
        public object Model { get; set; }
    }
}
