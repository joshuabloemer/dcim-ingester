public class MetadataNode : SyntaxNode{
    public string Directory {get;}

    public string Tag {get;}

    public MetadataNode(string directory, string tag){
        this.Directory = directory;
        this.Tag = tag;
    }

    public override string ToString()
    {
        return base.ToString() + " " + this.Directory + " - " + this.Tag;
    }
}