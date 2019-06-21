using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace ClassTranscribeDatabase.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public List<UserOffering> UserOfferings { get; set; }
        public University University { get; set; }
    }
}
