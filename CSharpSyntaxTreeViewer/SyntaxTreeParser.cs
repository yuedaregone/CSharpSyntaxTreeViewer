using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Text;

namespace CSharpSyntaxTreeViewer
{
    public class SyntaxTreeParser
    {
        public SyntaxNode ParseCode(string code)
        {
            var tree = CSharpSyntaxTree.ParseText(code);
            return tree.GetRoot();
        }

        public ChildSyntaxList GetChildrenAndToken(SyntaxNode node)
        {
            return node.ChildNodesAndTokens();
        }

        public string GetDetailedNodeInfo(SyntaxNode node)
        {
            return $"{node.Kind()} - {node.GetType().Name}";
        }
    }
}