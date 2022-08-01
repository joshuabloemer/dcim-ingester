using System;
using System.Linq;



public class Rule {
    public string indentLevel {get;}
    public string? rule {
        get{
            return rule;
        }
        set{
            rule = value;
            updateChildren(value);
        }
    }

    public string? path {get;set;}
    public Rule? under {get;set;}
    public Rule? indent {get;set;}
    public Rule? next {get;set;}

    public Rule(string indentLevel){
        this.indentLevel = indentLevel;
        Console.WriteLine(indentLevel);

    }

    public void updateChildren(string value){
        if (value.Length > 0) {
            string[] levels = this.indentLevel.Split("_");

            if (this.under == null) {
                string new_under = String.Join("_",levels[0..^1]) +"_"+ (Int32.Parse(levels[^1])+1);
                this.under = new Rule(new_under);
            }
            if (this.indent == null) {
                string new_indent = indentLevel+"_1_1";
                this.indent = new Rule(new_indent);
            }
            if (this.next == null) {
                string new_next = String.Join("_",new string[]{String.Join("_",levels[0..^2]),(Int32.Parse(levels[^2])+1) +"_1"}.Where(s => !string.IsNullOrEmpty(s)));
                this.next = new Rule(new_next);
            }
            //     string indentlevel = textBox.Name.Substring(4);
            //     string[] levels = indentlevel.Split("_");
            //     // Console.WriteLine(indentlevel);
            //     // // Console.WriteLine(levels[0]);
            //     // // Console.WriteLine(levels[1]);
            //     // var under = this.FindName("Rule" + String.Join("_",levels[0..^2]) +"_"+ levels[^2] +"_"+ (Int32.Parse(levels[^1])+1));
            //     // var indent = this.FindName(textBox.Name+"1_1");
            //     // var next = this.FindName("Rule" + (Int32.Parse(levels[^1])+1) +"_"+ 1);
            //     // //levels[^2] +"_"+ (Int32.Parse(levels[^1])+1))

            //     // Console.WriteLine("Rule" + String.Join("_",levels[0..^1]) +"_"+ (Int32.Parse(levels[^1])+1));
            //     // Console.WriteLine(textBox.Name+"_1_1");
            //     // // TODO: fix issue where extra underscore is placed before  
            //     // Console.WriteLine("Rule" + String.Join("_",levels[0..^2]) +"_"+ (Int32.Parse(levels[^2])+1) +"_1");
        } 
    }

}