namespace Helion.Resources.Archives.Locator;

/// <summary>
/// An interface for an object that is responsible for locating an archive. This
/// means it may find it on the local computer as file, from memory, or even by
/// things like a data stream over an internet connection.
///
/// The implementation will be given some kind of URI and it will be responsible
/// for finding it, or notifying the caller that it cannot be found.
/// </summary>
public interface IArchiveLocator
{
    /// <summary>
    /// Finds the resource at the URI provided.
    /// </summary>
    /// <param name="uri">The resource locator index.</param>
    /// <returns>The archive, or an error reason on why it cannot be found.
    /// </returns>
    Archive? Locate(string uri);

    /// <summary>
    /// Checks the search paths for the archive, without opening it or confirming its type,
    /// returning its path if found.
    /// </summary>
    string? LocateWithoutLoading(string uri);
}
