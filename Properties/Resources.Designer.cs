﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.239
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Inedo.BuildMasterExtensions.DotNetRecipes.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Inedo.BuildMasterExtensions.DotNetRecipes.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Builds the application using the source code retrieved in the previous action group..
        /// </summary>
        internal static string BitChecker_Build {
            get {
                return ResourceManager.GetString("BitChecker_Build", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Generates a report listing all of the source-code changes since the last deployed release. This report will be accessible on the Build page..
        /// </summary>
        internal static string BitChecker_CompareSource {
            get {
                return ResourceManager.GetString("BitChecker_CompareSource", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Deploys database schema changes to the target environment..
        /// </summary>
        internal static string BitChecker_DeployDatabase {
            get {
                return ResourceManager.GetString("BitChecker_DeployDatabase", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Deploys the built application and its configuration files to the target directory..
        /// </summary>
        internal static string BitChecker_DeployWeb {
            get {
                return ResourceManager.GetString("BitChecker_DeployWeb", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Applies a label to source control, then gets source code by that label and set the version attribute in AssemblyInfo.cs to the release and build numbers..
        /// </summary>
        internal static string BitChecker_GetSource {
            get {
                return ResourceManager.GetString("BitChecker_GetSource", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Runs automated unit tests on the compiled application. The execution will fail if any unit test indicates an error..
        /// </summary>
        internal static string BitChecker_UnitTests {
            get {
                return ResourceManager.GetString("BitChecker_UnitTests", resourceCulture);
            }
        }
    }
}