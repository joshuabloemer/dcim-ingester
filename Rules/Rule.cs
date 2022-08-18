using System;
using System.Linq;

public class Rule {
    public int indentLevel {get;}
    private string _rule;
    public string rule {
        get{return _rule;}
        set{
            _rule = value;
            updateChildren();
        }
    }

    public string? path {get;set;}
    public Rule? under {get;private set;}
    public Rule? indent {get;private set;}
    public Rule? next {get;private set;}

    public Rule(int indentLevel){
        this.indentLevel = indentLevel;
        this._rule = "";
        Console.WriteLine(indentLevel);

    }

    private void updateChildren(){
        if (this._rule.Length > 0) {
            under = new Rule(this.indentLevel);
            indent = new Rule(this.indentLevel+1);
            next = new Rule(this.indentLevel);
        }
   }

}