public abstract class ConditionNode : SyntaxNode{
    public SyntaxNode l {get;}
    public string r {get;}
    
    public ConditionNode(SyntaxNode l, string r){
        this.l = l;
        this.r= r;
    }
    public override string ToString()
    {
        return base.ToString() + " " + this.l.ToString() + " : " + this.r ;
    }
}