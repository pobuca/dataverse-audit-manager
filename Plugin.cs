using System.ComponentModel.Composition;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;

namespace DataverseAuditManager.XrmToolBox
{
    [Export(typeof(IXrmToolBoxPlugin))]
    [ExportMetadata("Name", "Dataverse Audit Manager")]
    [ExportMetadata("Description", "Inspect and manage table and column auditing in Dataverse")]
    [ExportMetadata("SmallImageBase64", LogoResources.Small)]
    [ExportMetadata("BigImageBase64", LogoResources.Big)]
    [ExportMetadata("BackgroundColor", "#1F4E78")]
    [ExportMetadata("PrimaryFontColor", "#FFFFFF")]
    [ExportMetadata("SecondaryFontColor", "#D9EAF7")]
    public sealed class Plugin : PluginBase
    {
        public override IXrmToolBoxPluginControl GetControl()
        {
            return new AuditManagerControl();
        }
    }
}
