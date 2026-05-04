public class ResourceData
{
    public string Name;
    public int Current;
    public int Max;

    public ResourceData(string name, int initialMax)
    {
        Name = name;
        Max = initialMax;
        Current = 0;
    }
}