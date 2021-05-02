using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AFK_Script_Interpreter
{
    class Value : IEquatable<Value>
    {
        private Func<Value, dynamic> valueGetter;

        public dynamic Get(bool nullWhenException = true) {
            try
            {
                return valueGetter(this);
            }
            catch (Exception)
            {
                if (nullWhenException) return null;
                else throw;
            }
        }
        public DataType Type;

        public static Value Create(DataType type, Func<Value, dynamic> valueFunc) => new Value
        {
            Type = type,
            valueGetter = valueFunc
        };
        
        public static Value Create(DataType type, dynamic value) => new Value
        {
            Type = type,
            valueGetter = _ => value
        };
        
        public bool Equals(Value other)
        {
            return Type == other.Type && Get() == other.Get();
        }
    }
}
