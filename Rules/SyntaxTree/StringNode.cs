public class StringNode : SyntaxNode{
    public string Value {get;}
    public StringNode(string value){
        this.Value = value;
    }

    public override string ToString()
    {
        return base.ToString() + " " + this.Value;
    }
}