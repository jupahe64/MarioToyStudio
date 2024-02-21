namespace ToyStudio.Core.common.byml_serialize
{
    public class BymlProperty : Attribute
    {
        public string Key { get; set; }
        public object DefaultValue { get; set; }

        public BymlProperty() { }

        public BymlProperty(string key)
        {
            Key = key;
        }
    }
}
