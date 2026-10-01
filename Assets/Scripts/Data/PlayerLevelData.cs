using System;
using UnityEngine;

namespace TopDownGame.Data
{
    [Serializable]
    public class PlayerLevelData
    {
        public int Level;
        public long ExpUpGrade;
        public int BaseAwardExp;
        public int RunSpeed;
        public int AttackSpeed;
        public int FightPower;
        public int AttackSeriesResist;
    }
}
