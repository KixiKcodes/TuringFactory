using System.Reflection;

namespace TuringFactory;

static class ResourceLoader {
    static readonly Assembly Assembly = typeof(ResourceLoader).Assembly;

    public static byte[] ReadBytes(string path) {
        string resourceName = path.Replace('\\', '/');
        using Stream? stream = Assembly.GetManifestResourceStream(resourceName) ?? throw new FileNotFoundException($"Embedded resource '{resourceName}' was not found.");
        using MemoryStream buffer = new();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }
}
