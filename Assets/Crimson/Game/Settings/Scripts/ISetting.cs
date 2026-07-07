namespace Crimson.Setting
{
    public interface ISetting<T>
    {
        T Choice { get; set; }
    }
}