namespace Models;

public struct Update
{
    public string Name { get; set; }
    public string Level { get; set; }

    public Update(string name, string level)
    {
        Name = name;
        Level = level;
    }

    public static Update FromArray(string[] values) =>
        new(values[0], values[1]);

    public string[] ToArray() =>
        new string[] { this.Name, this.Level };
}
