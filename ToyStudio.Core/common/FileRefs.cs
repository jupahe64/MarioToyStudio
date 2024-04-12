namespace ToyStudio.Core.Common
{
    internal interface IFileRef<T>
        where T : IFileRef<T>
    {
        public static abstract T FromRefString(string refString);
        public string GenerateRefString();
        public string[] GetPath();

        public string Name { get; }
    }

    internal class BcettRef(string name) : IFileRef<BcettRef>
    {
        public static BcettRef FromRefString(string refString)
        {
            if (!refString.StartsWith(RefStringPrefix) || !refString.EndsWith(RefStringSuffix))
                throw new ArgumentException(
                    $"{refString} does not match the pattern {RefStringPrefix}<NAME>{RefStringSuffix}");

            return new BcettRef(refString[RefStringPrefix.Length..^RefStringSuffix.Length]);
        }

        public string GenerateRefString() => string.Concat(RefStringPrefix, name, RefStringSuffix);

        public string[] GetPath() => ["Banc", name + ".bcett.byml.zs"];

        public string Name => name;

        private const string RefStringPrefix = "Work/Banc/Scene/";
        private const string RefStringSuffix = ".bcett.json";
    }
}
