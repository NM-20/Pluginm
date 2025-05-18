namespace Pluginm.Ports.Classes
{
  public interface IValidationPage
  {
    /// <summary>
    /// Validates the user input of a page object.
    /// </summary>
    /// <returns>A bool based on the validation process.</returns>
    bool Validate();
  }
}
