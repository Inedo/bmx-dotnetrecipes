﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34014
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
        ///   Looks up a localized string similar to &lt;?xml version=&quot;1.0&quot; encoding=&quot;utf-8&quot;?&gt;
        ///&lt;DeploymentPlan xmlns=&quot;http://inedo.com/schemas/buildmaster/2014/05/bmxplan&quot;&gt;
        ///  &lt;Servers&gt;
        ///    &lt;Server Id=&quot;1&quot; LastModified=&quot;2000-01-01T00:00:00Z&quot;&gt;BUILDMASTER&lt;/Server&gt;
        ///  &lt;/Servers&gt;
        ///  &lt;Providers /&gt;
        ///  &lt;ActionGroup DeploymentPlanActionGroupId=&quot;1&quot; DeploymentPlanActionGroupSequence=&quot;1&quot; ActionGroupId=&quot;1&quot; Active=&quot;Y&quot; Shared=&quot;N&quot; Parallel=&quot;N&quot; IterateServers=&quot;N&quot; IterateDeployables=&quot;N&quot; OnFailureContinue=&quot;N&quot; DeployableName=&quot;Web&quot;&gt;
        ///    &lt;Name&gt;Source&lt;/Name&gt;
        ///    &lt;Description&gt;Appli [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string BitChecker_Build_Plan {
            get {
                return ResourceManager.GetString("BitChecker_Build_Plan", resourceCulture);
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
        ///   Looks up a localized string similar to &lt;?xml version=&quot;1.0&quot; encoding=&quot;utf-8&quot;?&gt;
        ///&lt;DeploymentPlan xmlns=&quot;http://inedo.com/schemas/buildmaster/2014/05/bmxplan&quot;&gt;
        ///  &lt;Servers&gt;
        ///    &lt;Server Id=&quot;1&quot; LastModified=&quot;2000-01-01T00:00:00Z&quot;&gt;BUILDMASTER&lt;/Server&gt;
        ///  &lt;/Servers&gt;
        ///  &lt;Providers /&gt;
        ///  &lt;ActionGroup DeploymentPlanActionGroupId=&quot;8&quot; DeploymentPlanActionGroupSequence=&quot;1&quot; ActionGroupId=&quot;8&quot; Active=&quot;Y&quot; Shared=&quot;N&quot; Parallel=&quot;N&quot; IterateServers=&quot;N&quot; IterateDeployables=&quot;N&quot; OnFailureContinue=&quot;N&quot; DeployableName=&quot;Web&quot;&gt;
        ///    &lt;Name&gt;Deploy Web&lt;/Name&gt;
        ///    &lt;Description&gt;D [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string BitChecker_Environment_Plan {
            get {
                return ResourceManager.GetString("BitChecker_Environment_Plan", resourceCulture);
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
        ///   Looks up a localized string similar to Runs a sample script that pings inedo.com..
        /// </summary>
        internal static string BitChecker_SayHello {
            get {
                return ResourceManager.GetString("BitChecker_SayHello", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized resource of type System.Byte[].
        /// </summary>
        internal static byte[] BitChecker_SayHelloScript {
            get {
                object obj = ResourceManager.GetObject("BitChecker_SayHelloScript", resourceCulture);
                return ((byte[])(obj));
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
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;?xml version=&quot;1.0&quot; encoding=&quot;utf-8&quot; ?&gt;
        ///&lt;appSettings&gt;
        ///  &lt;add key=&quot;ContactFormRecipient&quot; value=&quot;$ContactFormRecipient&quot; /&gt;
        ///  &lt;add key=&quot;SmtpServer&quot; value=&quot;$SmtpServer&quot; /&gt;
        ///&lt;/appSettings&gt;.
        /// </summary>
        internal static string web_appsettings {
            get {
                return ResourceManager.GetString("web_appsettings", resourceCulture);
            }
        }
    }
}
