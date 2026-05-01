namespace PipeLine.PipeLineBase
{
    public interface IPipeLineStep<T>
    {
        T Execute(T context);
    }
}
