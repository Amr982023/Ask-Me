using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AskMe.Application.Common.Models
{
    public record FileContent(byte[] Data, string ContentType);
}
