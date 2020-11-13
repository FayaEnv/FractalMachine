﻿using System;
using System.Collections.Generic;
using System.Text;

namespace FractalMachine.Code.Components
{
    public class Struct : DataStructure
    {
        public Struct(Component parent, Linear linear) : base(parent, linear)
        {
            dataStructureType = DataStructureTypes.Struct;
        }

        #region ReadLinear

        public override void ReadSubLinear(Linear instr)
        {
            switch (instr.Op)
            {
                case "declare":
                    readLinear_declare(instr);
                    break;

                default:
                    throw new Exception("Operation not permitted");
            }
        }

        #endregion

        #region Write

        public override string WriteTo(Lang Lang)
        {
            throw new NotImplementedException();
        }

        #endregion

    }
}