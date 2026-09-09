using AskMe.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AskMe.Domain.Entities
{
    public class StoredImage : BaseEntity
    {
        public required byte[] Data { get; set; }
        public required string ContentType { get; set; }
    }
}
