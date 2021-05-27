using Photon.Deterministic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Quantum {
  partial class RuntimePlayer
    {
        public long roleid = 0;
        public string rolename = "";
        public int  headid = 0;
        public int  avaterId = 0;
        public int  hairId = 0;
        public int  hairColor = 0;
        public int  bodyColor = 0;
        public bool IsLoadMap = false;
        public bool IsLoadScene = false;
        public bool IsLoadPlayer = false;
        public bool IsEnter = false;
        public bool IsFinish = false;

        partial void SerializeUserData(BitStream stream)
        {
            stream.Serialize(ref roleid);
            stream.Serialize(ref rolename);
            stream.Serialize(ref headid);
            stream.Serialize(ref avaterId);
            stream.Serialize(ref hairId);
            stream.Serialize(ref hairColor);
            stream.Serialize(ref bodyColor);
            stream.Serialize(ref IsLoadMap);
            stream.Serialize(ref IsLoadScene);
            stream.Serialize(ref IsLoadPlayer);
            stream.Serialize(ref IsEnter);
            stream.Serialize(ref IsFinish);
        }
    }
}
