﻿#region Copyright
//  Copyright, Sascha Kiefer (esskar)
//  Released under LGPL License.
//  
//  License: https://raw.github.com/esskar/Serialize.Linq/master/LICENSE
//  Contributing: https://github.com/esskar/Serialize.Linq
#endregion

using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using Serialize.Linq.Extensions;
using Serialize.Linq.Factories;

namespace Serialize.Linq.Nodes
{
    [DataContract(Name = "L")]
    public class LambdaExpressionNode : ExpressionNode<LambdaExpression>
    {
        public LambdaExpressionNode() { }

        public LambdaExpressionNode(INodeFactory factory, LambdaExpression expression)
            : base(factory, expression) { }

        [DataMember(EmitDefaultValue = false, Name = "B")]
        public ExpressionNode Body { get; set; }

        [DataMember(EmitDefaultValue = false, Name = "P")]
        public ExpressionNodeList Parameters { get; set; }

        protected override void Initialize(LambdaExpression expression)
        {
            Parameters = new ExpressionNodeList(Factory, expression.Parameters);
            Body = Factory.Create(expression.Body);
        }

        public override Expression ToExpression(ExpressionContext context)
        {
            var body = Body.ToExpression(context);
            var parameters = Parameters.GetParameterExpressions(context).ToArray();

            var bodyParameters = body.GetNodes().OfType<ParameterExpression>().ToArray();
            for (var i = 0; i < parameters.Length; ++i)
            {
                var matchingParameter = bodyParameters.Where(p => p.Name == parameters[i].Name && p.Type == parameters[i].Type).ToArray();
                if (matchingParameter.Length == 1)
                    parameters[i] = matchingParameter.First();
            }

            return Expression.Lambda(Type.ToType(context), body, parameters);
        }
    }
}
