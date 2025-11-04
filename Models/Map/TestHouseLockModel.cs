using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Adventure.Models.Map
{
    public class TestHouseLockModel
    {
        public string LockType { get; set; } = "none";

        public bool Locked { get; set; } = false;
    }
}
