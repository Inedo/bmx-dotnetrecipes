using System.Linq;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Extensibility.Gadgets;

namespace Inedo.BuildMasterExtensions.DotNetRecipes.Gadgets
{
    /// <summary>
    /// Hides the application navigation bar before the first build of release 1.1
    /// of BitChecker is created.
    /// </summary>
    [GadgetProperties(
        "Hide Navigation",
        "Hides the application navigation bar in BitChecker.",
        GadgetScope.Application)]
    internal sealed class HideNavigationGadget : GadgetBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HideNavigationGadget"/> class.
        /// </summary>
        public HideNavigationGadget()
        {
        }

        /// <summary>
        /// Returns an instance of a <see cref="T:System.Web.UI.Control"/> that can be
        /// added to a Dashboard page.
        /// </summary>
        /// <param name="context">Context information for the gadget.</param>
        /// <returns>
        /// <see cref="T:System.Web.UI.Control"/> that can be added to a Dashboard page.
        /// </returns>
        public override Control CreateFullSizeGadgetControl(GadgetContext context)
        {
            bool hasBuild = false;
            try
            {
                StoredProcs.Builds_GetBuild(context.ApplicationId, "1.1", "1").Execute().Any();
                hasBuild = true;
            }
            catch { }

            var ctlScript = new HtmlGenericControl("script")
            {
                Visible = !hasBuild,
                InnerHtml = @"
$(function(){ 
$('.application-navigation-bar').hide(); 
$('.header-app-search').hide(); 
$('.live-help-container').hide(); 
$('#BodyContainer').css('border-radius','8px') 
.css('-moz-border-radius','8px') 
.css('-webkit-border-radius','8px'); 
$('.bitchecker-container').show().prependTo('.main-content'); 
$('.bitchecker-container-opener').hide(); 
$('.main-content').css('margin','0px 20px;').children().not('.bitchecker-container').hide(); 
$('.fixed-width-container').css('width', '750px'); 
});"
            };

            ctlScript.Attributes.Add("type", "text/javascript");
            return ctlScript;
        }
    }
}
