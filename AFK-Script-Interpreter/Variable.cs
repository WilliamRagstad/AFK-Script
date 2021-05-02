using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AFK_Script_Interpreter
{
    class Variable : IEquatable<Variable>
    {
        public string Name;
        public Value Value;
        
        public static Variable Create(DataType type, Value value) => new Variable
        {
            Value = value
        };
        
        public static Variable Create(string name, DataType type, Value value) => new Variable
        {
            Name = name,
            Value = value
        };
        public static Variable Create(DataType type, dynamic value) => new Variable
        {
            Value = Value.Create(type, _ => value)
        };
        public static Variable Create(string name, DataType type, dynamic value) => new Variable
        {
            Name = name,
            Value = Value.Create(type, _ => value)
        };
        public static Variable Create(DataType type, Func<Value, dynamic> valueFunc) => new Variable
        {
            Value = Value.Create(type, valueFunc)
        };
        public static Variable Create(string name, DataType type, Func<Value, dynamic> valueFunc) => new Variable
        {
            Name = name,
            Value = Value.Create(type, valueFunc)
        };

        public bool Equals(Variable other)
        {
            return Value.Equals(other.Value);
        }
    }
}
