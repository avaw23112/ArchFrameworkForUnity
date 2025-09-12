namespace Tools.Pool
{
    internal interface IPool<T>
    {
        public T Get();

        public void Release(T obj);
    }
}