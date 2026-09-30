using System;
using System.Collections.Generic;

namespace TopDownGame.Data
{
    [Serializable]
    public class PartSlotData
    {
        public int slotId;
        public string slotName;
        public string description;
        public List<string> aliasNames = new List<string>();

        public PartSlotData(int id, string name, string des)
        {
            this.slotId = id;
            this.slotName = name;
            this.description = des;
            this.aliasNames.Add(name);

            if (name.Equals("B_RH", StringComparison.OrdinalIgnoreCase) || id == 1)
            {
                aliasNames.Add("Bip01 R Hand");
                aliasNames.Add("R_Hand");
                aliasNames.Add("Weapon_R");
            }
            else if (name.Equals("B_LH", StringComparison.OrdinalIgnoreCase) || id == 2)
            {
                aliasNames.Add("Bip01 L Hand");
                aliasNames.Add("L_Hand");
                aliasNames.Add("Weapon_L");
            }
            else if (name.Equals("Bip01 Spine1", StringComparison.OrdinalIgnoreCase) || id == 7)
            {
                aliasNames.Add("Spine1");
                aliasNames.Add("Bip01 Spine");
                aliasNames.Add("Spine");
            }
            else if (name.Equals("S_Hat", StringComparison.OrdinalIgnoreCase) || id == 15 || id == 21)
            {
                aliasNames.Add("head");
                aliasNames.Add("head_bone");
                aliasNames.Add("Bip001 Head");
                aliasNames.Add("Bip01 Head");
                aliasNames.Add("Head");
            }
            else if (id == 19 || name.Equals("Bip01 R Foot", StringComparison.OrdinalIgnoreCase))
            {
                aliasNames.Add("R_Foot");
                aliasNames.Add("Foot_R");
            }
            else if (id == 20 || name.Equals("Bip01 L Foot", StringComparison.OrdinalIgnoreCase))
            {
                aliasNames.Add("L_Foot");
                aliasNames.Add("Foot_L");
            }
            else if (id == 6 || name.Equals("back", StringComparison.OrdinalIgnoreCase))
            {
                aliasNames.Add("Back");
                aliasNames.Add("B_Spine2");
            }
        }
    }
}
