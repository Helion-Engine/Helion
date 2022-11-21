namespace Helion.Resources.Archives;

public class IndexGenerator : IIndexGenerator
{
    private int m_index = 0;

    public int GetIndex(Archive archive) => m_index++;
}
