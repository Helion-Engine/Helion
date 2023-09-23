using Helion.Maps.Bsp.States.Split;

namespace Helion.Maps.Bsp;

/// <summary>
/// A simple configuration class that holds tunable parameters.
/// </summary>
public class BspConfig
{
    /// <summary>
    /// The epsilon distance to which any vertices within this distance will
    /// be assumed to be the same vertex.
    /// </summary>
    public readonly double VertexWeldingEpsilon = 0.005;

    /// <summary>
    /// The distance (in length, or map units) from an endpoint to which we
    /// consider it a close enough split to punish such a split score-wise
    /// when selecting a splitter.
    /// </summary>
    public readonly double PunishableEndpointDistance = 0.1;

    /// <summary>
    /// The weights for each split criteria.
    /// </summary>
    public readonly SplitWeights SplitWeights = new SplitWeights();

    /// <summary>
    /// When doing a split, it will branch always take a branch to the
    /// right when doing it recursively.
    /// </summary>
    /// <remarks>
    /// This can be used to control the direction of descent, so if you
    /// want to go down a right path in some debugging circumstance, you
    /// can achieve that by having this set to true. This way you do not
    /// need to evaluate the entire left side of the tree and descend to
    /// the place you want to go immediately. If this is false, it will
    /// take branches to the left first.
    /// </remarks>
    public bool BranchRight = true;
}
