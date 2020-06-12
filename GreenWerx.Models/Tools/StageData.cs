using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenWerx.Models.Tools
{
    [Table("StageData")]
    public class StageData
    {
        public StageData()
        {
            UUID = Guid.NewGuid().ToString("N");
        }

        public string DataType { get; set; }

        public DateTime? DateParsed { get; set; }

        public string Domain { get; set; }

        // the local data that may match
        public string LocalMatch { get; set; }

        // should be x/y  x points match over y = total number of points to test.
        // name
        // email
        // phone
        //
        public string MatchConfidence { get; set; }

        public string NSFW { get; set; }

        public DateTime? PublishedDate { get; set; }

        public string Result { get; set; }

        public string StageResults { get; set; }

        public DateTime? SyncDate { get; set; }

        public string Type { get; set; }

        [Key]
        public string UUID { get; set; }
    }
}