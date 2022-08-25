public class PathNode : SyntaxNode{
    public string Value {get;}
    public PathNode(string value){
        this.Value = value;
    }

    public override string ToString()
    {
        return base.ToString() + " " + this.Value;
    }
}