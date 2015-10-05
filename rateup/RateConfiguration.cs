using System;
using System.Collections.Generic;
using ExpressionEvaluator;

namespace rateup
{
    public class RateConfiguration
    {
        public string Name { get; set; }
        public IList<RateCofigurationClass> Classes { get; set; }
        public RateConfigurationInputFormat InputFormats { get; set; }
        public IList<RateConfigExpression> Expressions { get; set; }
    }

    public class RateCofigurationClass
    {
        public string Name { get; set; }
        public string Pattern { get; set; }
        public int Priority { get; set; }
        public IList<RateConfigurationClassExpression> Expressions { get; set; }
    }

    public class RateConfigurationClassExpression
    {
        public string Name { get; set; }
        public decimal Param { get; set; }
    }

    public class RateConfigExpression
    {
        public string Name { get; set; }
        public string Expression { get; set; }

        public decimal Evaluate(decimal input, decimal paramY)
        {
            var registry = new TypeRegistry();
            registry.RegisterType("m", typeof (Math));
            registry.RegisterSymbol("x", input);
            registry.RegisterSymbol("y", paramY);

            var expression = new CompiledExpression<decimal>(Expression)
            {
                TypeRegistry = registry
            };
            return expression.Eval();
        }
    }

    public class RateConfigurationInputFormat
    {
        public int MatchColumnId { get; set; }
        public string MatchPrefix { get; set; }
        public bool HasHeaders { get; set; }
        public IList<RateConfigurationInputFormatColumn> Columns { get; set; }
    }

    public class RateConfigurationInputFormatColumn
    {
        public int? Id { get; set; }
        public bool Rate { get; set; }
        public string[] Restrict { get; set; }

        public RateConfigurationInputFormatColumnOutput Output { get; set; }
    }

    public class RateConfigurationInputFormatColumnOutput
    {
        public bool Output { get; set; }
        public int Id { get; set; }
        public string DefaultValue { get; set; }
    }
}