public class ConditionNode : SyntaxNode{
    public string Type {get;}
    public string Operand {get;}
    public string Value {get;}

    public ConditionNode(string type, string operand, string value){
        this.Type = type;
        this.Operand= operand;
        this.Value=value;

    }

    public override string ToString()
    {
        return base.ToString() + " " + this.Type + this.Operand + this.Value;
    }
}