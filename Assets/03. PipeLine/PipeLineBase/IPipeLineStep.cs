namespace _03._PipeLine.PipeLineBase
{
    public interface IPipeLineStep<T>
    {
        T Execute(T context);
    }
}
