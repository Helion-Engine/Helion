namespace Helion.Resources.Archives;

public class IndexGenerator : IIndexGenerator
{
    private int m_index;

    public int GetIndex(Archive archive) => m_index++;
}
