using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AFK_Script_Interpreter
{
    class Variable : IEqualityComparer<Variable>
    {
        public string Name;
        private Func<Variable, dynamic> Value;

        private static string defaultValueGetter(Variable v) { return v.Value.ToString(); }
        
        public static Variable CreateVariable(Func<Variable, dynamic> valueFunc = null) => new Variable
            {
                Value = valueFunc ?? defaultValueGetter
            };
        
        public static Variable CreateVariable(string name, Func<Variable, dynamic> valueFunc = null) => new Variable
            {
                Name = name,
                Value = valueFunc ?? defaultValueGetter
            };
        public static Variable CreateVariable(dynamic value) => new Variable
            {
                Value = _ => value
            };
        public static Variable CreateVariable(string name, dynamic value) => new Variable
            {
                Name = name,
                Value = _ => value
            };

        public dynamic GetValue() => Value(this);

        bool IEqualityComparer<Variable>.Equals(Variable x, Variable y)
        {
            return x.Value == y.Value;
        }

        public int GetHashCode(Variable obj)
        {
            return Name.GetHashCode();
        }
    }
}
