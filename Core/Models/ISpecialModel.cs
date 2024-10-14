using Helion.World;
using Helion.World.Special;
using System.Text.Json.Serialization;

namespace Helion.Models;

[JsonConverter(typeof(SpecialModelConverter))]
public interface ISpecialModel
{
    ISpecial? ToWorldSpecial(IWorld world);
}
