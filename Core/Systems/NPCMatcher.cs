using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace YharimEX.Core.Systems
{
    public class NPCMatcher
    {
        public List<INPCMatchCondition> Conditions;

        public NPCMatcher()
        {
            int num = 1;
            List<INPCMatchCondition> list = new List<INPCMatchCondition>(num);
            CollectionsMarshal.SetCount(list, num);
            Span<INPCMatchCondition> span = CollectionsMarshal.AsSpan(list);
            int num2 = 0;
            span[num2] = new MatchEverythingCondition();
            num2++;
            Conditions = list;
        }

        public NPCMatcher MatchType(int type)
        {
            Conditions.Add(new MatchTypeCondition(type));
            return this;
        }

        public NPCMatcher MatchTypeRange(params int[] types)
        {
            Conditions.Add(new MatchTypeRangeCondition(types));
            return this;
        }

        public bool Satisfies(int type)
        {
            return Conditions.TrueForAll((INPCMatchCondition condition) => condition.Satisfies(type));
        }
    }
    public interface INPCMatchCondition
    {
        bool Satisfies(int type);
    }

    public class MatchEverythingCondition : INPCMatchCondition
    {
        public bool Satisfies(int type)
        {
            return true;
        }
    }

    public class MatchTypeCondition : INPCMatchCondition
    {
        public int Type;

        public MatchTypeCondition(int type)
        {
            Type = type;
        }

        public bool Satisfies(int type)
        {
            return type == Type;
        }
    }

    public class MatchTypeRangeCondition : INPCMatchCondition
    {
        public int[] Types;

        public MatchTypeRangeCondition(IEnumerable<int> types)
        {
            Types = types.ToArray();
        }

        public MatchTypeRangeCondition(params int[] types)
        {
            Types = types;
        }

        public bool Satisfies(int type)
        {
            return Types.Contains(type);
        }
    }
}
