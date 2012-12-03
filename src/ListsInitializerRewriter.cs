using System.Collections.Generic;
using System.Linq;
using Roslyn.Compilers.CSharp;

namespace RoslynCSharpExtensions
{
    /// <summary>
    /// Adds possibility to initialize lists like "[<1;2;3>]" which will be short equivalent of "new List<int>{1;2;3}"
    /// </summary>
    public class ListsInitializerRewriter : SyntaxRewriter
    {
        private readonly SemanticModel semanticModel;

        public ListsInitializerRewriter(SemanticModel semanticModel)
        {
            this.semanticModel = semanticModel;
        }

        public override SyntaxNode VisitElementAccessExpression(ElementAccessExpressionSyntax node)
        {
            var elements = GetListCollectionInitializerElements(node);
            if (elements != null)
            {
                if (elements.Count > 0)
                {
                    var type = GetArgumentType(elements[0]);
                    var syntaxList = new SeparatedSyntaxList<ExpressionSyntax>();
                    var intializerExpr = Syntax.InitializerExpression(SyntaxKind.CollectionInitializerExpression, syntaxList.Add(elements.ToArray()));

                    return Syntax.ParseExpression(string.Format("new System.Collections.Generic.List<{1}>{0}", intializerExpr, type));
                }
                else
                {
                    //no elements of list - returning empty list of objects
                    return Syntax.ParseExpression("new System.Collections.Generic.List<Object>()");
                }
              
            }
            return base.VisitElementAccessExpression(node);
        }

        private TypeSymbol GetArgumentType(ExpressionSyntax expression)
        {
            var info = semanticModel.GetTypeInfo(expression);

            var resultantType = info.Type;
            return resultantType;
        }

        private static List<ExpressionSyntax> GetListCollectionInitializerElements(ElementAccessExpressionSyntax node)
        {
            var arguments = node.ArgumentList.Arguments;

            if (arguments.Count == 0)
                return null;
            if (arguments.Count == 1)
            {
                var arg = arguments[0];
                //should be greaterThen expression containing lessthen expression
                var greaterThenBinaryExpression = arg.Expression as BinaryExpressionSyntax;
                if (greaterThenBinaryExpression == null || greaterThenBinaryExpression.OperatorToken.Kind != SyntaxKind.GreaterThanToken)
                    return null;
                var lessThenBinaryExpression = greaterThenBinaryExpression.ChildNodes().OfType<BinaryExpressionSyntax>().FirstOrDefault();
                if (lessThenBinaryExpression == null || lessThenBinaryExpression.OperatorToken.Kind != SyntaxKind.LessThanToken)
                    return null;
                var result = lessThenBinaryExpression.ChildNodes().OfType<ExpressionSyntax>().SingleOrDefault(child => !child.IsMissing);
                if (result == null)
                {
                    //we are dealing with [<>] construct - returning empty list
                    return new List<ExpressionSyntax>();
                }
                return new List<ExpressionSyntax> { result };
            }
            else
            {
                var first = arguments[0].Expression as BinaryExpressionSyntax;
                var last = arguments.Last().Expression as BinaryExpressionSyntax;
                if (first == null || first.Kind != SyntaxKind.LessThanExpression
                    || last == null || last.Kind != SyntaxKind.GreaterThanExpression)
                {
                    return null;
                }

                var result = new List<ExpressionSyntax> { first.Right, last.Left };
                var totalArgs = arguments.Count;
                result.InsertRange(1, arguments.Skip(1).Take(totalArgs - 2).Select(arg => arg.Expression));
                return result;
            }
        }

    }
}