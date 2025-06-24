using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.Models.Enviroment
{
    public class RoomModels
    {
        public string Description { get; set; }
        public string Id { get; set; }

        public RoomModels(string description, string id)
        {
            Description = description;
            Id = id;
        }
    }
}
