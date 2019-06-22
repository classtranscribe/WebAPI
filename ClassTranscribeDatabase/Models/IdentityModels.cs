using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace ClassTranscribeDatabase.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public virtual List<UserOffering> UserOfferings { get; set; }
        public virtual University University { get; set; }
    }
}
