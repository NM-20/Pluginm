using Frosty.Controls;
using Frosty.Core;
using Frosty.Core.Interfaces;
using Frosty.Core.Misc;
using Frosty.Core.Windows;
using FrostySdk.Ebx;
using FrostySdk.IO;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Input;

namespace Pluginm.Ports.Classes
{
  public static class TypeExtensions
  {
    /// <summary>
    /// Exports an asset's dependencies.
    /// </summary>
    /// <param name="ebx">The <see cref="EbxAsset"/> to be used.</param>
    /// <param name="outputPath">The output path to export the assets to.</param>
    /// <param name="doRecursively">Determines whether or not recursive export should be used.</param>
    /// <param name="createDirectories">Determines whether or not assets will be exported to a single directory or numerous subdirectories.</param>
    /// <param name="exportDefinition">The export <see cref="AssetDefinition"/> to be used.</param>
    /// <param name="extension">The file extension to be used for determining the export type.</param>
    public static void ExportAssetDependencies(this AssetManager assetManager, EbxAsset ebx, string outputPath, bool createDirectories = false, bool doRecursively = false, AssetDefinition exportDefinition = null, string extension = null)
    {
      // Check if there's dependencies to export
      if (ebx.Dependencies.Count() != 0)
      {
        foreach (Guid dependencyGuidEntry in ebx.Dependencies)
        {
          EbxAssetEntry dependencyAssetEntry = assetManager.GetEbxEntry(dependencyGuidEntry);
          EbxAsset dependencyEbx = assetManager.GetEbx(dependencyAssetEntry);

          Stream dependencyEbxStream = assetManager.GetEbxStream(dependencyAssetEntry);

          // Create a string for storing the export directory path; these are always appended with _Dependencies
          string dependencyOutputPath = createDirectories ? Path.Combine(new string[]
          {
            outputPath,
            string.Concat(new string[]
            {
              App.AssetManager.GetEbxEntry(ebx.FileGuid).Filename,
              "_Dependencies"
            })
          }) : outputPath;

          Directory.CreateDirectory(dependencyOutputPath);

          bool exportSuccessful = false;

          // Check if there's a valid list of export types and if there's an assigned filter
          if (exportDefinition != null && extension != null)
          {
            // Attempt to export the asset
            exportSuccessful = exportDefinition.BetterExport(dependencyAssetEntry, dependencyOutputPath, extension);
          }

          if (!exportSuccessful)
          {
            using (NativeWriter nativeWriter = new NativeWriter(new FileStream(string.Concat(new string[]
            {
              Path.Combine(new string[]
              {
                dependencyOutputPath,
                dependencyAssetEntry.Filename
              }),
              ".bin"
            }), FileMode.Create, FileAccess.Write)))
            {
              nativeWriter.Write(NativeReader.ReadInStream(dependencyEbxStream));
            }
          }

          if (doRecursively)
          {
            // Recursively export any available dependencies of the current dependency
            assetManager.ExportAssetDependencies(dependencyEbx, dependencyOutputPath, createDirectories, doRecursively, exportDefinition, extension);
          }
        }
      }
    }

    /// <summary>
    /// Registers key bindings to a <see cref="CommandBindingCollection"/> via a dictionary of <see cref="KeyGesture"/>/<see cref="ExecutedRoutedEventHandler"/> pairings.
    /// </summary>
    /// <param name="gestureHandlerPairings">The dictionary of key/handler pairings to be used.</param>
    public static void RegisterKeyBindings(this CommandBindingCollection commandBindingCollection, Dictionary<KeyGesture, ExecutedRoutedEventHandler> gestureHandlerPairings)
    {
      RoutedCommand currentCommand;

      for (int i = 0; i < gestureHandlerPairings.Count; i++)
      {
        currentCommand = new RoutedCommand();
        currentCommand.InputGestures.Add(gestureHandlerPairings.Keys.ElementAt(i));

        commandBindingCollection.Add(new CommandBinding(currentCommand, gestureHandlerPairings.Values.ElementAt(i)));
      }
    }

    /// <summary>
    /// Gets the value of a <see cref="PointerRef"/>.
    /// </summary>
    /// <param name="pointerRef">The <see cref="PointerRef"/> to be used.</param>
    /// <returns>The resolved value.</returns>
    public static object Resolve(this PointerRef pointerRef)
    {
      object pointerRefValue = null;

      if (pointerRef.Type == PointerRefType.External)
      {
        EbxImportReference importReference = pointerRef.External;

        // Get the associated asset and asset entry
        EbxAssetEntry importEntry = App.AssetManager.GetEbxEntry(importReference.FileGuid);
        EbxAsset importAsset = App.AssetManager.GetEbx(importEntry);

        // Set the pointerref's value to its import reference's referenced object
        pointerRefValue = importAsset.GetObject(importReference.ClassGuid);
      }
      else if (pointerRef.Type == PointerRefType.Internal)
      {
        pointerRefValue = pointerRef.Internal;
      }

