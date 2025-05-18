using Frosty.Core.Controls.Editors;
using Frosty.Core.Misc;
using FrostySdk.Attributes;
using System.Collections.Generic;

namespace Pluginm.Ports.Classes
{
  [Description("The configuration to be used for exporting the selected asset(s).")]
  [DisplayName("Settings")]
  public class AssetExportSettings
  {
    /// <summary>
    /// A bool determining whether or not dependencies will be included in the asset export process.
    /// </summary>
    [Category("Settings")]
    [Description("Determines whether or not dependencies of the selected asset should also be exported. This will alter the export process to still use a file dialog, but export the dependencies alongside that exported asset if a single asset is selected.")]
    [DisplayName("Include Dependencies")]
    public bool IncludeDependencies
    {
      get;
      set;
    }

    /// <summary>
    /// A bool determining whether or not separate directories should be created for storing dependencies.
    /// </summary>
    [Category("Settings")]
    [DependsOn("IncludeDependencies")]
    [Description("Determines whether or not separate directories should be created for storing dependencies. Using this alongside recursive export may eventually trigger the Windows path length limit.")]
    [DisplayName("Include Dependencies: Create Directories")]
    public bool CreateDirectories
    {
      get;
      set;
    }

    /// <summary>
    /// A bool determining whether or not dependencies should be recursively exported.
    /// </summary>
    [Category("Settings")]
    [DependsOn("IncludeDependencies")]
    [Description("Determines whether or not dependencies should be recursively exported.")]
    [DisplayName("Include Dependencies: Recursive Export")]
    public bool DoRecursively
    {
      get;
      set;
    }

    /// <summary>
    /// The export type to be used for exporting dependencies if the associated property is enabled and multi-select asset exporting. This is retrieved via a <see cref="CustomComboData{T, U}"/> instance.
    /// </summary>
    [Category("Settings")]
    [Description("The export type to be used for exporting dependencies if the associated property is enabled and multi-select asset exporting.")]
    [DisplayName("Mass Export Type")]
    [Editor(typeof(FrostyCustomComboDataEditor<string, string>))]
    public CustomComboData<string, string> MassExportType
    {
      get;
      set;
    } = new CustomComboData<string, string>(new List<string>(), new List<string>());

    /// <summary>
    /// Initializes a new instance of the <see cref="AssetExportSettings"/> class.
    /// </summary>
    public AssetExportSettings()
    {
    }
  }
}
