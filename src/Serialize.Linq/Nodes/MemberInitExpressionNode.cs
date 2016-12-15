﻿#region Copyright
//  Copyright, Sascha Kiefer (esskar)
//  Released under LGPL License.
//  
//  License: https://raw.github.com/esskar/Serialize.Linq/master/LICENSE
//  Contributing: https://github.com/esskar/Serialize.Linq
#endregion

using Serialize.Linq.Factories;
using System.Linq.Expressions;
using System.Runtime.Serialization;

namespace Serialize.Linq.Nodes
{
    [DataContract(Name = "MIE")]
    public class MemberInitExpressionNode : ExpressionNode<MemberInitExpression>
    {
        public MemberInitExpressionNode() { }

        public MemberInitExpressionNode(INodeFactory factory, MemberInitExpression expression)
            : base(factory, expression) { }

        [DataMember(EmitDefaultValue = false, Name = "B")]
        public MemberBindingNodeList Bindings { get; set; }

        [DataMember(EmitDefaultValue = false, Name = "N")]
        public NewExpressionNode NewExpression { get; set; }

        protected override void Initialize(MemberInitExpression expression)
        {
            this.Bindings = new MemberBindingNodeList(this.Factory, expression.Bindings);
            this.NewExpression = (NewExpressionNode)this.Factory.Create(expression.NewExpression);
        }

        public override Expression ToExpression(ExpressionContext context)
        {
            return Expression.MemberInit((NewExpression)this.NewExpression.ToExpression(context), this.Bindings.GetMemberBindings(context));
        }
    }
}
