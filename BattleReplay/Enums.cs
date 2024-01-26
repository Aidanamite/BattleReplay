using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleReplay
{
    public enum ReplaySaving
    {
        Ask,
        AlwaysSave,
        NeverSave
    }

    public enum EventType
    {
        Movement,
        Ability,
        EndOfTurn
    }
}
