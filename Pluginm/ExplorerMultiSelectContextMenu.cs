using Frosty.Controls;
using Frosty.Core;
using Frosty.Core.Controls;
using Frosty.Core.Interfaces;
using Frosty.Core.Windows;
using FrostySdk.Managers;
using Pluginm.Ports.Classes;
using Pluginm.Ports.Classes.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Pluginm
{
  public class ExplorerMultiSelectContextMenu : DataExplorerContextMenuExtension
  {
    /// <summary>
    /// The <see cref="Type"/> of this class.
    /// </summary>
    public Type classType = typeof(ExplorerMultiSelectContextMenu);

    /// <summary>
    /// The override <see cref="RelayCommand"/> to be assigned to the Export <see cref="MenuItem"/>.
    /// </summary>
    public RelayCommand ExportCommand
    {
      get;
      private set;
    } = new RelayCommand(delegate (object execute)
    {
      // Check if there's at least one asset selected via the same method
      if (App.EditorWindow.DataExplorer.SelectedAsset != null)
      {
        // Create an AssetDefinition variable for storing either a plugin Asset AssetDefinition or the base AssetDefinition
        AssetDefinition exportDefinition = App.PluginManager.GetAssetDefinition("Asset") ?? new AssetDefinition();

        // Create an EbxAssetEntry for storing the selected asset of the data explorer; if there's more than one asset selected, store the selected asset of this method
        EbxAssetEntry selectedEntry = (EbxAssetEntry)App.EditorWindow.DataExplorer.SelectedAsset;

        AssetExportSettings exportSettings = new AssetExportSettings();
        List<AssetExportType> exportTypes = new List<AssetExportType>();

        List<AssetEntry> selectedEntries = App.EditorWindow.DataExplorer.SelectedAssets.ToList();

        PageInputWindow assetSettingsWindow = new PageInputWindow("Asset Export Settings", new object[]
        {
          exportSettings
        });

        exportDefinition.GetSupportedExportTypes(exportTypes);

        foreach (AssetExportType exportType in exportTypes)
        {
          // Split the string via the separator and use the first string element for the export options
          exportSettings.MassExportType.AddNameValuePair(exportType.FilterString.Split('|')[0]);
        }

        // Check if the PageInputWindow wasn't given input
        if (!assetSettingsWindow.ShowDialog().GetValueOrDefault())
          return;

        // Check if there's more than one asset selected
        if (selectedEntries.Count > 1)
        {
          System.Windows.Forms.FolderBrowserDialog folderDialog = new System.Windows.Forms.FolderBrowserDialog
          {
            ShowNewFolderButton = true
          };

          int exportedAssets = 0;

          // Display the folder dialog and check if the result is a success
          if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
          {
            for (int i = 0; i < selectedEntries.Count; i++)
            {
              selectedEntry = (EbxAssetEntry)selectedEntries[i];

              // Export the asset via the associated AssetDefinition and check if it was successful
              if (exportDefinition.BetterExport(selectedEntry, folderDialog.SelectedPath, exportTypes[exportSettings.MassExportType.SelectedIndex].Extension))
              {
                exportedAssets++;

                // Check if asset dependencies should be included
                if (exportSettings.IncludeDependencies)
                {
                  // Export the asset's dependencies via the class extension
                  App.AssetManager.ExportAssetDependencies(App.AssetManager.GetEbx(selectedEntry), folderDialog.SelectedPath, exportSettings.CreateDirectories, exportSettings.DoRecursively, exportDefinition, exportTypes[exportSettings.MassExportType.SelectedIndex].Extension);
                }
              }
            }

            // Write the completion message to the logger
            App.Logger.Log("Successfully exported {0} assets{1} to \"{2}\"", new object[]
            {
              exportedAssets,
              exportSettings.IncludeDependencies ? " and their dependencies" : null,
              folderDialog.SelectedPath
            });
          }
        }
        else
        {
          // Create an AssetDefinition for storing a definition made specifically for the selected entry's type
          AssetDefinition entryAssetDefinition = App.PluginManager.GetAssetDefinition(selectedEntry.Type) ?? new AssetDefinition();

          List<AssetExportType> entryExportTypes = new List<AssetExportType>();
          entryAssetDefinition.GetSupportedExportTypes(entryExportTypes);

          string filterText = "";

          foreach (AssetExportType entryExportType in entryExportTypes)
          {
            // Append the export type to the associated string
            filterText += (!string.IsNullOrEmpty(filterText) ? "|" : null) + entryExportType.FilterString;
          }

          FrostySaveFileDialog frostySaveDialog = new FrostySaveFileDialog("Export Asset", filterText, entryAssetDefinition.GetType().Name, selectedEntry.Filename);

          // Check if the dialog was successful
          if (frostySaveDialog.ShowDialog())
          {
            // Check if the asset was exported properly
            if (entryAssetDefinition.BetterExport(selectedEntry, frostySaveDialog.FileName, entryExportTypes[frostySaveDialog.FilterIndex - 1].Extension))
            {
              // Check if the asset was successfully written and if dependencies should be included
              if (exportSettings.IncludeDependencies)
              {
                App.AssetManager.ExportAssetDependencies(App.AssetManager.GetEbx(selectedEntry), Path.GetDirectoryName(frostySaveDialog.FileName), exportSettings.CreateDirectories, exportSettings.DoRecursively, exportDefinition, exportTypes[exportSettings.MassExportType.SelectedIndex].Extension);
              }

              App.Logger.Log("Successfully exported \"{0}\"{1} to \"{2}\"", selectedEntry.Name, exportSettings.IncludeDependencies ? " with its dependencies" : null, exportSettings.IncludeDependencies ? Path.GetDirectoryName(frostySaveDialog.FileName) : frostySaveDialog.FileName);
            }
            else
            {
              App.Logger.Log("Failed to export \"{0}\" to \"{1}\"", selectedEntry.Name, frostySaveDialog.FileName);
            }
          }
        }
      }
    });

    /// <summary>
    /// The override <see cref="RelayCommand"/> to be assigned to the Open <see cref="MenuItem"/>.
    /// </summary>
    public RelayCommand OpenCommand
    {
      get;
      private set;
    } = new RelayCommand(delegate (object execute)
    {
      // Check if the current explorer's selected asset isn't null as an alternative to checking if there's any assets selected
      // This is needed due to SelectedAssets not clearing with 0 assets selected (at least the last time I tested it)
      if (App.EditorWindow.VisibleExplorer.SelectedAsset != null)
      {
        // Check if there's more than one asset selected
        if (App.EditorWindow.VisibleExplorer.SelectedAssets.Count > 1)
        {
          List<AssetEntry> selectedAssets = App.EditorWindow.VisibleExplorer.SelectedAssets.ToList();

          IEditorWindow passthroughEditorWindow = App.EditorWindow;

          // Create a task indicator for the multi-select asset opening
          FrostyTaskWindow.Show("Opening Assets", "Gathering assets", delegate (FrostyTaskWindow taskRef)
          {
            // Execute the asset opening on the calling thread
            // This should be treated as a temporary solution; more research into how to handle task threading is essential
            ((Window)passthroughEditorWindow).Dispatcher.Invoke(delegate ()
            {
              // Open the provided assets
              passthroughEditorWindow.OpenAssets(selectedAssets, App.EditorWindow.VisibleExplorer == App.EditorWindow.DataExplorer, taskRef);
            });
          });
        }
        else
        {
          // Use the base OpenAsset method to open the asset
          App.EditorWindow.OpenAsset(App.EditorWindow.VisibleExplorer.SelectedAsset, App.EditorWindow.VisibleExplorer == App.EditorWindow.DataExplorer);
        }
      }
    });

    /// <summary>
    /// The override <see cref="RelayCommand"/> to be assigned to the Revert <see cref="MenuItem"/>.
    /// </summary>
    public RelayCommand RevertCommand
    {
      get;
      private set;
    } = new RelayCommand(delegate (object execute)
    {
      bool assetReverted = false;

      // Check if there's at least one asset selected (same explanation as before)
      if (App.EditorWindow.VisibleExplorer.SelectedAsset != null)
      {
        AssetEntry assetSelection = App.EditorWindow.VisibleExplorer.SelectedAsset;

        // Check if there's more than one asset selected
        if (App.EditorWindow.VisibleExplorer.SelectedAssets.Count > 1)
        {
          if (FrostyMessageBox.Show(string.Format("Are you sure you want to revert all changes made to the {0} selected assets?", App.EditorWindow.DataExplorer.SelectedAssets.Count), "Frosty Editor", MessageBoxButton.YesNo) == MessageBoxResult.No)
            return;

          List<AssetEntry> selectedAssets = App.EditorWindow.VisibleExplorer.SelectedAssets.ToList();

          // Remove the selected assets from the tab control if they're opened
          App.EditorWindow.RemoveTabs(App.EditorWindow.VisibleExplorer.SelectedAssets.ToList(), true);

          FrostyTaskWindow.Show("Reverting/Removing Assets", "Gathering modified assets", delegate (FrostyTaskWindow taskRef)
          {
            for (int i = 0; i < selectedAssets.Count; i++)
            {
              assetSelection = selectedAssets[i];

              if (assetSelection.IsModified)
              {
                taskRef.Update(string.Format("{0} {1}", new object[]
                {
                  assetSelection.IsAdded ? "Removing" : "Reverting",
                  assetSelection.Filename
                }), i / selectedAssets.Count * 100);

                App.AssetManager.RevertAsset(assetSelection, false, false);

                assetReverted = true;
              }
            }
          });
        }
        else
        {
          // Ensure the asset is modified before proceeding
          // This check is simply a precaution for if the revert MenuItem disable handler doesn't work
          if (assetSelection.IsModified)
          {
            if (FrostyMessageBox.Show(string.Format("Are you sure you want to revert all changes made to \"{0}?\"", assetSelection.Filename), "Frosty Editor", MessageBoxButton.YesNo) == MessageBoxResult.No)
              return;

            // Remove the associated TabItem of the asset if applicable
            App.EditorWindow.RemoveTabs(new List<AssetEntry>
            {
              assetSelection
            });

            FrostyTaskWindow.Show(string.Format("{0} Asset", !assetSelection.IsAdded ? "Reverting" : "Removing"), assetSelection.Filename, delegate
            {
              App.AssetManager.RevertAsset(assetSelection, false, false);
            });

            assetReverted = true;
          }
        }
      }

      // Check if any assets were reverted
      if (assetReverted)
      {
        App.EditorWindow.DataExplorer.RefreshAll();

        // Refresh the legacy explorer
        // CurrentExplorer being avoided is only due to me not having a complete understanding of the legacy explorer and if it may use assets from the data explorer
        App.EditorWindow.LegacyExplorer.RefreshAll();
      }
    });

    /// <summary>
    /// The name to be used for this <see cref="MenuExtension"/>.
    /// </summary>
    public override string ContextItemName
    {
      get
      {
        // Rather than using a startup action, we must use a custom get accessor of ContextItemName, as this would allow for only executing this once the plugin is being processed in MainWindow
        // This would mean MainWindow's loaded, so DataExplorer and LegacyExplorer won't be null

        // If EditorWindow is not null, the plugin is either being loaded manually or being loaded on 1.0.7.0 and above
        if (App.EditorWindow != null)
        {
          App.EditorWindow.DataExplorer.MultiSelect = true;
          App.EditorWindow.DataExplorer.AssetContextMenu.Opened += AssetContextMenu_Opened;
          App.EditorWindow.DataExplorer.OnApplyTemplate();

          return classType.Name;
        }

        // Register a Closed event to SplashWindow, which would indicate that the main window is now loaded once triggered
        Application.Current.MainWindow.Closed += delegate (object sender, EventArgs e)
        {
          App.EditorWindow.DataExplorer.MultiSelect = true;

          // Register a new event to the asset contextmenu's Opened event
          // This event will be utilized for adding our custom base contextmenu options that support multi-select and removing the dummy contextmenu item
          App.EditorWindow.DataExplorer.AssetContextMenu.Opened += AssetContextMenu_Opened;

          // Execute the data explorer's OnApplyTemplate method again, as it is the only viable applicator of the multi-select change
          App.EditorWindow.DataExplorer.OnApplyTemplate();
        };

        return classType.Name;
      }
    }

    /// <summary>
    /// Generic <see cref="ContextMenu"/> Opened event.
    /// </summary>
    /// <param name="sender">Generic parameter.</param>
    /// <param name="e">Generic parameter.</param>
    private void AssetContextMenu_Opened(object sender, RoutedEventArgs e)
    {
      ContextMenu dataExplorerMenu = App.EditorWindow.DataExplorer.AssetContextMenu;

      // Check if the dummy header isn't null
      if (classType != null)
      {
        MenuItem currentMenuItem;

        for (int i = 0; i < dataExplorerMenu.Items.Count; i++)
        {
          currentMenuItem = (MenuItem)dataExplorerMenu.Items[i];

          switch (currentMenuItem.Header)
          {
            case "Export":
            case "Open":
            case "Revert":
              // Replace the MenuItem at the iteration's current index with a completely new instance, effectively removing all registered click events and commands
              dataExplorerMenu.Items[i] = new MenuItem
              {
                Command = (RelayCommand)classType.GetProperty(string.Format("{0}Command", (string)currentMenuItem.Header)).GetValue(this),
                Header = currentMenuItem.Header,
                Icon = currentMenuItem.Icon
              };

              break;

            default:
              // Check if the current MenuItem's header matches the type's name, meaning it's the temporary MenuItem
              if ((string)currentMenuItem.Header == classType.Name)
              {
                // Remove the item from the data explorer's asset context menu if it matches
                dataExplorerMenu.Items.Remove(currentMenuItem);

                break;
              }

              break;
          }
        }

        // Assign null to the class type, preventing the foreach loop from running again unnecessarily
        classType = null;
      }

      // Assign to the import asset MenuItem's IsEnabled property with the IsEnabled value of the duplication MenuItem for efficiency
      ((MenuItem)dataExplorerMenu.Items[3]).IsEnabled = App.SelectedAsset != null && App.EditorWindow.DataExplorer.SelectedAssets.Count == 1;

      // Assign to the revert button's enabled status based on the selected asset being null and either added or modified
      ((MenuItem)dataExplorerMenu.Items[1]).IsEnabled = App.SelectedAsset != null && (App.SelectedAsset.IsModified || App.SelectedAsset.IsAdded || App.EditorWindow.DataExplorer.SelectedAssets.Count > 1);

      // Assign to the revert button's text content based on the following condition, using Revert/Remove or the terms separately depending on the result 
      ((MenuItem)dataExplorerMenu.Items[1]).Header = App.SelectedAsset != null && App.EditorWindow.DataExplorer.SelectedAssets.Count > 1 ? "Revert/Remove" : (!App.EditorWindow.DataExplorer.SelectedAsset.IsAdded ? "Revert" : "Remove");

      // Included the same polish for the Legacy Explorer; can be found below for when support is implemented

      //legacyExplorer.AssetContextMenu.Opened += delegate (object sender, RoutedEventArgs e)
      //{
      //  // Assign to the legacy import MenuItem's enabled status
      //  legacyImportMenuItem.IsEnabled = legacyExplorer.SelectedAsset != null && legacyExplorer.SelectedAssets.Count == 1;

      //  // Assign to the legacy revert MenuItem's enabled status
      //  legacyRevertMenuItem.IsEnabled = legacyExplorer.SelectedAsset != null && (legacyExplorer.SelectedAsset.IsModified || legacyExplorer.SelectedAsset.IsAdded || legacyExplorer.SelectedAssets.Count > 1);
      //};
    }
  }
}
