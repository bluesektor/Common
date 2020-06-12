namespace GreenWerx.Models.Flags
{
    public class RoleFlags
    {
        public struct BlockingRoles
        {
        }

        public struct MemberRoleNames
        {
            public const string Admin = "Admin";
            public const string Couple = "Couple";
            public const string Group = "Group";
            public const string Guest = "Guest";
            public const string Manager = "Manager";
            public const string Member = "Member";
            public const string Moderator = "Moderator";
            public const string Owner = "Owner";
            public const string Partner = "Partner";
            public const string Poly = "Poly";
            public const string SingleFemale = "Single Female";
            public const string SingleMale = "Single Male";
            public const string Subscriber = "Subscriber";
        }

        public struct MemberRoleWeights
        {
            public const int Admin = 95;
            public const int Couple = 5;
            public const int Group = 2;
            public const int Guest = 0;
            public const int Manager = 90;
            public const int Member = 10;
            public const int Moderator = 94;
            public const int Owner = 100;
            public const int Partner = 50;
            public const int Poly = 4;
            public const int SingleFemale = 3;
            public const int SingleMale = 1;
            public const int Subscriber = 20;
        }

        public struct VerifiedRoleWeights
        {
            public const int VerifiedByAmbassador = 33;
            public const int VerifiedByCriticalUser = 34;
            public const int VerifiedByOtherMember = 31;
            public const int VerifiedByPhotoSubmission = 32;
            public const int VerifiedWithGeolocation = 33;
        }

        //Name                          Weight      RoleWeight      Category        Status
        //Guest	                        0	        0	            nonmember       guest
        //Block Single Females	        0	        0	            block           single female
        //Block Groups	                0	        0	            block           group
        //Block Poly	                0	        0	            block           poly
        //Block Couples	                0	        0	            block           couple
        //Block	                        0	        0	            block
        //Single Male	                1	        1	            member          single male
        //Group	                        2	        2	            member          group
        //Single Female	                3	        3	            member          single female
        //Poly	                        4	        4	            member          poly
        //Couple	                    5	        5	            member          couple
        //Block Single Males	        5	        5	            block           single male
        //Member	                    10	        10	            member          member
        //Subscriber	                20	        20	            member          subscriber
        //Verifiedby by Other Member	31	        31	            verified        other member
        //Verifiedby Photo Submission	32	        32	            verified        photo submission
        //Verified by Ambassador	    33	        33	            verified        ambassador
        //Verified with Geolocation	    33	        33	            verified        geolocation
        //Verified by Critical User	    34	        34	            verified        critical user
        //Partner	                    50	        50	            member          partner
        //Manager	                    90	        90	            member          manager
        //Moderator	                    94	        94	            member          moderator
        //Admin	                        95	        95	            member          admin
        //Owner	                        100	        100	            member          owner
    }
}