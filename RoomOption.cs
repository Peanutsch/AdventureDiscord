using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventure
{
    public class RoomOption
    {
        public string Description { get; set; }
        public string Id { get; set; }

        public RoomOption(string description, string id)
        {
            Description = description;
            Id = id;
        }
    }
}
