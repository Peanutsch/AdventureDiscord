using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Adventure.Models.Map
{
    public class TestHouseModel
    {
        public Dictionary<string, TestHouseAreaModel> Areas { get; set; } = new();
    }
}