      // If it isn't either of these, it is a null pointerref, so nothing has to be set since the pointerref's value is defaulted to null
      return pointerRefValue;
    }

    /// <summary>
    /// Improves upon <see cref="AssetDefinition.Export(EbxAssetEntry, string, string)"/> to automatically handle an export path.
    /// </summary>
    /// <param name="entry">The asset entry to be exported.</param>
    /// <param name="path">The export path to be used.</param>
    /// <param name="filterType">The export type to be used.</param>
    /// <returns>A bool indicating the success of this operation.</returns>
    public static bool BetterExport(this AssetDefinition assetDefinition, EbxAssetEntry entry, string path, string filterType)
    {
      if (Directory.Exists(path))
      {
        path = string.Format("{0}.{1}", Path.Combine(new string[]
        {
          path,
          entry.Filename
        }), filterType);
      }

      // Pass this method's parameters to the real export method
      return assetDefinition.Export(entry, path, filterType);
    }

    /// <summary>
    /// Retrieves the value of a field within a class regardless of its access modifiers.
    /// </summary>
    /// <param name="targetField">The field name to be used.</param>
    public static object GetFieldValue(this object refObject, string targetField)
    {
      Type currentType = refObject.GetType();

      FieldInfo objectField = null;

      while (currentType != null && objectField == null)
      {
        // Assign to the object field with the current FieldInfo retrieval attempt
        objectField = currentType.GetField(targetField, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        currentType = currentType.BaseType;
      }

      return objectField != null ? objectField.GetValue(refObject) : null;
    }

    // Methods listed below were also not originally in ClassExtensions; they were moved to this class for cleanliness within the plugin's internals

    /// <summary>
    /// Adds a NameValue pair to this <see cref="CustomComboData{T, U}"/>.
    /// </summary>
    /// <param name="name">The name to be added.</param>
    /// <param name="value">The optional value to be added. If null, the name will simply be used as the value if both <see cref="List{T}"/> instances use the same element type.</param>
    public static void AddNameValuePair<T, U>(this CustomComboData<T, U> customComboData, U name, T value = default)
    {
      customComboData.Names.Add(name);

      // Add the provided value if it's assigned; otherwise, use the name
      customComboData.Values.Add(value != null && !value.Equals(default) ? value : (T)Convert.ChangeType(name, typeof(T)));
    }

    /// <summary>
    /// Opens a variety of assets from a provided list of asset entries.
    /// </summary>
    /// <param name="entries">The collection of asset entries to be used.</param>
    /// <param name="createDefaultEditors">A bool determining whether or not the base asset editor should be used by all newly-created tabs.</param>
    /// <param name="passthroughTask">An optional <see cref="FrostyTaskWindow"/> passthrough parameter for current asset status alterations.</param>
    public static void OpenAssets(this IEditorWindow editorWindow, List<AssetEntry> entries, bool createDefaultEditors, FrostyTaskWindow passthroughTask = null)
    {
      AssetEntry currentEntry;

      for (int i = 0; i < entries.Count; i++)
      {
        currentEntry = entries[i];

        if (currentEntry != null)
        {
          editorWindow.OpenAsset(currentEntry, createDefaultEditors);

          if (passthroughTask != null)
          {
            passthroughTask.Update(currentEntry.Filename, (i + 1) / entries.Count * 100);
          }
        }
      }
    }

    /// <summary>
    /// Closes all <see cref="FrostyTabItem"/> instances that are found to be using an asset from a provided list of asset entries.
    /// </summary>
    /// <param name="sourceEntries">The list of <see cref="AssetEntry"/> instances to be used.</param>
    /// <returns>An int representing the total quantity of tabs closed.</returns>
    public static int RemoveTabs(this IEditorWindow editorWindow, List<AssetEntry> sourceEntries, bool onlyModified = false)
    {
      MethodInfo removeTabInfo = App.EditorWindow.GetType().GetMethod("RemoveTab", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

      int closedTabs = 0;

      FrostyTabControl mainTabControl = (FrostyTabControl)editorWindow.GetFieldValue("tabControl") ?? (FrostyTabControl)editorWindow.GetFieldValue("TabControl");

      FrostyTabItem currentSelection;

      for (int i = 0; i < sourceEntries.Count; i++)
      {
        for (int j = 0; j < mainTabControl.Items.Count; j++)
        {
          currentSelection = (FrostyTabItem)mainTabControl.Items[j];

          // Check if the selected asset entry matches the tab's opened asset
          if (sourceEntries[i].Name == currentSelection.TabId && (!onlyModified || sourceEntries[i].IsModified))
          {
            // Remove the tab
            removeTabInfo.Invoke(App.EditorWindow, new object[]
            {
              currentSelection
            });

            closedTabs++;

            break;
          }
        }
      }

      return closedTabs;
    }
  }
}
