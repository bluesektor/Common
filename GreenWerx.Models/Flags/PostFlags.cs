using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GreenWerx.Models.Flags
{
    public class PostFlags
    {
        public struct Status
        {
            public const string Publish = "publish";

            public const string Draft = "draft";

            //was published by user, needs admin approval to
            //prevent spam.
            public const string Moderate = "moderate";

        }
    }
}
