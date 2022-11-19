//GNU GENERAL PUBLIC LICENSE Version 3
using System;
using System.Collections.Generic;
using MetadataExtractor;
using System.IO;
using static DcimIngester.Utilities;

namespace DcimIngester.Windows {
    class FileTreeEvaluator {
        
        public FileTreeEvaluator(){}

        public object Evaluate(SyntaxNode node) {
            switch(node) {
                case BlockNode b: return blockNode(b);
                case RuleNode r: return ruleNode(r);
                case PathNode p: return PathToString(p);
                case StringNode s: return s.Value;
                case ExtensionNode: return "extension";
                case YearNode: return "YYYY";
                case MonthNode: return "MM";
                case DayNode: return "DD";
                case HourNode: return "hh";
                case MinuteNode: return "mm";
                case SecondNode: return "ss";
                case FileNameNode: return "file name";
                case PathPartNode p: return $"path[{p.Part}]";
                case PathNameNode: return "path";
                case MetadataNode m: return $"{m.Directory};{m.Tag}";
            }
            throw(new Exception($"Unknown node type {node.GetType()}"));
        }

        private string PathToString(PathNode path){
            string result = "";
            foreach (SyntaxNode part in path.Parts){
                result = Path.Join(result,Convert.ToString(Evaluate(part)));
            }
            return result;
        }

        private List<string> ruleNode(RuleNode r)
        {
            List<String> result = new List<string>();

            if (r.GetIndent() is not null){
                foreach(string path in (List<String>)Evaluate(r.GetIndent())){
                    if (r.Path is not EmptyNode)
                        result.Add(Path.Join((string)Evaluate((PathNode)r.Path),path));
                    else
                        result.Add(path);
                }
            }
            else{
                result.Add((string)Evaluate((PathNode)r.Path));
            }
            if (r.Under is not EmptyNode){
                result.AddRange((List<String>)Evaluate(r.Under));
            }

            return result;
        }

        private List<String> blockNode(BlockNode b)
        {
            List<String> result = new List<string>();
            foreach (RuleNode rule in b.Statements){
                result.AddRange((List<String>)Evaluate(rule));
            }
            return result;

        }
    }
}