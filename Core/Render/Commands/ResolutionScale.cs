namespace Helion.Render.Commands
{
    /// <summary>
    /// Extra information on scaling a drawn image.
    /// </summary>
    /// <remarks>
    /// The following is an example of each type if this is drawn at a custom
    /// resolution on widescreen.
    ///
    /// None (looks bad because it assumes a custom different resolution):
    ///   +-----------------------+
    ///   |...........            |
    ///   |...........            |
    ///   |...........            |
    ///   |...........            |
    ///   |...........            |
    ///   +-----------------------+
    ///
    /// Centered:
    ///   +-----------------------+
    ///   |      ...........      |
    ///   |      ...........      |
    ///   |      ...........      |
    ///   |      ...........      |
    ///   |      ...........      |
    ///   +-----------------------+
    ///
    /// Stretched (it would be filled in properly though):
    ///   +-----------------------+
    ///   |. . . . . . . . . . . .|
    ///   |. . . . . . . . . . . .|
    ///   |. . . . . . . . . . . .|
    ///   |. . . . . . . . . . . .|
    ///   |. . . . . . . . . . . .|
    ///   +-----------------------+
    /// </remarks>
    public enum ResolutionScale
    {
        /// <summary>
        /// Indicates nothing should be done.
        /// </summary>
        None,

        /// <summary>
        /// Centers the drawn image. This is the same as adjusting the drawing
        /// such that there would be horizontal invisible bars on the left and
        /// right that make it look like it's drawn at 4:3 and placed on the
        /// center.
        /// </summary>
        Center,

        /// <summary>
        /// Stretches the image so it fills the entire viewport.
        /// </summary>
        Stretch
    }
}
