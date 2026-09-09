using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AskMe.Infrastructure.Storage
{
    public class PostgresImageStorageOptions
    {
        public const string SectionName = "ImageStorage";

        // Public origin the browser can reach directly (e.g. your deployed API
        // URL) - needed because GetPublicUrl must return an absolute URL usable
        // straight in an <img src>, not a relative path resolved against the
        // frontend's own origin.
        public required string PublicBaseUrl { get; set; }
    }
}
