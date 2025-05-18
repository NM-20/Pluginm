using Frosty.Controls;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace Pluginm.Ports.Classes.Windows
{
  public partial class PageInputWindow : FrostyDockableWindow
  {
    /// <summary>
    /// An int for representing this <see cref="PageInputWindow"/>'s selected page.
    /// </summary>
    public int CurrentPage
    {
      get;
      private set;
    } = 0;

    /// <summary>
    /// A list of objects for storing the page objects of this <see cref="PageInputWindow"/>.
    /// </summary>
    public List<object> PageObjects
    {
      get;
      private set;
    } = new List<object>();

    /// <summary>
    /// Initializes a new instance of the <see cref="PageInputWindow"/> class.
    /// </summary>
    /// <param name="title">The title to be used.</param>
    /// <param name="pageObjects">The objects to be used within the asset property grid. Providing multiple objects will cause the window to use a paging system.</param>
    /// <param name="overrideWidth">An optional int to be used as the width for this <see cref="PageInputWindow"/> instance.</param>
    /// <param name="overrideHeight">An optional int to be used as the height for this <see cref="PageInputWindow"/>.</param>
    public PageInputWindow(string title, IEnumerable<object> pageObjects, int overrideWidth = 550, int overrideHeight = 542)
    {
      InitializeComponent();

      if (pageObjects != null)
        AddPages(pageObjects);

      // Assign to the title if it isn't null
      Title = !string.IsNullOrEmpty(title) ? title : Title;

      // Assign to the width and height of this window with the associated override if available
      Width = overrideWidth;
      Height = overrideHeight;

      // Register the "Enter" and "Escape" keybindings to their associated click events
      CommandBindings.RegisterKeyBindings(new Dictionary<KeyGesture, ExecutedRoutedEventHandler>
      {
        { new KeyGesture(Key.Enter), new ExecutedRoutedEventHandler(NextButton_Click) },
        { new KeyGesture(Key.Escape), new ExecutedRoutedEventHandler(CancelButton_Click) }
      });
    }

    /// <summary>
    /// Generic click event.
    /// </summary>
    /// <param name="sender">Generic parameter.</param>
    /// <param name="e">Generic parameter.</param>
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
      // Check if the selected page is the first page
      if (CurrentPage <= 1)
      {
        // Since the isSuccessful bool is false by default, it does not need assignment here

        Close();
      }
      else
      {
        CurrentPage--;
        Refresh();

        // Set the asset property grid's selected object
        assetPropertyGrid.SetClass(PageObjects[CurrentPage - 1]);
      }
    }

    /// <summary>
    /// Generic click event.
    /// </summary>
    /// <param name="sender">Generic parameter.</param>
    /// <param name="e">Generic parameter.</param>
    private void NextButton_Click(object sender, RoutedEventArgs e)
    {
      object currentPage = PageObjects[CurrentPage - 1];

      // Check if the type of the current page object is of a validation page and if the validation was successful
      if (currentPage is IValidationPage && !((IValidationPage)currentPage).Validate())
        return;

      // Check if the current page is the last page
      if (CurrentPage == PageObjects.Count)
      {
        DialogResult = true;
        Close();
      }
      else
      {
        CurrentPage++;
        Refresh();

        // Set the property grid's object to the new page
        assetPropertyGrid.SetClass(PageObjects[CurrentPage - 1]);
      }
    }

    /// <summary>
    /// Adds a collection of new pages to the current collection of page objects.
    /// </summary>
    /// <param name="pageObjects">The page objects to be added.</param>
    /// <param name="insertIndex">The optional index at which the new pages should be added.</param>
    public void AddPages(IEnumerable<object> pageObjects, int? insertIndex = null)
    {
      bool noPages = PageObjects.Count <= 0;

      // Check if the insert index is assigned
      if (insertIndex.HasValue && insertIndex.Value <= PageObjects.Count && insertIndex.Value >= 0)
      {
        // Add the pages at the specified index
        PageObjects.InsertRange(insertIndex.Value, pageObjects);
      }
      else
      {
        // Add the pages to the list of page objects normally
        PageObjects.AddRange(pageObjects);
      }

      // Check if there was previously no page objects and if any new pages were even added
      if (noPages && PageObjects.Count > 0)
      {
        // Set the current page to the first page if there was
        CurrentPage = 1;

        // Assign to the asset property grid's selected object
        assetPropertyGrid.SetClass(PageObjects[CurrentPage - 1]);
      }

      Refresh();
    }

    /// <summary>
    /// Removes a page object at the specified index.
    /// </summary>
    /// <param name="index">The insert index to be used.</param>
    /// <returns>A bool indicating whether or not the page could be removed.</returns>
    public bool RemovePage(int index)
    {
      // Check if the index is less than or equal to the amount of indexed page objects and if it's greater than or equal to 0
      if (index <= PageObjects.Count - 1 && index >= 0)
      {
        // Remove the page at the specified index
        PageObjects.RemoveAt(index);

        if (PageObjects.Count > 0)
        {
          // Set the page to the first page to avoid any conflicts
          CurrentPage = 1;
          assetPropertyGrid.SetClass(PageObjects[CurrentPage - 1]);
        }
        else
        {
          // Set the current page to zero to indicate an invalid page
          CurrentPage = 0;

          // Empty the property grid
          assetPropertyGrid.SetClass(null);
        }

        Refresh();

        return true;
      }
      else
      {
        return false;
      }
    }

    /// <summary>
    /// Updates the <see cref="PageInputWindow"/> to accommodate for any changes.
    /// </summary>
    public void Refresh()
    {
      pageText.Text = PageObjects.Count > 0 ? string.Format("Page {0} of {1}", CurrentPage, PageObjects.Count) : "No pages available";

      // Assign to the cancel button's text based on the current page being the first page or an invalid page
      cancelButton.Content = CurrentPage <= 1 ? "Cancel" : "Back";

      // Assign to the cancel button's tooltip based on the same condition
      cancelButton.ToolTip = string.Format("{0} (Esc)", CurrentPage <= 1 ? "Close the window." : "Return to the previous page.");

      // Assign to the next button's text based on the current page not being the last page
      nextButton.Content = CurrentPage != PageObjects.Count ? "Next" : "Ok";

      // Enable the next button based on the page text not being "No pages available"
      nextButton.IsEnabled = pageText.Text != "No pages available";

      // Assign to the next button's tooltip based on the same condition as the content assignment
      nextButton.ToolTip = string.Format("{0} (Enter)", CurrentPage != PageObjects.Count ? "Proceed to the next available page." : "Close the window with the provided input.");
    }
  }
}
