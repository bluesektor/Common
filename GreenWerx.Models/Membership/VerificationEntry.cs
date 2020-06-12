using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenWerx.Models.Membership
{
    [Table("UserVerificationLog")]
    public class VerificationEntry
    {
        public VerificationEntry()
        {
            UUID = Guid.NewGuid().ToString("N");
            UUIDType = "VerificationEntry";
            VerificationDate = DateTime.UtcNow;
        }

        [Key]
        public string UUID { get; set; }

        public string UUIDType { get; set; }

        public DateTime VerificationDate { get; set; }

        public string RecipientUUID { get; set; }
        public string RecipientProfileUUID { get; set; }
        public string RecipientAccountUUID { get; set; }
        public string RecipientIP { get; set; }
        public string RecipientLocationUUID { get; set; }

        public string VerifierUUID { get; set; }
        public string VerifierIP { get; set; }
        public string VerifierProfileUUID { get; set; }
        public string VerifierAccountUUID { get; set; }
        public string VerifierRoleUUID { get; set; }
        public string VerifierLocationUUID { get; set; }

        public string VerificationType { get; set; }

        //  role.Category=member.Weight <== of verifying user
        public int Weight { get; set; }

        //relationshipRole.Weight <== of verifying user
        public int Multiplier { get; set; }

        public int VerificationTypeMultiplier { get; set; }

        //= ((verificationType=inperson,photo..) + weight) * multiplier
        public int Points { get; set; }

        public bool Deleted { get; set; }

        public DateTime? DateDeleted { get; set; }

        public double VerifierLatitude { get; set; }
        public double VerifierLongitude { get; set; }
        public double RecipientLatitude { get; set; }
        public double RecipientLongitude { get; set; }
    }
}